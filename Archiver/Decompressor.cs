using static Archiver.BytesManager;

namespace Archiver;

public static class Decompressor
{
    internal static List<byte> DecompressUsingRle(IEnumerable<byte> enumerableBytes)
    {
        var bytes = enumerableBytes.ToList();
        
        var decodedBytes = new List<byte>();
        
        for (var i = 0; i < bytes.Count;)
        {
            var flag = GetStringRepresentationOfByte(bytes[i]).PadLeft(8, '0');
            var cnt = Convert.ToByte(flag[1..], 2);
            switch (flag[0])
            {
                case '1': // repeating byte
                    var b = bytes[i + 1];
                    i++;
                    decodedBytes.AddRange(Enumerable.Repeat(b, cnt + 2));
                    break;
                case '0': // non repeating bytes
                    for (var j = 0; j < cnt + 1; j++)
                    {
                        var nextByte = bytes[i + 1];
                        decodedBytes.Add(nextByte);
                        i++;
                    }
                    break;
                default:
                    throw new AggregateException($"Binary string has illegal characters: {flag}");
            }

            i++;
        }

        return decodedBytes;
    }
    
    internal static List<byte> DecompressUsingShannon(in BinaryReader reader)
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
}