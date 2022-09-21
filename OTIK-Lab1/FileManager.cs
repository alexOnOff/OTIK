using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTIK_Lab1
{
    public class FileManager
    {
        private List<string> FilePath { get; set; }

        public FileManager()
        {
            FilePath = new List<string>();
        }

        public FileManager(List<string> filePath)
        {
            this.FilePath = filePath;
        }

        public void ReadFiles()
        {
            foreach (var path in this.FilePath)
            {
                Console.WriteLine(path);

                FileInfo fileInfo = new FileInfo(path);
                Console.WriteLine(File.ReadAllText(path));

            }
        }
    }
}
