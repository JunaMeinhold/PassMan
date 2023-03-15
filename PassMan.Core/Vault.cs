namespace PassMan.Core
{
    using System;
    using System.Buffers.Binary;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Cryptography;

    public class Vault : IDisposable, INotifyCollectionChanged
    {
        private readonly List<VaultItem> _items = new();
        private readonly IStorageProvider provider;
        internal SecureString password;
        private bool disposedValue;

        public const int Version = 1;

        public bool IsReadOnly { get; private set; }

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public Vault(IStorageProvider provider, SecureString password)
        {
            this.provider = provider;
            this.password = password;
        }

        public SecureString GetPassword()
        {
            return password;
        }

        public void SetPassword(SecureString pwd)
        {
            var old = password;
            password = pwd;
            for (int i = 0; i < Files.Count; i++)
            {
                var file = Files[i];
                file.Reencrypt(old, pwd);
            }
            Save();
        }

        public VaultItem this[int index] { get => ((IList<VaultItem>)_items)[index]; set => ((IList<VaultItem>)_items)[index] = value; }

        public int Count => ((ICollection<VaultItem>)_items).Count;

        public ObservableCollection<Website> Websites { get; } = new();

        public ObservableCollection<App> Apps { get; } = new();

        public ObservableCollection<Note> Notes { get; } = new();

        public ObservableCollection<EncryptedFile> Files { get; } = new();

        private void Integrate(VaultItem item)
        {
            if (item is Website website)
                Websites.Add(website);
            if (item is App app)
                Apps.Add(app);
            if (item is Note note)
                Notes.Add(note);
            if (item is EncryptedFile file)
                Files.Add(file);
        }

        private void Disintegrate(VaultItem item)
        {
            if (item is Website website)
                Websites.Remove(website);
            if (item is App app)
                Apps.Remove(app);
            if (item is Note note)
                Notes.Remove(note);
            if (item is EncryptedFile file)
                Files.Remove(file);
        }

        public void Add(VaultItem item)
        {
            ((ICollection<VaultItem>)_items).Add(item);
            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Add, item));
            Integrate(item);
        }

        public void Clear()
        {
            ((ICollection<VaultItem>)_items).Clear();
            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Reset));
            Websites.Clear();
            Apps.Clear();
            Notes.Clear();
        }

        public bool Contains(VaultItem item)
        {
            return ((ICollection<VaultItem>)_items).Contains(item);
        }

        public void CopyTo(VaultItem[] array, int arrayIndex)
        {
            ((ICollection<VaultItem>)_items).CopyTo(array, arrayIndex);
        }

        public IEnumerator<VaultItem> GetEnumerator()
        {
            return ((IEnumerable<VaultItem>)_items).GetEnumerator();
        }

        public int IndexOf(VaultItem item)
        {
            return ((IList<VaultItem>)_items).IndexOf(item);
        }

        public bool Remove(VaultItem item)
        {
            bool result = ((ICollection<VaultItem>)_items).Remove(item);
            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Remove, item));
            Disintegrate(item);
            return result;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                foreach (VaultItem item in _items)
                {
                    Disintegrate(item);
                    item.Dispose();
                }
                _items.Clear();

                disposedValue = true;
            }
        }

        ~Vault()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Save()
        {
            Trace.WriteLine("Autosaving...");
            if (IsReadOnly)
                return;

            int size = ComputeSize();
            byte[] data = new byte[size];
            GCHandle bufferHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            Span<byte> buffer = data;

            BinaryPrimitives.WriteInt32LittleEndian(buffer, _items.Count);
            int written = 4;
            foreach (var item in _items)
            {
                written += item.Type.WriteString(buffer[written..]);
                written += item.WriteTo(buffer[written..]);
            }

            password.HashSecureString(out var hash, out var hashHandle);
            AesGcm aes = new(hash);

            int cipherSize = Extensions.MeasureEncryptedSize(size);
            Span<byte> cipher = new byte[cipherSize];
            aes.Encrypt(buffer, cipher);

            hash.Fill(0);
            hashHandle.Free();

            buffer.Fill(0);
            bufferHandle.Free();

            provider.Save(cipher.ToArray());
        }

        public void Load()
        {
            Span<byte> cipher = provider.Load();

            password.HashSecureString(out var hash, out var hashHandle);

            AesGcm aes = new(hash);
            int size = Extensions.MeasureDecryptedSize(cipher);

            byte[] data = new byte[size];
            GCHandle bufferHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            Span<byte> buffer = data;

            aes.Decrypt(cipher, buffer);

            hash.Fill(0);
            hashHandle.Free();

            int itemCount = BinaryPrimitives.ReadInt32LittleEndian(buffer);

            int read = 4;
            for (int i = 0; i < itemCount; i++)
            {
                VaultItem? item = null;
                read += buffer[read..].ReadString(out string type);
                if (type == nameof(Note))
                {
                    item = new Note();
                }
                if (type == nameof(Website))
                {
                    item = new Website();
                }
                if (type == nameof(App))
                {
                    item = new App();
                }
                if (type == nameof(EncryptedFile))
                {
                    item = new EncryptedFile();
                }

                if (item != null)
                {
                    read += item.ReadFrom(buffer[read..]);
                    _items.Add(item);
                }
            }

            // Zeros the buffer.
            buffer.Fill(0);
            bufferHandle.Free();

            // Integrate to extra lists;
            foreach (var item in _items)
            {
                Integrate(item);
            }
        }

        private int ComputeSize()
        {
            int size = 4;
            foreach (VaultItem item in _items)
            {
                size += item.Type.SizeOfString();
                size += item.ComputeSize();
            }
            return size;
        }
    }
}