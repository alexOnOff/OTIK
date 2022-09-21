

namespace OTIK_Lab1
{
    public class Program
    {
        public static void Main()
        {
            // сначала даем на вход файлики, которые надо будет добавить в архив, тут у меня их два
            List<string> FilePath = new List<string> { "C:\\Work\\Study\\OTIKDir\\example.txt", "C:\\Work\\Study\\OTIKDir\\pic.bmp" };

            // дальше указываю папку, где создать архив
            string pathToZip = "C:\\Work\\Study\\OTIK-Lab1\\";

            // создаю файловый менеджер, который в себе хранит все загруженные файлы
            FileManager fileManager = new FileManager(FilePath, pathToZip);
            fileManager.ReadFiles(); // считываю загруженные файлы

            // добавляю их в архив
            Zipper.Encode(fileManager);
        }
    }
}
