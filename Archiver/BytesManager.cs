using System.Text;

namespace Archiver;

public static class BytesManager
{
    internal static List<byte> CompressBytesUsingShannon(in List<byte> bytes)
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

    private static bool[] GetBites(byte b)
    {
        var str = Convert.ToString(b, 2);
        return str.ToArray().Select(c => c == '1').ToArray();
    }

    internal static string GetBitesStringFromByteArray(IEnumerable<byte> bytes)
    {
        StringBuilder sb = new();

        foreach (var b in bytes)
        {
            var bites = GetBites(b);
            for (var i = 0; i < 8 - bites.Length; i++)
            {
                sb.Append('0');
            }

            sb.Append(string.Join("", bites.Select(bit => bit ? "1" : "0")));
        }

        return sb.ToString();
    }

    internal static Dictionary<string, string> GetCodesForBytes(in BinaryReader reader)
    {
        Dictionary<string, string> codes = new();

        var dictLen = reader.ReadByte() + 1;
        for (var i = 0; i < dictLen; i++)
        {
            var symbol = reader.ReadByte();
            var codeLen = reader.ReadByte() + 1;
            var codeBytes = reader.ReadBytes((int)Math.Ceiling(codeLen / 8d));
            var str = "";
            foreach (var b in codeBytes)
            {
                var res = GetBites(b).Select(bit => bit ? "1" : "0");
                var temp = string.Join("", res);
                temp = temp.PadLeft(8, '0');
                str += string.Join("", temp);
            }

            str = str.Substring(0, codeLen);
            if (str == "00000000") str = "0";
            var tempSymbol = symbol.ToString();
            codes[str] = tempSymbol;
        }

        return codes;
    }

    private static int GetLenghtOfBinary(double probability)
    {
        if (probability >= 1)
            throw new ArgumentException("Probability can't be greater that 1");

        int n = 1;
        while ((double)1 / n > probability && n < 24)
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