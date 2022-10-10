using static Archiver.BytesManager;

namespace Archiver;

public static class Decompressor
{
    internal static List<byte> DecompressUsingRle(IEnumerable<byte> bytes)
    {
        throw new NotImplementedException();
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