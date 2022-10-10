using System.Text;

namespace Archiver;

public static class BytesManager
{
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

    internal static int GetLenghtOfBinary(double probability)
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

    internal static byte ConvertOneByteIntToInt(int oneByteNumber)
    {
        return Convert.ToByte((oneByteNumber - 1).ToString());
    }

    internal static byte[] ConvertStringBitsToByteArray(string bits)
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

    internal static string GetStringRepresentationOfByte(byte b) => Convert.ToString(b, 2);
}