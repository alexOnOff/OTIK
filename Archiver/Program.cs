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
            int choice = Convert.ToInt32(Console.ReadLine());
            if (choice == 1)
                Zipper.Encode(directory);
            else if (choice == 2)
                Zipper.Decode(directory);
            else
                throw new ArgumentException("Invalid argument!");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
            Console.ResetColor();
        }
    }
}