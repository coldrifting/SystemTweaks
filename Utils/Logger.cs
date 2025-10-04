namespace SystemTweaks.Utils;

public class Logger : IDisposable
{
    private static Logger _instance = new(false, null);
    
    private readonly bool _isVerbose;
    private readonly StreamWriter? _fileWriter;
    
    public Logger(bool isVerbose, string? filePath)
    {
        _isVerbose = isVerbose;
        if (filePath is null)
        {
            _instance = this;
            return;
        }
        
        try
        {
            _fileWriter = new StreamWriter(filePath, false);
        }
        catch (Exception ex)
        {
            switch (ex)
            {
                case DirectoryNotFoundException:
                    PrintWithColor("Warning: Log file directory does not exist", ConsoleColor.Yellow);
                    break;
                default:
                    PrintWithColor("Warning: Could not open log file", ConsoleColor.Yellow);
                    PrintWithColor(ex.Message, ConsoleColor.Yellow);
                    break;
            }

            Thread.Sleep(2000);
        }

        _instance = this;
    }
    
    public void Dispose()
    {
        _fileWriter?.Close();
    }

    public static void Print(string msg)
    {
        _instance.PrintInstance(msg);
    }
    
    public static void Log(string msg, bool forceShow = false)
    {
        _instance.LogInstance(msg, forceShow);
    }

    public static void Warn(string msg, bool forceShow = false)
    {
        _instance.WarnInstance(msg, forceShow);
    }

    public static void Error(string msg, bool forceShow = false)
    {
        _instance.ErrorInstance(msg, forceShow);
    }
        
    
    private void PrintInstance(string msg)
    {
        Console.WriteLine(msg);
        AddToLog($"MSG: {msg}");
    }
        
    private void LogInstance(string msg, bool forceShow)
    {
        if (_isVerbose || forceShow)
        {
            PrintWithColor(msg,  ConsoleColor.Blue);
        }
        AddToLog($"INFO: {msg}");
    }
    
    private void WarnInstance(string msg, bool forceShow)
    {
        PrintWithColor(msg,  ConsoleColor.Yellow);
        AddToLog($"WARN: {msg}");
    }

    private void ErrorInstance(string msg, bool forceShow)
    {
        PrintWithColor(msg,  ConsoleColor.Red);
        AddToLog($"ERROR: {msg}");
    }
    
    private static void PrintWithColor(string msg, ConsoleColor color)
    {
        ConsoleColor defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(msg);
        Console.ForegroundColor = defaultColor;
    }
    
    private void AddToLog(string msg)
    {
        _fileWriter?.WriteLine(msg);
    }
}