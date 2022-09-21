

namespace OTIK_Lab1
{
    public class Program
    {
        public static void Main()
        {
            List<string> FilePath = new List<string> { "C:\\Work\\Study\\OTIKDir\\example.txt", "C:\\Work\\Study\\OTIKDir\\pic.bmp" };
            string pathToZip = "C:\\Work\\Study\\OTIK-Lab1\\";
            FileManager fileManager = new FileManager(FilePath, pathToZip);
            fileManager.ReadFiles();
            Zipper.Encode(fileManager);
        }
    }
}
