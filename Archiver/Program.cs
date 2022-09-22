namespace Archiver;

public static class Program
{
    private static readonly string directory = Path.GetFullPath(Directory.GetCurrentDirectory() + @"../../../../");

    public static void Main()
    {
        Zipper.Encode(directory);
        //Zipper.Decode(directory);
    }
}