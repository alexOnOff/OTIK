using System.Text;

namespace OTIK_Lab1;

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
        string binaryFilePath = fileManager.PathToZip + "zip.nkvd"; // путь до нового архива
        using (BinaryWriter writer = new BinaryWriter(File.Open(binaryFilePath, FileMode.OpenOrCreate))) // открываю бинарник на запись и пишу
        {
            // это хедер
            writer.Write(_signature);
            writer.Write(_version);
            writer.Write(_subVersion);
            writer.Write(_codingByte);
            writer.Write(_addByte);

            // это бади
            foreach(var file in fileManager.FileInfos)
            {
                
                if (file.Exists)
                {
                    byte[] nameBytes = Encoding.UTF8.GetBytes(file.Name);
                    byte[] contentBytes = File.ReadAllBytes(file.FullName);

                    writer.Write((int)file.Name.Length);             // пишу длину названия (тут надо пофиксить, чтобы записывался ровно один байт)
                    writer.Write(nameBytes);                    // само название
                    writer.Write((int)contentBytes.Length);     // пишу длину файла (тут тоже надо записать ровно 4 байта)
                    writer.Write(contentBytes);                 // сам файл
                }
            }

            Console.WriteLine("File has been written");
        }
    }

    public static void Decode(FileManager fileManager)
    {
        string binaryFylePath = fileManager.PathToZip + "zip.nkvd";
        string fileName;
        byte[] fileByteArray;

        using (BinaryReader binaryReader = new BinaryReader(File.Open(binaryFylePath,FileMode.Open)))
        {
            byte[] fileSignature = binaryReader.ReadBytes(_signature.Length);
            if (!fileSignature.SequenceEqual(_signature))
            {
                Console.WriteLine("Input file has incorrect signature!");
                return;
            }


            binaryReader.BaseStream.Position = 8;

            while(true)
            {
                int fileNameLen = binaryReader.ReadInt32();
                fileName = new string(binaryReader.ReadChars(fileNameLen));
                int fileLen = binaryReader.ReadInt32();
                fileByteArray = binaryReader.ReadBytes(fileLen);
                // Console.WriteLine(fileByteArray);

                try
                {
                    // Create the file, or overwrite if the file exists.
                    fileName = fileManager.PathToZip + fileName;
                    Console.WriteLine(fileName);
                    using (FileStream fs = File.Create(fileName))
                    {

                        fs.Write(fileByteArray, 0, fileLen);
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                if (binaryReader.BaseStream.Position == binaryReader.BaseStream.Length)
                    break;

            }
        }
    }
}
