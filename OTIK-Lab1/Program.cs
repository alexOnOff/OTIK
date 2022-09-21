

namespace OTIK_Lab1
{
    public class Program
    {
        public static void Main()
        {
            List<string> FilePath = new List<string> { "C:\\Work\\Study\\OTIKDir\\example.txt", "C:\\Work\\Study\\OTIKDir\\pic.bmp" };
            FileManager fileManager = new FileManager(FilePath);
            fileManager.ReadFiles();
        }
    }
}
