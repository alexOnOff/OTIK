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
        if (File.Exists(_archivePath))
        {
            File.Delete(_archivePath);
        }
        using var writer = new BinaryWriter(File.Open(_archivePath, FileMode.OpenOrCreate));
        
        // write header
        writer.Write(Signature);
        writer.Write(Version);

        var bytes = new List<byte>();
        ProcessDirectory(_filesDirectory, ref bytes);
        Console.WriteLine("Original bytes: ");
        foreach (var b in bytes)
        {
            Console.Write(b + " ");
        }

        Console.WriteLine();
        
        var compressedBytes = CompressBytes(in bytes);
        Console.WriteLine("Compressed bytes:");
        foreach (var b in compressedBytes)
        {
            Console.Write(b + " ");
        }

        var abc = GetBitesStringFromByteArray(compressedBytes);
        Console.WriteLine(abc);
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

    private static List<byte> CompressBytes(in List<byte> bytes)
    {
        List<byte> compressBytes = new();
        StringBuilder fileBitArrayString = new(16 * bytes.Count);

        var bytesDict = FileManager.GetEntriesCount(bytes);
        var bytesProbabilityDict = FileManager.GetEntriesProbability(bytesDict);
        bytesProbabilityDict = FileManager.SortDictionary(bytesProbabilityDict, SortType.ByEntries);
        double probabilitySum = 0;
        var bytesProbabilityDictSum = new Dictionary<byte, string>();
        compressBytes.Add(ConvertOneByteIntToInt(bytesProbabilityDict.Count)); // количество символов в словаре

        foreach (var byteEntry in bytesProbabilityDict)
        {
            var y = BitConverter.DoubleToInt64Bits(probabilitySum);
            var length = GetLenghtOfBinary(byteEntry.Value);
            string binary = Convert.ToString(y, 2);

            if (binary.Length > 24)
            {
                binary = binary.Substring(8, length);
            }

            bytesProbabilityDictSum.Add(byteEntry.Key, binary);
            probabilitySum += byteEntry.Value;

            compressBytes.Add(byteEntry.Key);
            byte b = ConvertOneByteIntToInt(binary.Length);
            compressBytes.Add(ConvertOneByteIntToInt(binary.Length)); // длина кода 

            byte[] symbolCode = ConvertStringBitsToByteArray(binary);

            compressBytes.AddRange(symbolCode);
        }

        foreach (var byteEntry in bytes)
        {
            fileBitArrayString.Append(bytesProbabilityDictSum[byteEntry]);
        }

        byte[] fileByteArray = ConvertStringBitsToByteArray(fileBitArrayString.ToString());

        compressBytes.AddRange(fileByteArray);

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
            var decodedBytes = DecodeFilesWithCompressing(in binaryReader);
            foreach (var b in decodedBytes)
            {
                Console.Write(b + " ");
            }
        }
        else // without compressing
        {
            DecodeFilesWithoutCompressing(in binaryReader);
        }
    }

    private static List<byte> DecodeFilesWithCompressing(in BinaryReader reader)
    {
        var codes = GetCodesForBytes(in reader);

        var compressedBytes = reader.ReadBytes(Convert.ToInt32(reader.BaseStream.Length));
        
        var bitesString = GetBitesStringFromByteArray(compressedBytes);

        Console.WriteLine(bitesString);
        
        var substr = "";
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
            if (filteredCodes.Count == 1)
            {
                var code = codes[filteredCodes[0]];
                decodedBytes.Add(Convert.ToByte(code));
                foreach (var b in decodedBytes)
                {       
                    Console.Write(b + " ");
                }
                start += filteredCodes[0].Length;
                len = 1;
            }

            len++;
        }

        return decodedBytes;
    }

    private static string GetBitesStringFromByteArray(IEnumerable<byte> bytes) 
    {
        StringBuilder sb = new();

        foreach (var b in bytes)
        {
            var bites = GetBites(b);
            sb.Append(string.Join("", bites.Select(bit => bit ? "1" : "0")));
        }

        return sb.ToString();
    }

    private static Dictionary<string, string> GetCodesForBytes(in BinaryReader reader)
    {
        Dictionary<string, string> codes = new();

        var dictLen = reader.ReadByte() + 1;
        for (var i = 0; i < dictLen; i++)
        {
            var symbol = reader.ReadByte();
            Console.Write($"{symbol} - ");
            var codeLen = reader.ReadByte() + 1;
            var codeBytes = reader.ReadBytes((int)Math.Ceiling(codeLen / 8d));
            var str = "";
            foreach (var b in codeBytes)
            {
                var res = GetBites(b).Select(bit => bit ? "1" : "0");
                str += string.Join("", res);
            }

            if (str == "00000000") str = "0";
            Console.WriteLine(str);
            var tempSymbol = symbol.ToString();
            codes[str] = tempSymbol == "0" ? tempSymbol : tempSymbol.Replace("0", "!");
        }

        return codes;
    }

    private static bool[] GetBites(byte b)
    {
        var str = Convert.ToString(b, 2);
        return str.ToArray().Select(c => c == '1').ToArray();
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
        {
            n++;
        }
        
        return n;
    }
    
    private static byte ConvertOneByteIntToInt(int oneByteNumber)
    {
        return Convert.ToByte((oneByteNumber - 1).ToString());
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