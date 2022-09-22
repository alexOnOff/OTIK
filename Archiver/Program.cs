namespace OTIK_Lab1;

public static class Program
{
    public static void Main()
    {
        Zipper.Encode(Directory.GetCurrentDirectory());
        Zipper.Decode(Directory.GetCurrentDirectory());
    }
}