using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTIK_Lab1
{
    public static class Zipper
    {
        // это все первые 8 байт нашего хедера
        private static byte[] _signature = new byte[] { 0x6e , 0x6b, 0x76, 0x64 }; 
        private static byte _version = 0x31;
        private static byte _subVersion = 0x31;
        private static byte _codingByte = 0x30;
        private static byte _addByte = 0x30;

        public static void Encode(FileManager fileManager)
        {
            string binaryFylePath = fileManager.PathToZip + "zip.nkvd"; // путь до нового архива
            using (BinaryWriter writer = new BinaryWriter(File.Open(binaryFylePath, FileMode.OpenOrCreate))) // открываю бинарник на запись и пишу
            {
                // это хедер
                writer.Write(_signature);
                writer.Write(_version);
                writer.Write(_subVersion);
                writer.Write(_codingByte);
                writer.Write(_addByte);

                // это бади
                foreach(var file in fileManager.Files)
                {
                    if(file.Exists)
                    {
                        byte[] nameBytes = Encoding.UTF8.GetBytes(file.Name);
                        byte[] contentBytes = File.ReadAllBytes(file.FullName);

                        writer.Write(file.Name.Length);             // пишу длину названия (тут надо пофиксить, чтобы записывался ровно один байт)
                        writer.Write(nameBytes);                    // само название
                        writer.Write((int)contentBytes.Length);     // пишу длину файла (тут тоже надо записать ровно 4 байта)
                        writer.Write(contentBytes);                 // сам файл
                    }
                }

                Console.WriteLine("File has been written");
            }
        }

        public static void Decode()
        {
            
        }


    }
}
