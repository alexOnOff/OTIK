namespace Archiver;

public class FileManager
{
    private readonly FileInfo _fileInfo;

    public FileManager(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found at path {path}");

        _fileInfo = new FileInfo(path);

    }
    
    private int FileLength => Convert.ToInt32(_fileInfo.Length);
    
    private static Dictionary<T, int> GetEntriesCount<T>(IEnumerable<T> symbols) where T : notnull
    {
        var stats = new Dictionary<T, int>();
        
        foreach (var symbol in symbols)
        {
            stats[symbol] = stats.GetValueOrDefault(symbol, 0) + 1;
        }

        return stats;
    }

    private static Dictionary<T, double> GetEntriesProbability<T>(Dictionary<T, int> stat) where T : notnull
    {
        var totalCounts = stat.Values.Sum();
        var symbolProbability = new Dictionary<T, double>();

        foreach (var kvp in stat)
        {
            symbolProbability[kvp.Key] = Convert.ToDouble(kvp.Value) / totalCounts;
        }

        return symbolProbability;
    }

    private static void PrintStat<T, TV>(Dictionary<T, TV> stat) where T : notnull
    {
        foreach (var kvp in stat)
        {
            Console.WriteLine($"{kvp.Key} - {kvp.Value}");
        }
    }

    private static Dictionary<T, TV> SortDictionary<T, TV>(Dictionary<T, TV> dict, SortType sortType) where T : notnull
    {
        return sortType switch
        {
            SortType.Alphabetic => dict.OrderBy(kvp => kvp.Key).ToDictionary(pair => pair.Key, pair => pair.Value),
            SortType.ByEntries => dict.OrderByDescending(kvp => kvp.Value).ToDictionary(pair => pair.Key, pair => pair.Value),
            _ => throw new ArgumentException($"SortType with value {sortType} not supported")
        };
    }

