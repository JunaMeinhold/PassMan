namespace PassMan.Core
{
    using System;

    public class Note : VaultItem
    {
        private string? name;
        private string? text;

        public Note(string name, string text)
        {
            this.name = name;
            this.text = text;
        }

        public Note(string name)
        {
            this.name = name;
            text = string.Empty;
        }

        public Note()
        {
        }

        public string? Name
        { get => name; set { name = value; NotifyPropertyChanged(); } }

        public string? Text
        { get => text; set { text = value; NotifyPropertyChanged(); } }

        public override string Type => "Note";

        public override int WriteTo(Span<byte> destination)
        {
            int writtenBytes = 0;
            writtenBytes += name.WriteString(destination[writtenBytes..]);
            writtenBytes += text.WriteString(destination[writtenBytes..]);
            return writtenBytes;
        }

        public override int ReadFrom(Span<byte> source)
        {
            int readBytes = 0;
            readBytes += source[readBytes..].ReadString(out name);
            readBytes += source[readBytes..].ReadString(out text);
            return readBytes;
        }

        public override int ComputeSize()
        {
            int size = 0;
            size += name.SizeOfString();
            size += text.SizeOfString();
            return size;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            name = null;
            text = null;
        }
    }
}