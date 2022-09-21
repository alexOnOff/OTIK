using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTIK_Lab1
{
    public class FileManager
    {
        public List<string> FilePath;// массив путей до файлов
        public List<FileInfo> Files; // массив "информации" файлов
        public string PathToZip;     // тут понятно

        public FileManager()
        {
            FilePath = new List<string>();
            PathToZip = string.Empty;
            Files = new List<FileInfo>();
        }

        public FileManager(List<string> filePath, string pathToZip)
        {
            FilePath = filePath;
            PathToZip = pathToZip;
            Files = new List<FileInfo>();
        }

        public void ReadFiles()
        {
            //перебираю все путя и добавляю в массив fileInfo все что есть
            foreach (var path in this.FilePath)
            {
                FileInfo fileInfo = new FileInfo(path);

                if(fileInfo.Exists)
                {
                    Files.Add(fileInfo);
                }
            }
        }
    }
}
