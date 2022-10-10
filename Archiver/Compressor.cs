using System.Text;
using static Archiver.BytesManager;

namespace Archiver;

public static class Compressor
{
    internal static List<byte> CompressUsingRle(in List<byte> bytes)
    {
        var compressedBytes = new List<byte>();
        var buffer = new List<byte>();
        
        var cnt = 1;
        for (var i = 0; i < bytes.Count - 1; i++)
        {
            if (bytes[i] == bytes[i + 1] && cnt <= 127)
            {
                cnt++;
            }
            else
            {
                if (buffer.Count >= 126) // have to add symbols to the array
                {
                    var f = Convert.ToString(buffer.Count - 1, 2).PadLeft(8, '0');
                    compressedBytes.Add(Convert.ToByte(f, 2));
                    compressedBytes.AddRange(buffer);
                    buffer.Clear();
                }
                
                if (cnt >= 2) // better using compressing
                {
                    if (buffer.Count != 0)
                    {

                        var f = Convert.ToString(buffer.Count - 1, 2).PadLeft(8, '0');
                        compressedBytes.Add(Convert.ToByte(f, 2));
                        compressedBytes.AddRange(buffer);
                        buffer.Clear();
                    }
                    
                    var flag = "1" + Convert.ToString(cnt - 2, 2).PadLeft(7, '0');
                    compressedBytes.Add(Convert.ToByte(flag, 2));
                    compressedBytes.Add(bytes[i]);
                }
                else // without compressing
                {
                    buffer.Add(bytes[i]);
                    
                }

                cnt = 1;
            }
        }

        if (buffer.Count != 0)
        {
            var f = Convert.ToString(buffer.Count - 1, 2).PadLeft(8, '0');
            compressedBytes.Add(Convert.ToByte(f, 2));
            compressedBytes.AddRange(buffer);
        }

        if (cnt != 1)
        {
            var flag = "1" + Convert.ToString(cnt - 2, 2).PadLeft(7, '0');
            compressedBytes.Add(Convert.ToByte(flag, 2));
            compressedBytes.Add(bytes.Last());
        }

        // Check last symbol
        if (bytes[^1] != bytes[^2])
        {
            compressedBytes.Add(0);
            compressedBytes.Add(bytes[^1]);
        }

        return compressedBytes;
    }
    
    internal static List<byte> CompressUsingShannon(in List<byte> bytes)
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

}