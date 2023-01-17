namespace PassMan.Core
{
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Download;
    using Google.Apis.Drive.v3;
    using Google.Apis.Services;
    using Google.Apis.Upload;
    using Google.Apis.Util.Store;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using GFile = Google.Apis.Drive.v3.Data.File;

    public sealed class GoogleDriveStorageProvider : LocalStorageProvider, IStorageProvider
    {
        private readonly DriveService service;
        private readonly Dictionary<string, string> nameToId = new();
        private const string credPath = "tokens";

        public GoogleDriveStorageProvider(string path) : base(Path.Combine(path, "gdrive"))
        {
            UserCredential credential;
            IConfigurationRoot config = new ConfigurationBuilder().SetBasePath(AppDomain.CurrentDomain.BaseDirectory).AddUserSecrets<App>().Build();
            ClientSecrets secrets = new()
            {
                ClientId = config["google:client_id"],
                ClientSecret = config["google:client_secret"]
            };
            credential = GoogleWebAuthorizationBroker.AuthorizeAsync(secrets, new string[] { DriveService.Scope.DriveFile }, "user", CancellationToken.None, new FileDataStore(credPath, true)).Result;
            service = new(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "PassMan Beta",
            });

            foreach (var file in service.Files.List().Execute().Files)
            {
                nameToId.Add(file.Name, file.Id);
            }
        }

        #region GDrive Methods

        public override sealed ProviderType Type => ProviderType.GoogleDrive;

        public void Delete(string file)
        {
            var req = service.Files.Delete(nameToId[file]);
            var res = req.Execute();
            Trace.WriteLine(res);
            lock (nameToId)
            {
                nameToId.Remove(file);
            }
        }

        public void Create(string file, byte[] data)
        {
            GFile gFile = new()
            {
                Name = file,
                Description = string.Empty,
                MimeType = "application/x-binary"
            };

            var stream = new MemoryStream(data);
            var req = service.Files.Create(gFile, stream, "application/x-binary");
            var res = req.Upload();

            if (res.Status != UploadStatus.Completed)
            {
                throw res.Exception;
            }

            lock (nameToId)
            {
                nameToId.Add(file, gFile.Id);
            }

            stream.Close();
        }

        public void Write(string file, byte[] data)
        {
            if (Exists(file))
            {
                Delete(file);
            }

            Create(file, data);
        }

        public byte[] Read(string file)
        {
            var req = service.Files.Get(nameToId[file]);
            var stream = new MemoryStream();
            var res = req.DownloadWithStatus(stream);
            if (res.Status != DownloadStatus.Completed)
            {
                throw res.Exception;
            }
            stream.Flush();
            var data = stream.ToArray();
            stream.Close();
            return data;
        }

        public async Task DeleteAsync(string file)
        {
            var req = service.Files.Delete(nameToId[file]);
            var res = await req.ExecuteAsync();
            Trace.WriteLine(res);
            lock (nameToId)
            {
                nameToId.Remove(file);
            }
        }

        public async Task CreateAsync(string file, byte[] data)
        {
            GFile gFile = new()
            {
                Name = file,
                Description = string.Empty,
                MimeType = "application/x-binary"
            };

            var stream = new MemoryStream(data);
            var req = service.Files.Create(gFile, stream, "application/x-binary");
            var res = await req.UploadAsync();

            if (res.Status != UploadStatus.Completed)
            {
                throw res.Exception;
            }

            lock (nameToId)
            {
                nameToId.Add(file, gFile.Id);
            }

            stream.Close();
        }

        public async Task WriteAsync(string file, byte[] data)
        {
            if (Exists(file))
            {
                await DeleteAsync(file);
            }

            await CreateAsync(file, data);
        }

        public async Task<byte[]> ReadAsync(string file)
        {
            var req = service.Files.Get(nameToId[file]);
            var stream = new MemoryStream();
            var res = await req.DownloadAsync(stream);
            if (res.Status != DownloadStatus.Completed)
            {
                throw res.Exception;
            }
            stream.Flush();
            var data = stream.ToArray();
            stream.Close();
            return data;
        }

        public bool Exists(string file)
        {
            return nameToId.ContainsKey(file);
        }

        #endregion GDrive Methods

        public override sealed bool Exists()
        {
            return Exists(filename);
        }

        public override sealed bool FileExists(string name)
        {
            return Exists(name);
        }

        public override sealed void Save(byte[] data)
        {
            base.Save(data);
            Write(filename, data);
        }

        public override sealed void SaveFile(byte[] data, string name)
        {
            base.SaveFile(data, name);
            Write(name, data);
        }

        public override sealed byte[] Load()
        {
            var data = Read(filename);
            base.Save(data);
            return data;
        }

        public override sealed byte[] LoadFile(string name)
        {
            var data = Read(name);
            base.SaveFile(data, name);
            return data;
        }

        public override sealed async Task SaveAsync(byte[] data)
        {
            await base.SaveAsync(data);
            await WriteAsync(filename, data);
        }

        public override sealed async Task SaveFileAsync(byte[] data, string name)
        {
            await base.SaveFileAsync(data, name);
            await WriteAsync(name, data);
        }

        public override sealed async Task<byte[]> LoadAsync()
        {
            var data = await ReadAsync(filename);
            await base.SaveAsync(data);
            return data;
        }

        public override sealed async Task<byte[]> LoadFileAsync(string name)
        {
            var data = await ReadAsync(name);
            base.SaveFile(data, name);
            return data;
        }
    }
}