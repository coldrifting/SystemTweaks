using System.Diagnostics;
using System.Text.RegularExpressions;
using SystemTweaks.Configurators;
using SystemTweaks.Utils;

namespace SystemTweaks;

public static partial class AppMain
{
    public static void Main(string[] args)
    {
        if (!Processes.IsCurrentProcessElevated())
        {
            Logger.Print("Requesting Admin Access...");
            Thread.Sleep(200);

            bool sucesfullyRelaunched = Processes.ForkAsAdmin(args);
            if (sucesfullyRelaunched)
            {
                return;
            }
            
            Logger.Error("Admin Access Denied. Exiting...", true);
            Thread.Sleep(1000);
        }
        else
        {
            (bool isVerbose, string? logFilePath) arguments = ReadArguments(args);
            using Logger l = new(arguments.isVerbose, arguments.logFilePath);
            if (Debugger.IsAttached)
            {
                AllTasks();
            }
            else
            {
                try
                {
                    AllTasks();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            if (Debugger.IsAttached)
            {
                return;
            }
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    private static void AllTasks()
    {
        string exePath = Environment.ProcessPath ?? @"C:\Config\SystemTweaks.exe";
        string configPath = EndingInExeRegex().Replace(exePath, ".json");
        
        Config? config = Config.ReadConfigFile(configPath);
        if (config == null)
        {
            Logger.Warn("Unable to read config file");
            return;
        }
        
        Applications.InstallAll(config);
        AppxPackages.RemoveAll(config.AppxPackagesToRemove);
        GeneralTweaks.RunAll();
        FileTypes.RunAll(config);
        
        External.RefreshShell();
    }

    private static (bool isVerbose, string? logFilePath) ReadArguments(string[] args)
    {
        bool isVerbose = false;
        string? logFilePath = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--log" || args[i] == "-l")
            {
                if (i + 1 < args.Length)
                {
                    logFilePath = args[i + 1];
                }
                else
                {
                    Logger.Warn("Logging requested but no log file path provided");
                    Thread.Sleep(2000);
                }
            }

            if (args[i] == "-v" || args[i] == "--verbose")
            {
                isVerbose = true;
            }
        }
        
        return new ValueTuple<bool, string?>(isVerbose, logFilePath);
    }

    [GeneratedRegex(@"\.exe$")]
    private static partial Regex EndingInExeRegex();
}