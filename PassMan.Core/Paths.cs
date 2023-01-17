namespace PassMan.Core
{
    using System;

    public class Paths
    {
        static Paths()
        {
            RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PassMan");
            if (!Directory.Exists(RootPath))
                Directory.CreateDirectory(RootPath);
            VaultPath = Path.Combine(RootPath, "vault.bin");
        }

        public static readonly string RootPath;
        public static readonly string VaultPath;
    }
}