namespace Archiver;

public static class Program
{
    private static readonly string directory = Path.GetFullPath(Directory.GetCurrentDirectory() + @"../../../../");

    public static void Main()
    {
        try
        {
            Console.WriteLine("1 - Encode\n2 - Decode\n");
            Console.Write("Your choice: ");
            var choice = Convert.ToInt32(Console.ReadLine());
            switch (choice)
            {
                case 1:
                    Zipper.Encode(directory);
                    break;
                case 2:
                    Zipper.Decode(directory);
                    break;
                default:
                    throw new ArgumentException("Invalid argument!");
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
            Console.ResetColor();
        }
    }
}