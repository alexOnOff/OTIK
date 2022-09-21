

namespace OTIK_Lab1
{
    public class Program
    {
        public static void Main()
        {
            // сначала даем на вход файлики, которые надо будет добавить в архив, тут у меня их два
            List<string> FilePath = new List<string> { "D:\\Study\\ОТИК\\example.txt" };//, "D:\\Study\\ОТИК\\pic.bmp" };

            // дальше указываю папку, где создать архив
            string pathToZip = "D:\\Study\\ОТИК\\";

            // создаю файловый менеджер, который в себе хранит все загруженные файлы
            FileManager fileManager = new FileManager(FilePath, pathToZip);
           // fileManager.ReadFiles(); // считываю загруженные файлы

            // добавляю их в архив
            //Zipper.Encode(fileManager);
            Zipper.Decode(fileManager);
        }
    }
}
