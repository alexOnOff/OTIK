using System.Text;
using static Archiver.BytesManager;

namespace Archiver;

public class Zipper
{
    // Archive info
    private const string ArchiveExtension = "nkvd";
    private const string ArchiveName = $"archive.{ArchiveExtension}";

    //  Archive header fields
    private static readonly byte[] Signature = { 0x6e, 0x6b, 0x76, 0x64 };
    private const byte Version = 0x33;
    private const byte CodingByteWithoutCompressing = 0x30;
    private const byte CodingByteUsedRle = 0x31;
    private const byte CodingByteUsedShannon = 0x32;
    private const byte CodingByteUsedRleAndShannon = 0x33;
    
    private const byte AddByte = 0x30;

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
        // Delete array if it's already exists
        if (File.Exists(_archivePath))
        {
            File.Delete(_archivePath);
        }
        
        // writer for archiver
        using var writer = new BinaryWriter(File.Open(_archivePath, FileMode.OpenOrCreate));
        
        // write header
        writer.Write(Signature);
        writer.Write(Version);
        
        // get bytes of files to be encode
        var bytes = new List<byte>();
        ProcessDirectory(_filesDirectory, ref bytes);
        
        // TODO: compress using only RLE
        List<byte> bytesCompressedByRle = null!;
        
        // compress bytes using only Shannon code
        var bytesCompressedByShannon = CompressBytesUsingShannon(in bytes);

        // compress bytes after RLE with Shannon code
        var bytesCompressedByRleAndShanon = CompressBytesUsingShannon(in bytesCompressedByRle);
        
        // length
        var direct = bytes.Count;
        var rle = bytesCompressedByRle.Count;
        var shannon = bytesCompressedByShannon.Count;
        var rleAndShannon = bytesCompressedByRleAndShanon.Count;

        if (direct >= rle && direct >= shannon && direct >= rleAndShannon)
        {
            // direct bytes encoding
            
            writer.Write(AddByte);
            bytes.ForEach(b => writer.Write(b));
            Console.WriteLine("Encode: direct bytes encoding");
        }
        else if (rle >= direct && rle >= shannon && rle >= rleAndShannon)
        {
            // using only RLE
            
            writer.Write(AddByte);
            bytesCompressedByRle.ForEach(b => writer.Write(b));
            Console.WriteLine("Encode: using only RLE");
        }
        else if (shannon >= direct && shannon >= rle && shannon >= rleAndShannon)
        {
            // using only Shannon code
            
            writer.Write(AddByte);
            bytesCompressedByShannon.ForEach(b => writer.Write(b));
            Console.WriteLine("Encode: using only Shannon code");
        }
        else if (rleAndShannon >= direct && rleAndShannon >= rle && rleAndShannon >= shannon)
        {
            // using RLE and Shannon code (after RLE)
            
            writer.Write(AddByte);
            bytesCompressedByRleAndShanon.ForEach(b => writer.Write(b));
            Console.WriteLine("Encode: using RLE and Shannon code (Shannon after RLE)");
        }
    }
    
    public void CompressUsingRle()
    {
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
            Console.WriteLine("Encode: using Shannon code");
            
            var decodedBytes = DecodeFilesWithCompressing(in binaryReader);
            using var ms = new MemoryStream(decodedBytes.ToArray());
            using var reader = new BinaryReader(ms);
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
        else // without compressing
        {
            Console.WriteLine("Encode: using direct bytes writing");
            
            DecodeFilesWithoutCompressing(in binaryReader);
        }
    }

    private static List<byte> DecodeFilesWithCompressing(in BinaryReader reader)
    {
        var codes = GetCodesForBytes(in reader);

        var compressedBytes = reader.ReadBytes(Convert.ToInt32(reader.BaseStream.Length));

        var bitesString = GetBitesStringFromByteArray(compressedBytes);

        string substr;
        var start = 0;
        var len = 1;
        List<byte> decodedBytes = new();
        while (start < bitesString.Length)
        {
            substr = bitesString.Substring(start, len);
            var filteredCodes = codes.Keys.Where(code => code.StartsWith(substr)).ToList();

            if (filteredCodes.Count == 0)
            {
                throw new DecodingException($"No one code starts with {substr}");
            }

            if (filteredCodes.Count == 1 && filteredCodes.Contains(substr))
            {
                var code = codes[filteredCodes[0]];
                decodedBytes.Add(Convert.ToByte(code));

                start += filteredCodes[0].Length;
                len = 0;
            }

            len++;
        }

        return decodedBytes;
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