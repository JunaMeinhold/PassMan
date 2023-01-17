// See https://aka.ms/new-console-template for more information

string path1 = args[0];
string path2 = args[1];

FileStream fs1 = File.OpenRead(path1);
FileStream fs2 = File.OpenRead(path2);

if (fs1.Length != fs2.Length)
{
    Console.WriteLine("File length is not the same.");
    return;
}

Span<byte> buffer1 = new byte[4096];
Span<byte> buffer2 = new byte[4096];

while (fs1.Position < fs1.Length)
{
    fs1.Read(buffer1);
    fs2.Read(buffer2);

    if (buffer1.SequenceCompareTo(buffer2) != 0)
    {
        Console.WriteLine("Byte wise compare failed, the files are not the same.");
        return;
    }
}

Console.WriteLine("Both files hold the same data");