namespace PassMan
{
    using PassMan.Core;
    using System;
    using System.Buffers.Binary;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Cryptography;
    using System.Text;

    internal static class Extensions
    {
        public static string GetRandomAlphanumericString(int length)
        {
            const string alphanumericCharacters =
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                "abcdefghijklmnopqrstuvwxyz" +
                "0123456789";
            return GetRandomString(length, alphanumericCharacters);
        }

        public static string GetRandomAlphanumericStringEx(int length)
        {
            const string alphanumericCharacters =
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                "abcdefghijklmnopqrstuvwxyz" +
                "0123456789+-*/@!\"§$%&()=?`,._<>|";
            return GetRandomString(length, alphanumericCharacters);
        }

        public static string GetRandomString(int length, IEnumerable<char> characterSet)
        {
            if (length < 0)
                throw new ArgumentException("length must not be negative", "length");
            if (length > int.MaxValue / 8) // 250 million chars ought to be enough for anybody
                throw new ArgumentException("length is too big", "length");
            if (characterSet == null)
                throw new ArgumentNullException("characterSet");
            var characterArray = characterSet.Distinct().ToArray();
            if (characterArray.Length == 0)
                throw new ArgumentException("characterSet must not be empty", "characterSet");

            var bytes = new byte[length * 8];
            RandomNumberGenerator.Fill(bytes);
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                ulong value = BitConverter.ToUInt64(bytes, i * 8);
                result[i] = characterArray[value % (uint)characterArray.Length];
            }
            return new string(result);
        }

        public static SecureString ConvertToSecureString(this string password)
        {
            if (password == null)
                throw new ArgumentNullException("password");

            var securePassword = new SecureString();

            foreach (char c in password)
                securePassword.AppendChar(c);

            securePassword.MakeReadOnly();
            return securePassword;
        }

        public static int WriteString(this string? str, Span<byte> dest)
        {
            int written = Encoding.Unicode.GetBytes(str ?? string.Empty, dest[4..]);
            BinaryPrimitives.WriteInt32LittleEndian(dest, written);
            return 4 + written;
        }

        public static int SizeOfString(this string? str)
        {
            return Encoding.Unicode.GetByteCount(str ?? string.Empty) + 4;
        }

        public static int ReadString(this Span<byte> source, out string str)
        {
            int sizeBytes = BinaryPrimitives.ReadInt32LittleEndian(source);
            str = Encoding.Unicode.GetString(source.Slice(4, sizeBytes));
            return 4 + sizeBytes;
        }

        public static int WriteCollection<T>(this Collection<T> collection, Span<byte> dest) where T : VaultItem
        {
            int written = 4;
            BinaryPrimitives.WriteInt32LittleEndian(dest, collection.Count);
            foreach (T item in collection)
            {
                written += item.WriteTo(dest[written..]);
            }
            return written;
        }

        public static int SizeOfCollection<T>(this Collection<T> collection) where T : VaultItem
        {
            int size = 4;

            foreach (T item in collection)
            {
                size += item.ComputeSize();
            }

            return size;
        }

        public static int ReadCollection<T>(this Span<byte> dest, Collection<T> collection) where T : VaultItem, new()
        {
            int count = BinaryPrimitives.ReadInt32LittleEndian(dest);
            int read = 4;

            for (int i = 0; i < count; i++)
            {
                T t = new();
                read += t.ReadFrom(dest[read..]);
                collection.Add(t);
            }

            return read;
        }

        public static int WriteSecureString(this SecureString? str, Span<byte> dest)
        {
            int written = str?.Length * 2 ?? 0;
            IntPtr bstr = IntPtr.Zero;
            byte[]? workArray = null;
            GCHandle? handle = null;
            try
            {
                /*** PLAINTEXT EXPOSURE BEGINS HERE ***/
                bstr = Marshal.SecureStringToBSTR(str ?? new());
                unsafe
                {
                    byte* bstrBytes = (byte*)bstr;
                    workArray = new byte[written];
                    handle = GCHandle.Alloc(workArray, GCHandleType.Pinned);

                    for (int i = 0; i < workArray.Length; i++)
                        workArray[i] = *bstrBytes++;
                }

                BinaryPrimitives.WriteInt32LittleEndian(dest, written);
                workArray.CopyTo(dest[4..]);
            }
            finally
            {
                if (workArray != null)
                    for (int i = 0; i < workArray.Length; i++)
                        workArray[i] = 0;

                if (bstr != IntPtr.Zero)
                    Marshal.ZeroFreeBSTR(bstr);

                handle?.Free();

                /*** PLAINTEXT EXPOSURE ENDS HERE ***/
            }

            return written + 4;
        }

