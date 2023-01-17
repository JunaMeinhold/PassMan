namespace PassMan.Core
{
    using System;
    using System.Security;

    public class Account : VaultItem
    {
        private string? name;
        private string? login;
        private SecureString? password;
        private string? comment;

        public Account(string name, string login, string password, string comment)
        {
            this.name = name;
            this.login = login;
            this.password = password.ConvertToSecureString();
            this.comment = comment;
        }

        public Account()
        {
        }

        public string? Name
        {
            get => name;
            set
            {
                name = value;
                NotifyPropertyChanged();
            }
        }

        public string? Login
        { get => login; set { login = value; NotifyPropertyChanged(); } }

        public SecureString? Password
        { get => password; set { password = value; NotifyPropertyChanged(); } }

        public string? Comment
        { get => comment; set { comment = value; NotifyPropertyChanged(); } }

        public override string Type => nameof(Account);

        public override int ReadFrom(Span<byte> source)
        {
            int read = 0;
            read += source[read..].ReadString(out name);
            read += source[read..].ReadString(out login);
            read += source[read..].ReadSecureString(out password);
            read += source[read..].ReadString(out comment);
            return read;
        }

        public override int WriteTo(Span<byte> destination)
        {
            int written = 0;
            written += name.WriteString(destination[written..]);
            written += login.WriteString(destination[written..]);
            written += password.WriteSecureString(destination[written..]);
            written += comment.WriteString(destination[written..]);
            return written;
        }

        public override int ComputeSize()
        {
            int size = 0;
            size += name.SizeOfString();
            size += login.SizeOfString();
            size += password.SizeOfSecureString();
            size += comment.SizeOfString();
            return size;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            name = null;
            login = null;
            password?.Dispose();
            password = null;
            comment = null;
        }
    }
}