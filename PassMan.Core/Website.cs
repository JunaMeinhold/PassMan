namespace PassMan.Core
{
    using System;
    using System.Collections.ObjectModel;

    public class Website : VaultItem
    {
        private string? name = string.Empty;
        private string? url = string.Empty;
        private readonly ObservableCollection<Account> accounts = new();

        public Website()
        {
            accounts.CollectionChanged += (s, e) => NotifyCollectionChanged(e);
        }

        public Website(string name, string url)
        {
            this.name = name;
            this.url = url;
            accounts.CollectionChanged += (s, e) => NotifyCollectionChanged(e);
        }

        public string? Name
        {
            get => name; set
            {
                name = value;
                NotifyPropertyChanged();
            }
        }

        public string? Url
        { get => url; set { url = value; NotifyPropertyChanged(); } }

        public ObservableCollection<Account> Accounts { get => accounts; }

        public override string Type => nameof(Website);

        public override int WriteTo(Span<byte> destination)
        {
            int written = 0;
            written += name.WriteString(destination[written..]);
            written += url.WriteString(destination[written..]);
            written += accounts.WriteCollection(destination[written..]);
            return written;
        }

        public override int ReadFrom(Span<byte> source)
        {
            int read = 0;
            read += source[read..].ReadString(out name);
            read += source[read..].ReadString(out url);
            read += source[read..].ReadCollection(accounts);
            return read;
        }

        public override int ComputeSize()
        {
            int size = 0;
            size += name.SizeOfString();
            size += url.SizeOfString();
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
            url = null;
            foreach (var account in accounts)
            {
                account.Dispose();
            }
            accounts.Clear();
        }
    }
}