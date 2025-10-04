using SystemTweaks.Utils;

namespace SystemTweaks.Configurators;

public static class AppxPackages
{
    public static void RemoveAll(IList<string> appxPackagesToRemove)
    {
        Logger.Print("Uninstalling selected AppxPackages...");
        foreach (string package in appxPackagesToRemove)
        {
            Logger.Print($"{package}...");
            RemoveAppxPackage(package);
        }
    }

    private static void RemoveAppxPackage(string packageName)
    {
        Processes.RunPowershellCommand("Get-AppxPackage -Name ''" + packageName + "'' | Remove-AppxPackage", redirectOutput: true);
    }
}