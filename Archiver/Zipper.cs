using System.Text;

namespace Archiver;

public class Zipper
{
    // Archive info
    private const string ArchiveExtension = "nkvd";
    private const string ArchiveName = $"archive.{ArchiveExtension}";

    //  Archive header fields
    private static readonly byte[] Signature = { 0x6e, 0x6b, 0x76, 0x64 };
    private const byte Version = 0x32;
    private const byte CodingByteWithoutCompressing = 0x30;
    private const byte CodingByteWithCompressing = 0x31;
    private const byte AddByte = 0x30;

    private static readonly int ArchiveBytesOffset = Signature.Length + 4;

    private readonly string _directory;
    private readonly string _archivePath;
    private readonly string _filesDirectory;
    private readonly string _encodedFilesPath;

    public Zipper(string directory)
    {
        _directory = directory;
        _archivePath = Path.Combine(directory, ArchiveName);
        _filesDirectory =  Path.Combine(_directory, "files");
        _encodedFilesPath = Path.Combine(_directory, "decodedFiles/");
    }

    public void Encode()
    {
        using var writer = new BinaryWriter(File.Open(_archivePath, FileMode.OpenOrCreate));
        
        // write header
        writer.Write(Signature);
        writer.Write(Version);

        var bytes = new List<byte>();
        ProcessDirectory(_filesDirectory, ref bytes);

        var compressedBytes = CompressBytes(in bytes);

        if (compressedBytes.Count < bytes.Count) // using compressing
        {
            // add rest part of header
            writer.Write(CodingByteWithCompressing);
            writer.Write(AddByte);

            // write content
            compressedBytes.ForEach(b => writer.Write(b));
        }
        else // NOT using compressing
        {
            // add rest part of header
            writer.Write(CodingByteWithoutCompressing);
            writer.Write(AddByte);

            // write content
            bytes.ForEach((b => writer.Write(b)));
            
        }
    }

    private List<byte> CompressBytes(in List<byte> bytes)
    {
        // TODO: compressing table should be inside bytes array
        throw new NotImplementedException();
    }

    public void Decode()
    {
        // Check if archive exists
        var archivePath = Path.Combine(_directory, ArchiveName);
        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException($"Archive not found at path: {archivePath}");
        }

        // Check signature
        using var binaryReader = new BinaryReader(File.Open(archivePath, FileMode.Open));
        var fileSignature = binaryReader.ReadBytes(Signature.Length);
        if (!fileSignature.SequenceEqual(Signature))
        {
            throw new FormatException("Input file has incorrect signature!");
        }
        
        // Prepare folder for decoded files
        if (!Directory.Exists(_encodedFilesPath))
        {
            Directory.CreateDirectory(_encodedFilesPath);
        }

        if (Directory.GetFiles(_encodedFilesPath).Length != 0)
        {
            Directory.Delete(_encodedFilesPath, true);
            Directory.CreateDirectory(_encodedFilesPath);
        }

        var version = binaryReader.ReadByte();
        if (version != 0x32)
            throw new NotSupportedException($"Version ${version} not supported");

        var compressingCode = binaryReader.ReadByte();
        _ = binaryReader.ReadByte(); // skip AddByte
        
        if (compressingCode == CodingByteWithCompressing) // with compressing
        {
            throw new NotImplementedException();
        }
        else // without compressing
        {
            DecodeFilesWithoutCompressing(in binaryReader);
        }
    }

    private void DecodeFilesWithoutCompressing(in BinaryReader reader)
    {
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var fileNameLen = reader.ReadInt32();
            var fileName = new string(reader.ReadChars(fileNameLen));

            var fileLen = reader.ReadInt32();
            var fileByteArray = reader.ReadBytes(fileLen);

            var filePath = Path.Combine(_encodedFilesPath, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            using var fs = File.Create(filePath);
            fs.Write(fileByteArray, 0, fileLen);
        }
    }

    private void ProcessDirectory(string path, ref List<byte> bytes)
    {
        var files = Directory.GetFiles(path);
        foreach (var file in files)
        {
            ProcessFile(new FileInfo(file), ref bytes);
        }

        var subDirectories = Directory.GetDirectories(path);
        foreach (var subDirectory in subDirectories)
        {
            ProcessDirectory(subDirectory, ref bytes);
        }
    }

    private void ProcessFile(FileInfo fileInfo, ref List<byte> bytes)
    {
        var fileName = fileInfo.FullName.Replace(_filesDirectory + Path.DirectorySeparatorChar, "");
        
        // Skip MacOS system files
        if (fileName.Contains(".DS_Store"))
            return;
        
        var nameBytes = Encoding.UTF8.GetBytes(fileName);
        bytes.AddRange(BitConverter.GetBytes(fileName.Length));
        bytes.AddRange(nameBytes);
        
        var contentBytes = File.ReadAllBytes(fileInfo.FullName);
        bytes.AddRange(BitConverter.GetBytes(contentBytes.Length));
        bytes.AddRange(contentBytes);
    }
}