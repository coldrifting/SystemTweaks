using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;

namespace SystemTweaks.Utils;

public abstract class Processes
{
    public static void RunPowershellCommand(string command, bool redirectOutput = false)
    {
        LaunchProcess("powershell.exe", "-Command '" + command + "'", redirectOutput);
    }
    
    public static void LaunchProcess(string uri, string args, bool redirectOutput = false)
    {
        ProcessStartInfo psi = new()
        {
            UseShellExecute = false,
            CreateNoWindow = false,
            FileName = uri,
            Arguments = args,
            RedirectStandardOutput = redirectOutput
        };
        Process? proc = Process.Start(psi);

        proc?.WaitForExit();
    }
    
    public static bool ForkAsAdmin(string[] args)
    {
        string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string wt =  Path.Combine(appdata, @"Microsoft\WindowsApps\wt.exe");

        string terminal = File.Exists(wt) ? wt : "cmd.exe";
        string argsJoined = string.Join(' ', args);
        string argsFull = !File.Exists(wt) ? $"/C {argsJoined}" : argsJoined;
        
        string currentProcessPath = Environment.ProcessPath ?? AppContext.BaseDirectory;
        ProcessStartInfo startInfo = new()
        {
            UseShellExecute = true,
            Verb = "runas",
            FileName = terminal,
            Arguments = $"{currentProcessPath} {argsFull}"
        };

        try
        {
            Process.Start(startInfo);
            Console.WriteLine("Process restarted with elevated privileges.");
            return true;
        }
        catch (Win32Exception ex)
        {
            // Error code 1223 indicates the operation was canceled by the user.
            if (ex.NativeErrorCode == 1223)
            {
                return false;
            }

            throw;
        }
    }
    
    public static bool IsCurrentProcessElevated()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}