        public static int SizeOfSecureString(this SecureString? str)
        {
            return (str?.Length ?? 0) * 2 + 4;
        }

        public static int ReadSecureString(this Span<byte> source, out SecureString str)
        {
            int sizeBytes = BinaryPrimitives.ReadInt32LittleEndian(source);

            unsafe
            {
                char* src = (char*)Unsafe.AsPointer(ref source.Slice(4, sizeBytes).GetPinnableReference());
                str = new(src, sizeBytes / 2);
            }

            return 4 + sizeBytes;
        }

        public static void HashSecureString(this SecureString input, out Span<byte> hash, out GCHandle hashPin)
        {
            var bstr = Marshal.SecureStringToBSTR(input);
            var length = Marshal.ReadInt32(bstr, -4);
            var bytes = new byte[length];

            hash = new byte[32];
            hashPin = GCHandle.Alloc(bytes, GCHandleType.Pinned);

            var bytesPin = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                Marshal.Copy(bstr, bytes, 0, length);
                Marshal.ZeroFreeBSTR(bstr);
                SHA256.TryHashData(bytes, hash, out _);
            }
            finally
            {
                for (var i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = 0;
                }

                bytesPin.Free();
            }
        }

        public static void Expose(this SecureString secstrPassword, Action<string?> action)
        {
            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secstrPassword);
                action(Marshal.PtrToStringUni(unmanagedString));
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        public static IEnumerable<T1> WhereCast<T, T1>(this IEnumerable<T> ts) => ts.Where(x => x is T1).Cast<T1>();

        public static void Encrypt(this AesGcm aes, Span<byte> source, Span<byte> destination)
        {
            // Get parameter sizes
            var nonceSize = AesGcm.NonceByteSizes.MaxSize;
            var tagSize = AesGcm.TagByteSizes.MaxSize;
            var cipherSize = source.Length;

            // We write everything into one big array for easier encoding
            var encryptedDataLength = 4 + nonceSize + 4 + tagSize + cipherSize;

            Span<byte> encryptedData = new byte[encryptedDataLength];

            // Copy parameters
            BinaryPrimitives.WriteInt32LittleEndian(encryptedData[..4], nonceSize);
            BinaryPrimitives.WriteInt32LittleEndian(encryptedData.Slice(4 + nonceSize, 4), tagSize);
            var nonce = encryptedData.Slice(4, nonceSize);
            var tag = encryptedData.Slice(4 + nonceSize + 4, tagSize);
            var cipherBytes = encryptedData.Slice(4 + nonceSize + 4 + tagSize, cipherSize);

            // Generate secure nonce
            RandomNumberGenerator.Fill(nonce);

            // Encrypt
            aes.Encrypt(nonce, source, cipherBytes, tag);

            // Encode for transmission
            encryptedData.CopyTo(destination);
        }

        public static void Decrypt(this AesGcm aes, Span<byte> source, Span<byte> destination)
        {
            // Extract parameter sizes
            var nonceSize = BinaryPrimitives.ReadInt32LittleEndian(source);
            var tagSize = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(4 + nonceSize, 4));
            var cipherSize = source.Length - 4 - nonceSize - 4 - tagSize;

            // Extract parameters
            var nonce = source.Slice(4, nonceSize);
            var tag = source.Slice(4 + nonceSize + 4, tagSize);
            var cipherBytes = source.Slice(4 + nonceSize + 4 + tagSize, cipherSize);

            // Decrypt
            aes.Decrypt(nonce, cipherBytes, tag, destination);
        }

        public static int MeasureEncryptedSize(int size)
        {
            return 4 + AesGcm.NonceByteSizes.MaxSize + 4 + AesGcm.TagByteSizes.MaxSize + size;
        }

        public static int MeasureDecryptedSize(Span<byte> source)
        {
            var nonceSize = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(0, 4));
            var tagSize = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(4 + nonceSize, 4));
            var cipherSize = source.Length - 4 - nonceSize - 4 - tagSize;
            return cipherSize;
        }
    }
}