    private static Dictionary<T, double> GetInformationAmount<T>(Dictionary<T, double> dict) where T : notnull
    {
        return dict
            .Select(kvp => new KeyValuePair<T, double>(kvp.Key, -Math.Log2(kvp.Value)))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private void PrintStatsForBytes()
    {
        Console.WriteLine($"Total length (in bytes): {FileLength}");
        Console.WriteLine("Stat of each byte:");
        
        // Bytes count in alphabetic order
        Console.WriteLine("Bytes entries count (by alphabetic):");
        var bytes = File.ReadAllBytes(_fileInfo.FullName);
        var bytesEntriesCount = GetEntriesCount(bytes);
        var sortedBytesEntriesCount = SortDictionary(bytesEntriesCount, SortType.Alphabetic);
        PrintStat(sortedBytesEntriesCount);
            
        // Bytes count in frequency order
        Console.WriteLine("\nBytes entries count (by entries count):");
        sortedBytesEntriesCount = SortDictionary(bytesEntriesCount, SortType.ByEntries);
        PrintStat(sortedBytesEntriesCount);

        // Bytes entries probability in alphabetic order
        Console.WriteLine("\nBytes entries probability (by count):");
        var bytesEntriesProbability = GetEntriesProbability(bytesEntriesCount);
        var sortedBytesEntriesProbability = SortDictionary(bytesEntriesProbability, SortType.Alphabetic);
        PrintStat(sortedBytesEntriesProbability);
        
        // Bytes entries probability in frequency order
        Console.WriteLine("\nBytes entries probability (by count):");
        sortedBytesEntriesProbability = SortDictionary(bytesEntriesProbability, SortType.ByEntries);
        PrintStat(sortedBytesEntriesProbability);
        
        // Information amount for bytes in alphabetical order
        Console.WriteLine("\nInformation amount for bytes (in alphabetical order)");
        var informationAmount = GetInformationAmount(bytesEntriesProbability);
        var sortedInformationAmount = SortDictionary(informationAmount, SortType.Alphabetic);
        PrintStat(sortedInformationAmount);
        
        // Information amount for bytes in frequency order
        Console.WriteLine("\nInformation amount for bytes (in frequency order)");
        sortedInformationAmount = SortDictionary(informationAmount, SortType.ByEntries);
        PrintStat(sortedInformationAmount);

        // Total information amount
        var totalInformationAmountForBytes = informationAmount.Sum(kvp => kvp.Value);
        Console.WriteLine($"\nTotal information amount: {totalInformationAmountForBytes}");
    }

    private void PrintStatsForSymbols()
    {
        Console.WriteLine("\n\n\nTotal stats of each Unicode symbol:");
        using var sr = new StreamReader(_fileInfo.FullName);
        var text = sr.ReadToEnd();
        
        // Symbols entries count in alphabetic order
        var symbols = text.Select(c => c).ToList();
        var symbolsEntriesCount = GetEntriesCount(symbols);
        var sortedSymbolsEntriesCount = SortDictionary(symbolsEntriesCount, SortType.Alphabetic);
        Console.WriteLine("\nSymbols entries count (by alphabetic):");
        PrintStat(sortedSymbolsEntriesCount);
        
        // Symbols entries count in frequency order
        sortedSymbolsEntriesCount = SortDictionary(symbolsEntriesCount, SortType.ByEntries);
        Console.WriteLine("\nSymbols entries count (by frequency):");
        PrintStat(sortedSymbolsEntriesCount);
        
        // Symbols entries probability in alphabetical order
        Console.WriteLine("\nSymbols entries probability (by alphabetical)");
        var symbolsEntriesProbability = GetEntriesProbability(symbolsEntriesCount);
        var sortedSymbolsEntriesProbability = SortDictionary(symbolsEntriesProbability, SortType.Alphabetic);
        PrintStat(sortedSymbolsEntriesProbability);
        
        // Symbols entries probability in frequency order
        Console.WriteLine("\nSymbols entries probability (by frequency)");
        sortedSymbolsEntriesProbability = SortDictionary(symbolsEntriesProbability, SortType.ByEntries);
        PrintStat(sortedSymbolsEntriesProbability);
        
        // Information amount in alphabetical order
        Console.WriteLine("\nInformation amount for Unicode symbols (by alphabetical)");
        var symbolsInformationAmount = GetInformationAmount(symbolsEntriesProbability);
        var sortedSymbolsInformationAmount = SortDictionary(symbolsInformationAmount, SortType.Alphabetic);
        PrintStat(sortedSymbolsInformationAmount);
        
        // Information amount in frequency order
        Console.WriteLine("\nInformation amount for Unicode symbols (by frequency)");
        sortedSymbolsInformationAmount = SortDictionary(symbolsInformationAmount, SortType.ByEntries);
        PrintStat(sortedSymbolsInformationAmount);
        
        // Total information amount for symbols
        var totalInformationAmountForSymbols = symbolsInformationAmount.Sum(kvp => kvp.Value);
        Console.WriteLine($"\nTotal information amount for symbols {totalInformationAmountForSymbols}");
    }
    
    public void PrintAllStat()
    {
        PrintStatsForBytes();
        PrintStatsForSymbols();
    }
    
    public static void Task3(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found at path {path}");
        
        var bytes = File.ReadAllBytes(path);
        var bytesEntriesCount = GetEntriesCount(bytes);
        var bytesEntriesProbability = GetEntriesProbability(bytesEntriesCount);
        
        Console.WriteLine("Top symbols from all:");
        var topSymbolsFromAll = SortDictionary(bytesEntriesProbability, SortType.ByEntries)
            .Take(4)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        PrintStat(topSymbolsFromAll);

        Console.WriteLine("\nTop non printing symbols:");
        var topSymbolsFromNonLetters = SortDictionary(bytesEntriesProbability, SortType.ByEntries)
            .Where(kvp => kvp.Key < 32)
            .Take(4)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        PrintStat(topSymbolsFromNonLetters);
    }
}