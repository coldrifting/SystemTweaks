using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using SystemTweaks.Utils;

using RegistryAccess = SystemTweaks.Utils.RegistryAccess;

namespace SystemTweaks.Configurators;

public static class Applications
{
    private static readonly string AppDataDir = Environment.ExpandEnvironmentVariables("%AppData%");
    private static readonly string ProgFiles = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
    private static readonly string ProgFiles32 = Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%");

    public static void InstallAll(Config config)
    {
        Logger.Print("(Re)Installing selected WinGet Packages...");
        foreach (string packageName in config.WinGetPackagesToInstall)
        {
            InstallWinGetPackage(packageName);
        }
        
        Logger.Print("(Re)Installing Programs...");
        foreach (string appFolderPath in Directory.EnumerateDirectories(config.ProgramsToInstallDirectory))
        {
            string appFolderName = Path.GetFileName(appFolderPath);

            Dictionary<string, string> parameters = ReadIniFile(Path.Combine(appFolderPath, "Install.ini"));
            Dictionary<string, string> envs = ReadIniFile(Path.Combine(appFolderPath, "Env.ini"));
            
            if (!parameters.TryGetValue("key", out string? key))
            {
                continue;
            }
            
            bool freshInstall = !File.Exists(key);
            Logger.Print($"{appFolderName}...");

            // Run Installer
            if (parameters.TryGetValue("installer", out string? installer))
            {
                string installerPath = Path.Combine(appFolderPath, installer);
                parameters.TryGetValue("installerargs", out string? installerArgs);

                if (freshInstall)
                {
                    ProcessStartInfo startInfo = installerPath.EndsWith(".msi")
                        ? new ProcessStartInfo
                        {
                            UseShellExecute = false,
                            Verb = "runas",
                            FileName = "msiexec.exe",
                            Arguments = $"/i \"{installerPath}\" /passive"
                        }
                        : new ProcessStartInfo
                        {
                            UseShellExecute = false,
                            Verb = "runas",
                            FileName = installerPath,
                            Arguments = installerArgs ?? ""
                        };

                    Process? p= Process.Start(startInfo);
                    p?.WaitForExit();
                }
                
            }

            // Copy override files
            string folderAppdata = Path.Combine(appFolderPath, "AppData");
            string folderProgramFiles = Path.Combine(appFolderPath, "Program Files");
            string folderProgramFiles32 = Path.Combine(appFolderPath, "Program Files (x86)");
            
            if (Directory.Exists(folderAppdata) && freshInstall)
            {
                Copy(folderAppdata, AppDataDir, true);
            }

            parameters.TryGetValue("portable", out string? portable);
            bool isPortableApp = portable?.ToLowerInvariant() == "true";
            
            if (Directory.Exists(folderProgramFiles) && freshInstall)
            {
                Copy(folderProgramFiles, ProgFiles, true);
                if (isPortableApp)
                {
                    SetFinalFolderFullControl(folderProgramFiles, ProgFiles);
                }
            }
            
            if (Directory.Exists(folderProgramFiles32) && freshInstall)
            {
                Copy(folderProgramFiles32, ProgFiles32, true);
                if (isPortableApp)
                {
                    SetFinalFolderFullControl(folderProgramFiles32, ProgFiles32);
                }
            }

            // Add Enviornmental Variables
            foreach ((string envName, string envValue) in envs)
            {
                if (envName.Equals("PATH", StringComparison.InvariantCultureIgnoreCase))
                {
                    AddToPath(envValue);
                }
                else
                {
                    AddEnvVariable(envName.ToUpper(), envValue);
                }
            }

            // Run any desired post install scripts here
            if (File.Exists($@"{appFolderPath}\Install_After.bat"))
            {
                Process px = Process.Start($@"{appFolderPath}\Install_After.bat");
                px.WaitForExit();
            }
        }
    }

    // Helpers

    private static void InstallWinGetPackage(string packageName)
    {
        Logger.Print($"{packageName}...");
        Processes.LaunchProcess("winget.exe", $"install --id {packageName} --source winget");
    }
    
    private static void Copy(string sourceDirectory, string targetDirectory, bool copyContents = false)
    {
        DirectoryInfo diSource = new(sourceDirectory);
        DirectoryInfo diTarget = new(targetDirectory);
        
        if (copyContents)
        {
            foreach (DirectoryInfo diSourceSubDir in diSource.GetDirectories())
            {
                DirectoryInfo diTargetSubDir = new(Path.Combine(diTarget.FullName, diSourceSubDir.Name));
                CopyRecursive(diSourceSubDir, diTargetSubDir);
            }
        }
        else
        {
            CopyRecursive(diSource, diTarget);
        }
    }

    private static void CopyRecursive(DirectoryInfo source, DirectoryInfo target)
    {
        Directory.CreateDirectory(target.FullName);

        // Copy each file into the new directory.
        foreach (FileInfo fi in source.GetFiles())
        {
            //Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
            DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
            CopyRecursive(diSourceSubDir, nextTargetSubDir);
        }
    }

    private static void SetFolderFullControl(string folderPath)
    {
        DirectorySecurity sec = new DirectoryInfo(folderPath).GetAccessControl();
        // Using this instead of the "Everyone" string means we work on non-English systems.
        SecurityIdentifier everyone = new(WellKnownSidType.WorldSid, null);
        sec.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.Modify | FileSystemRights.Synchronize, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        new DirectoryInfo(folderPath).SetAccessControl(sec);
    }

    private static void SetFinalFolderFullControl(string folderPath, string finalFolderParent)
    {
        DirectoryInfo di = new(folderPath);
        foreach (DirectoryInfo d in di.EnumerateDirectories())
        {
            string finalFolderPath = Path.Combine(finalFolderParent, d.Name);
            SetFolderFullControl(finalFolderPath);
        }
    }
    
    private static void AddToPath(string pathToAppend)
    {
        object? path = RegistryAccess.GetValue(@"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Environment", "PATH");
        if (path is not string pathAsString) 
            return;

        string[] paths = pathAsString.Split(';');
        if (paths.Contains(pathToAppend))
            return;
        
        string newPath = pathAsString + ";" + pathToAppend;
        AddEnvVariable("PATH", newPath);
    }
    
    private static void AddEnvVariable(string variable, string value)
    {
        const string envPath = @"HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Environment";

        string? currentEnvValue = RegistryAccess.GetValue(envPath, variable)?.ToString();
        if (value == currentEnvValue)
        {
            return;
        }
        
        RegistryAccess.SetValue(envPath, variable, value);
    }
    
    private static Dictionary<string, string> ReadIniFile(string iniFilePath)
    {
        if (!File.Exists(iniFilePath))
        {
            return new Dictionary<string, string>();
        }

        string[] lines = File.ReadAllLines(iniFilePath);

        return lines.Select(line => line.Trim()
                .Split('=', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(keyAndValue => keyAndValue.Length == 2)
                .ToDictionary(keyAndValue => keyAndValue[0].ToLowerInvariant(), keyAndValue =>
                {
                    string value = keyAndValue[1].Trim();
                    if ((value.StartsWith('\'') && value.EndsWith('\'')) ||
                        (value.StartsWith('"') && value.EndsWith('"')))
                    {
                        return value[1..^1].Trim();
                    }
                    
                    return keyAndValue[1];
                });
    }
}