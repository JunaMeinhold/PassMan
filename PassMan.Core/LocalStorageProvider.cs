namespace PassMan.Core
{
    public class LocalStorageProvider : IStorageProvider
    {
        public const string filename = "vault.bin";

        private readonly string basePath;
        private readonly string filePath;
        public virtual ProviderType Type => ProviderType.Local;

        public LocalStorageProvider(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            basePath = path;
            filePath = Path.Combine(basePath, filename);
        }

        public virtual bool Exists()
        {
            return File.Exists(filePath);
        }

        public virtual bool FileExists(string name)
        {
            return File.Exists(Path.Combine(basePath, name));
        }

        public virtual void Migrate(IStorageProvider provider, string[] files)
        {
            Save(provider.Load());

            foreach (var file in files)
            {
                var name = file;
                SaveFile(provider.LoadFile(name), name);
            }
        }

        public virtual void Save(byte[] data)
        {
            File.WriteAllBytes(filePath, data);
        }

        public virtual byte[] Load()
        {
            return File.ReadAllBytes(filePath);
        }

        public virtual void SaveFile(byte[] data, string name)
        {
            File.WriteAllBytes(Path.Combine(basePath, name), data);
        }

        public virtual byte[] LoadFile(string name)
        {
            return File.ReadAllBytes(Path.Combine(basePath, name));
        }

        public virtual async Task MigrateAsync(IStorageProvider provider, string[] files)
        {
            await SaveAsync(await provider.LoadAsync());

            foreach (var file in files)
            {
                var name = file;
                await SaveFileAsync(await provider.LoadFileAsync(name), name);
            }
        }

        public virtual async Task SaveAsync(byte[] data)
        {
            await File.WriteAllBytesAsync(filePath, data);
        }

        public virtual async Task SaveFileAsync(byte[] data, string name)
        {
            await File.WriteAllBytesAsync(name, data);
        }

        public virtual async Task<byte[]> LoadAsync()
        {
            return await File.ReadAllBytesAsync(filePath);
        }

        public virtual async Task<byte[]> LoadFileAsync(string name)
        {
            return await File.ReadAllBytesAsync(Path.Combine(basePath, name));
        }
    }
}