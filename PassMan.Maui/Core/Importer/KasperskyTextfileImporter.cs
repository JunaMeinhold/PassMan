namespace PassMan.Core.Importer
{
    using PassMan.Core;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    public static class KasperskyTextfileImporter
    {
        public static void Import(string file, Vault vault)
        {
            List<VaultItem> items = new();
            FileStream fs = File.OpenRead(file);
            TextReader reader = new StreamReader(fs, Encoding.UTF8);
            string mode = string.Empty;
            Queue<string> lines = new();
            while (fs.Position != fs.Length)
            {
                string? line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                if (line == "---")
                {
                    ImportItem(items, mode, lines);
                    Trace.Assert(lines.Count == 0, "Stack is not empty");

                    continue;
                }

                if (line == "Websites")
                {
                    mode = nameof(Website);
                    continue;
                }

                if (line == "Applications")
                {
                    mode = nameof(App);
                    continue;
                }

                if (line == "Notes")
                {
                    mode = nameof(Note);
                    continue;
                }

                lines.Enqueue(line);
            }

            foreach (VaultItem item in items)
            {
                vault.Add(item);
            }
        }

        private static void ImportItem(List<VaultItem> items, string mode, Queue<string> lines)
        {
            if (mode == nameof(Website))
            {
                string websiteName = lines.Dequeue().Replace("Website name: ", string.Empty);
                string websiteUrl = lines.Dequeue().Replace("Website URL: ", string.Empty);
                string name = lines.Dequeue().Replace("Login name: ", string.Empty);
                string login = lines.Dequeue().Replace("Login: ", string.Empty);
                string password = lines.Dequeue().Replace("Password: ", string.Empty);
                string comment = lines.Dequeue().Replace("Comment: ", string.Empty);
                Account account = new(name, login, password, comment);
                Website? website = items.WhereCast<VaultItem, Website>().FirstOrDefault(x => x.Url == websiteUrl);
                if (website != null)
                {
                    website.Accounts.Add(account);
                }
                else
                {
                    website = new(websiteName, websiteUrl);
                    website.Accounts.Add(account);
                    items.Add(website);
                }
            }
            if (mode == nameof(App))
            {
                string appName = lines.Dequeue().Replace("Application: ", string.Empty);
                string name = lines.Dequeue().Replace("Login name: ", string.Empty);
                string login = lines.Dequeue().Replace("Login: ", string.Empty);
                string password = lines.Dequeue().Replace("Password: ", string.Empty);
                string comment = lines.Dequeue().Replace("Comment: ", string.Empty);
                Account account = new(name, login, password, comment);
                App? app = items.WhereCast<VaultItem, App>().FirstOrDefault(x => x.Name == appName);
                if (app != null)
                {
                    app.Accounts.Add(account);
                }
                else
                {
                    app = new(appName);
                    app.Accounts.Add(account);
                    items.Add(app);
                }
            }
            if (mode == nameof(Note))
            {
                Note note = new(
                    lines.Dequeue().Replace("Application: ", string.Empty),
                    lines.Dequeue().Replace("Application: ", string.Empty));
            }
        }
    }
}