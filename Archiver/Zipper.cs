using System.Text;

namespace Archiver;

public static class Zipper
{
    // Archive info
    private const string ArchiveExtension = "nkvd";
    private const string ArchiveName = $"archive.{ArchiveExtension}";

    //  Archive header fields
    private static readonly byte[] Signature = { 0x6e, 0x6b, 0x76, 0x64 };
    private const byte Version = 0x31;
    private const byte SubVersion = 0x31;
    private const byte CodingByte = 0x30;
    private const byte AddByte = 0x30;

    private static readonly int ArchiveBytesOffset = Signature.Length + 4;

    private static string pathBase;

    public static void Encode(string directory)
    {
        var archivePath = Path.Combine(directory, ArchiveName);

        using var writer = new BinaryWriter(File.Open(archivePath, FileMode.OpenOrCreate));

        // write header
        writer.Write(Signature);
        writer.Write(Version);
        writer.Write(SubVersion);
        writer.Write(CodingByte);
        writer.Write(AddByte);

        var filesDirectory = Path.Combine(directory, "files");
        pathBase = filesDirectory;
        ProcessDirectory(filesDirectory, writer);
    }

    public static void Decode(string directory)
    {
        var encodedFilesPath = Path.Combine(directory, "decodedFiles/");

        if (!Directory.Exists(encodedFilesPath))
        {
            Directory.CreateDirectory(encodedFilesPath);
        }

        if (Directory.GetFiles(encodedFilesPath).Length != 0)
        {
            Directory.Delete(encodedFilesPath, true);
            Directory.CreateDirectory(encodedFilesPath);
        }

        var archivePath = Path.Combine(directory, ArchiveName);

        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException($"Archive not found at path: {archivePath}");
        }

        using var binaryReader = new BinaryReader(File.Open(archivePath, FileMode.Open));
        var fileSignature = binaryReader.ReadBytes(Signature.Length);
        if (!fileSignature.SequenceEqual(Signature))
        {
            throw new FormatException("Input file has incorrect signature!");
        }

        // TODO: read all field and check there validity
        binaryReader.BaseStream.Position = ArchiveBytesOffset;

        while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
        {
            var fileNameLen = binaryReader.ReadInt32();
            var fileName = new string(binaryReader.ReadChars(fileNameLen));

            var fileLen = binaryReader.ReadInt32();
            var fileByteArray = binaryReader.ReadBytes(fileLen);

            var filePath = Path.Combine(encodedFilesPath, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            using var fs = File.Create(filePath);
            fs.Write(fileByteArray, 0, fileLen);
        }
    }

    private static List<FileInfo> GetFiles(string directory)
    {
        var pathToFiles = Path.Combine(directory, "files/");
        return Directory
            .GetFiles(pathToFiles)
            .Select(file => new FileInfo(file))
            .ToList();
    }

    private static void ProcessDirectory(string path, BinaryWriter writer)
    {
        var files = Directory.GetFiles(path);
        foreach (var file in files)
        {
            //if (file.StartsWith('.')) continue;
            ProcessFile(new FileInfo(file), writer);
        }

        var subDirectories = Directory.GetDirectories(path);
        foreach (var subDirectory in subDirectories)
        {
            ProcessDirectory(subDirectory, writer);
        }
    }

    private static void ProcessFile(FileInfo fileInfo, BinaryWriter writer)
    {
        var fileName = fileInfo.FullName.Replace(pathBase + Path.DirectorySeparatorChar, "");

        // Ignore hide files, that start with dot
        var pattern = Path.DirectorySeparatorChar + ".";
        //if (fileName.Contains(pattern) || fileName.StartsWith('.')) return;

        byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);

        byte[] contentBytes = File.ReadAllBytes(fileInfo.FullName);

        writer.Write(fileName.Length);
        writer.Write(nameBytes);
        writer.Write(contentBytes.Length);
        writer.Write(contentBytes);
    }
}