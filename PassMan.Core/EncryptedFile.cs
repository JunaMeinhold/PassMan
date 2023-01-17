namespace PassMan.Core
{
    using System;
    using System.IO;
    using System.Security;
    using System.Security.Cryptography;

    public class EncryptedFile : VaultItem
    {
        private string? name;
        private string path;

        public EncryptedFile(string name, string path)
        {
            this.name = name;
            this.path = path;
        }

        public EncryptedFile()
        {
            path = string.Empty;
        }

        public static async Task<EncryptedFile> CreateAsync(IStorageProvider provider, string path, SecureString password)
        {
            var file = Create(path, password, out byte[] data);
            await provider.SaveFileAsync(data, file.path);
            return file;
        }

        public static EncryptedFile Create(string path, SecureString password, out byte[] data)
        {
            string newPath = Guid.NewGuid().ToString();
            string name = System.IO.Path.GetFileName(path);

            FileStream fsInput = File.OpenRead(path);
            MemoryStream fsOutput = new();

            Span<byte> inBuffer = new byte[4096];

            password.HashSecureString(out Span<byte> hash, out var hashHandle);
            AesGcm aes = new(hash);

            Span<byte> outBuffer = new byte[4096 + 4 + AesGcm.NonceByteSizes.MaxSize + 4 + AesGcm.TagByteSizes.MaxSize];

            while (fsInput.Position < fsInput.Length)
            {
                if (fsInput.Length - fsInput.Position >= inBuffer.Length)
                {
                    fsInput.Read(inBuffer);
                    aes.Encrypt(inBuffer, outBuffer);
                    fsOutput.Write(outBuffer);
                }
                else
                {
                    Span<byte> inbuffer = new byte[fsInput.Length - fsInput.Position];
                    Span<byte> outbuffer = new byte[Extensions.MeasureEncryptedSize(inbuffer.Length)];
                    fsInput.Read(inbuffer);
                    aes.Encrypt(inbuffer, outbuffer);
                    fsOutput.Write(outbuffer);
                    inbuffer.Fill(0);
                    outbuffer.Fill(0);
                }
            }

            aes.Dispose();
            hash.Fill(0);
            hashHandle.Free();

            inBuffer.Fill(0);
            outBuffer.Fill(0);

            fsOutput.Flush();
            data = fsOutput.ToArray();
            fsOutput.Close();
            fsOutput.Dispose();

            fsInput.Close();
            fsInput.Dispose();

            return new EncryptedFile(name, newPath);
        }

        public static EncryptedFile Create(IStorageProvider provider, string path, SecureString password)
        {
            string newPath = Guid.NewGuid().ToString();
            string name = System.IO.Path.GetFileName(path);

            FileStream fsInput = File.OpenRead(path);
            MemoryStream fsOutput = new();

            Span<byte> inBuffer = new byte[4096];

            password.HashSecureString(out Span<byte> hash, out var hashHandle);
            AesGcm aes = new(hash);

            Span<byte> outBuffer = new byte[4096 + 4 + AesGcm.NonceByteSizes.MaxSize + 4 + AesGcm.TagByteSizes.MaxSize];

            while (fsInput.Position < fsInput.Length)
            {
                if (fsInput.Length - fsInput.Position >= inBuffer.Length)
                {
                    fsInput.Read(inBuffer);
                    aes.Encrypt(inBuffer, outBuffer);
                    fsOutput.Write(outBuffer);
                }
                else
                {
                    Span<byte> inbuffer = new byte[fsInput.Length - fsInput.Position];
                    Span<byte> outbuffer = new byte[Extensions.MeasureEncryptedSize(inbuffer.Length)];
                    fsInput.Read(inbuffer);
                    aes.Encrypt(inbuffer, outbuffer);
                    fsOutput.Write(outbuffer);
                    inbuffer.Fill(0);
                    outbuffer.Fill(0);
                }
            }

            aes.Dispose();
            hash.Fill(0);
            hashHandle.Free();

            inBuffer.Fill(0);
            outBuffer.Fill(0);

            fsOutput.Flush();
            var data = fsOutput.ToArray();
            provider.SaveFile(data, newPath);
            fsOutput.Close();
            fsOutput.Dispose();

            fsInput.Close();
            fsInput.Dispose();

            return new EncryptedFile(name, newPath);
        }

        public static EncryptedFile Create(string path, SecureString password)
        {
            string newPath = Guid.NewGuid().ToString();
            string name = System.IO.Path.GetFileName(path);

            FileStream fsInput = File.OpenRead(path);
            FileStream fsOutput = File.Create(System.IO.Path.Combine(Paths.RootPath, newPath));

            Span<byte> inBuffer = new byte[4096];

            password.HashSecureString(out Span<byte> hash, out var hashHandle);
            AesGcm aes = new(hash);

            Span<byte> outBuffer = new byte[4096 + 4 + AesGcm.NonceByteSizes.MaxSize + 4 + AesGcm.TagByteSizes.MaxSize];

            while (fsInput.Position < fsInput.Length)
            {
                if (fsInput.Length - fsInput.Position >= inBuffer.Length)
                {
                    fsInput.Read(inBuffer);
                    aes.Encrypt(inBuffer, outBuffer);
                    fsOutput.Write(outBuffer);
                }
                else
                {
                    Span<byte> inbuffer = new byte[fsInput.Length - fsInput.Position];
                    Span<byte> outbuffer = new byte[Extensions.MeasureEncryptedSize(inbuffer.Length)];
                    fsInput.Read(inbuffer);
                    aes.Encrypt(inbuffer, outbuffer);
                    fsOutput.Write(outbuffer);
                    inbuffer.Fill(0);
                    outbuffer.Fill(0);
                }
            }

            aes.Dispose();
            hash.Fill(0);
            hashHandle.Free();

            inBuffer.Fill(0);
            outBuffer.Fill(0);

            fsOutput.Flush();
            fsOutput.Close();
            fsOutput.Dispose();

            fsInput.Close();
            fsInput.Dispose();

            return new EncryptedFile(name, newPath);
        }

        public override string Type => nameof(EncryptedFile);

        public string? Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                NotifyPropertyChanged();
            }
        }

        public string Path
        {
            get
            {
                return path;
            }
            set
            {
                path = value;
                NotifyPropertyChanged();
            }
        }

        public override int ComputeSize()
        {
            int size = 0;
            size += name.SizeOfString();
            size += path.SizeOfString();
            return size;
        }

        public override int ReadFrom(Span<byte> source)
        {
            int read = 0;
            read += source[read..].ReadString(out name);
            read += source[read..].ReadString(out path);
            return read;
        }

        public override int WriteTo(Span<byte> destination)
        {
            int written = 0;
            written += name.WriteString(destination[written..]);
            written += path.WriteString(destination[written..]);
            return written;
        }

        public void Delete()
        {
            File.Delete(System.IO.Path.Combine(Paths.RootPath, path));
        }

        public void Export(string path, SecureString password)
        {
            FileStream fsInput = File.OpenRead(System.IO.Path.Combine(Paths.RootPath, this.path));
            FileStream fsOutput = File.Create(path);

            Span<byte> inBuffer = new byte[4096 + 4 + AesGcm.NonceByteSizes.MaxSize + 4 + AesGcm.TagByteSizes.MaxSize];

            password.HashSecureString(out Span<byte> hash, out var hashHandle);
            AesGcm aes = new(hash);

            Span<byte> outBuffer = new byte[4096];

            while (fsInput.Position < fsInput.Length)
            {
                if (fsInput.Length - fsInput.Position >= inBuffer.Length)
                {
                    fsInput.Read(inBuffer);
                    aes.Decrypt(inBuffer, outBuffer);
                    fsOutput.Write(outBuffer);
                }
                else
                {
                    Span<byte> inbuffer = new byte[fsInput.Length - fsInput.Position];
                    fsInput.Read(inbuffer);
                    Span<byte> outbuffer = new byte[Extensions.MeasureDecryptedSize(inbuffer)];
                    aes.Decrypt(inbuffer, outbuffer);
                    fsOutput.Write(outbuffer);
                    inbuffer.Fill(0);
                    outbuffer.Fill(0);
                }
            }

            aes.Dispose();
            hash.Fill(0);
            hashHandle.Free();

            inBuffer.Fill(0);
            outBuffer.Fill(0);

            fsOutput.Flush();
            fsOutput.Close();
            fsOutput.Dispose();

            fsInput.Close();
            fsInput.Dispose();
        }
    }
}