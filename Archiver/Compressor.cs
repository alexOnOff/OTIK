using System.Text;
using static Archiver.BytesManager;

namespace Archiver;

public static class Compressor
{
    internal static List<byte> CompressUsingRle(in IEnumerable<byte> bytes)
    {
        throw new NotImplementedException();
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