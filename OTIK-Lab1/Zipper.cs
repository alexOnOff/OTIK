using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTIK_Lab1
{
    public static class Zipper
    {
        private static byte[] _signature = new byte[] { 0x6e , 0x6b, 0x76, 0x64 };
        private static byte _version = 0x31;
        private static byte _subVersion = 0x31;
        private static byte _codingByte = 0x30;
        private static byte _addByte = 0x30;

        public static void Encode(FileManager fileManager)
        {
            string binaryFylePath = fileManager.PathToZip + "zip.nkvd";
            using (BinaryWriter writer = new BinaryWriter(File.Open(binaryFylePath, FileMode.OpenOrCreate)))
            {
                writer.Write(_signature);
                writer.Write(_version);
                writer.Write(_subVersion);
                writer.Write(_codingByte);
                writer.Write(_addByte);

                foreach(var file in fileManager.Files)
                {
                    if(file.Exists)
                    {
                        byte[] nameBytes = Encoding.UTF8.GetBytes(file.Name);
                        byte[] contentBytes = File.ReadAllBytes(file.FullName);

                        writer.Write(file.Name.Length);
                        writer.Write(nameBytes);
                        writer.Write((int)contentBytes.Length);
                        writer.Write(contentBytes);
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
