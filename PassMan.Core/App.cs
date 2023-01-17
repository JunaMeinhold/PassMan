namespace PassMan.Core
{
    using System;
    using System.Collections.ObjectModel;

    public class App : VaultItem
    {
        private string? name;
        private readonly ObservableCollection<Account> accounts = new();

        public App(string appName)
        {
            name = appName;
            accounts.CollectionChanged += (s, e) => NotifyCollectionChanged(e);
        }

        public App()
        {
            accounts.CollectionChanged += (s, e) => NotifyCollectionChanged(e);
        }

        public string? Name
        { get { return name; } set { name = value; } }

        public ObservableCollection<Account> Accounts { get => accounts; }

        public override string Type => nameof(App);

        public override int ReadFrom(Span<byte> source)
        {
            int read = 0;
            read += source[read..].ReadString(out name);
            read += source[read..].ReadCollection(accounts);
            return read;
        }

        public override int WriteTo(Span<byte> destination)
        {
            int written = 0;
            written += name.WriteString(destination[written..]);
            written += accounts.WriteCollection(destination[written..]);
            return written;
        }

        public override int ComputeSize()
        {
            int size = 0;
            size += name.SizeOfString();
            size += accounts.SizeOfCollection();
            return size;
        }

        public override string ToString()
        {
            return name ?? string.Empty;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            name = null;
            foreach (var account in accounts)
            {
                account.Dispose();
            }
            accounts.Clear();
        }
    }
}