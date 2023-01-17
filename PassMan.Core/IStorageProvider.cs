namespace PassMan.Core
{
    public interface IStorageProvider
    {
        public ProviderType Type { get; }

        bool Exists();

        bool FileExists(string name);

        void Migrate(IStorageProvider provider, string[] files);

        void Save(byte[] data);

        void SaveFile(byte[] data, string name);

        byte[] Load();

        byte[] LoadFile(string name);

        Task MigrateAsync(IStorageProvider provider, string[] files);

        Task SaveAsync(byte[] data);

        Task SaveFileAsync(byte[] data, string name);

        Task<byte[]> LoadAsync();

        Task<byte[]> LoadFileAsync(string name);
    }
}