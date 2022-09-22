namespace Archiver;

public static class Program
{
    public static void Main()
    {
        Zipper.Encode(Directory.GetCurrentDirectory());
        Zipper.Decode(Directory.GetCurrentDirectory());
    }
}