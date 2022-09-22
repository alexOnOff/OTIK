namespace OTIK_Lab1;

public class FileManager
{
    private readonly List<string> _filePaths = new();// массив путей до файлов
    public List<FileInfo> FileInfos = new(); // массив "информации" файлов
    public string PathToZip;     // тут понятно

    public FileManager(List<string> filePath, string pathToZip)
    {
        _filePaths = filePath;
        PathToZip = pathToZip;
    }

    public void ReadFiles()
    {
        //перебираю все путя и добавляю в массив fileInfo все что есть
        foreach (var path in _filePaths)
        {
            FileInfo fileInfo = new FileInfo(path);

            if(fileInfo.Exists)
            {
                FileInfos.Add(fileInfo);
            }
        }
    }
}
