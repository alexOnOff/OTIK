using System.Collections;
using System.Text;
using System;

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
        List<byte> compressBytes = new();
        StringBuilder fileBitArrayString = new StringBuilder(16 * bytes.Count);

        var bytesDict = FileManager.GetEntriesCount(bytes);
        var bytesProbabylityDict = FileManager.GetEntriesProbability(bytesDict);
        bytesProbabylityDict = FileManager.SortDictionary(bytesProbabylityDict, SortType.ByEntries);
        double probabilytySum = 0;
        var bytesProbabylityDictSum = new Dictionary<byte, string>();

        compressBytes.Add(ConvertOneByteIntToInt(bytesProbabylityDict.Count)); // количество символов в словаре

        foreach (var byteEntry in bytesProbabylityDict)
        {
            var y = BitConverter.DoubleToInt64Bits(probabilytySum);
            var length = GetLenghtOfBinary(byteEntry.Value);
            string binary = Convert.ToString(y, 2);

            if (binary.Length > 24)
            {
                binary = binary.Substring(8, length);
            }

            // Console.WriteLine(binary);

            bytesProbabylityDictSum.Add(byteEntry.Key, binary);
            probabilytySum += byteEntry.Value;

            compressBytes.Add(byteEntry.Key);
            byte b = ConvertOneByteIntToInt(binary.Length);
            compressBytes.Add(ConvertOneByteIntToInt(binary.Length)); // длина кода 

            byte[] symbolCode = ConvertStringBitsToByteArray(binary);

            foreach(var byteCode in symbolCode)
            {
                compressBytes.Add(byteCode);
            }
        }

        foreach (var byteEntry in bytes)
            fileBitArrayString.Append(bytesProbabylityDictSum[byteEntry]);
            
       
        byte[] fileByteArray = ConvertStringBitsToByteArray(fileBitArrayString.ToString());

        foreach (var byteEntry in fileByteArray)
            compressBytes.Add(byteEntry);

        return compressBytes;
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
            DecodeFilesWithCompressing(in binaryReader);
        }
        else // without compressing
        {
            DecodeFilesWithoutCompressing(in binaryReader);
        }
    }

    private void DecodeFilesWithCompressing(in BinaryReader reader)
    {
        var dictLen = reader.ReadByte() + 1;
        Console.WriteLine(dictLen);
        for (var i = 0; i < dictLen; i++)
        {
            var symbol = reader.ReadByte();
            var codeLen = reader.ReadByte() + 1;
            var codeBytes = reader.ReadBytes((int)Math.Ceiling(codeLen / 8d));
            foreach (var b in codeBytes)
            {
                var res = GetBites(b);
                foreach (var re in res)
                {
                    Console.Write(re ? "1" : "0");
                }
            }
            
            Console.WriteLine();
        }
    }

    private static bool[] GetBites(byte b)
    {
        var str = Convert.ToString(b, 2);
        return str.PadLeft(8).ToArray().Select(c => c == '1').ToArray();
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

    private static int GetLenghtOfBinary(double probability)
    {
        if (probability >= 1)
            throw new Exception();

        int n = 1;
        while((double)1/n > probability && n < 24)
            n++;
        

        return n;
    }


    //FOR DECODE
    /*            long x = Convert.ToInt64(binary, 2);
            double y1 = BitConverter.Int64BitsToDouble(x);
            Console.WriteLine(y1);*/

    private static byte ConvertOneByteIntToInt(int oneByteNumber)
    {
        byte b = Convert.ToByte((oneByteNumber - 1).ToString());
        return b;
    }

    
    private static byte[] ConvertStringBitsToByteArray(string bits)
    {
        int numOfBytes = (int)Math.Ceiling(bits.Length / 8d);
        byte[] bytes = new byte[numOfBytes];
        bits = bits.PadRight(numOfBytes * 8, '0');

        for (var i = 0; i < numOfBytes; ++i)
        {
            bytes[i] = Convert.ToByte(bits.Substring(8 * i, 8), 2);
        }

        return bytes;
    }
}