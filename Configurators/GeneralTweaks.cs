using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using SystemTweaks.Utils;
using RegistryAccess = SystemTweaks.Utils.RegistryAccess;

namespace SystemTweaks.Configurators;

public static partial class GeneralTweaks
{
    private class Tweak(string enableMsg, Action enableAction)
    {
        public void Run()
        {
            Logger.Print($"{enableMsg}...");
            enableAction.Invoke();
        }
    }
    
    public static void RunAll()
    {
        Logger.Print("Installing shell configurations...");
        foreach (Tweak shellTweak in AllShellTweaks)
        {
            shellTweak.Run();
        }
    }

    private static readonly List<Tweak> AllShellTweaks =
    [
        new("Disabling Forced password Reset", DisableForcedPasswordReset),
        new("Disabling UserChoiceProtectionDriver (UCPD)", DisableUcpd),
        new("Disabling UserChoiceLatest System", DisableUserChoiceLatest),
        new("Hiding Open Powershell Window Here", HideOpenPowershellHere),
        new("Hiding Recycle Bin from Desktop", HideRecycleBinFromDesktop),
        new("Removing Visual C from Programs List", RemoveVisualC),
        new("Hiding Unwanted Folders", HideUnwantedFolders),
        new("Adding Restart Explorer item to desktop context menu", AddRestartExplorerToDesktopContextMenu),
        new("Removing AMD Settings Context Menu Item from Desktop", RemoveAmdSettingsFromDesktopContextMenu),
        new("Removing Visual Studio Context Menus", RemoveOpenWithVisualStudioFromContextMenus),
        new("Setting Inactive Title bar color", SetInactiveTitlebarColor),
        new("Disabling Windows 11 New Style Context Menu", DisableWin11NewContextMenu),
        new("Removing new Modern sharing menu", RemoveModernSharing),
        new("Removing Open in New Process Context Item", RemoveOpenInNewProcess),
        new("Moving Open in new Window to Extended Context Menu", MoveOpenInNewWindowToExtendedContext),
        new("Removing Open With from context Menu", RemoveOpenWithContext),
        new("Removing Restore Previous Version Tab and Context Item", RemoveRestorePreviousVersion),
        new("Removing Pin to Start Context Item", RemovePinToStart),
        new("Removing Pin to Taskbar Context Item", RemovePinToTaskbar),
        new("Removing Scan with Defender Context Item", RemoveScanWithDefenderContext),
        new("Removing Sent To Context Menu", RemoveSendToContext),
        new("Removing Troubleshoot Compat", RemoveTroubleshootCompat),
        new("Removing Cast To Device Context Item", RemoveCastToDeviceContext),
        new("Removing Give Access To Context Item", RemoveGiveAccessToContext),
        new("Removing Include In Libary Context Item", RemoveIncludeInLibraryContext),
        new("Removing Add to Favourites Context Item", RemoveAddToFavorites),
        new("Removing Copy As Path Context Item", RemoveCopyAsPath),
        new("Disabling New App Alert", DisableNewAppAlert),
        new("Moving Open in New Tab to Extended Context Menu", MoveOpenInNewTabToExtendedContext),
        new("Removing Pin to Home from Drives", RemovePinToHomeFromDrives),
        new("Moving Pin to Home on folders to Extended Context Menu", MovePinToHomeInFoldersToExtendedContextMenu),
        new("Disabling Regedit History", DisableRegeditHistory),
        new("Removing Ask Copilot from context menu", RemoveAskCopilotFromContextMenu),
        new("Aligning Taskbar icons to the left", AlignTaskbarToLeft),
        new("Enabling Explorer file extensions", ShowExplorerFileExtensions),
        new("Hiding Search from Taskbar", HideTaskbarSearch),
        new("Hiding Task View Button from Taskbar", HideTaskViewButton),
        new("Hiding News And Interests (Widgets)", HideNewsAndInterests)
    ];

    private static void RemoveVisualC()
    {
        string[] softwareFolders = [
            @"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", 
            @"HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        ];
        foreach (string softwareFolder in softwareFolders)
        {
            using RegistryKey allProgs64 = RegistryAccess.GetKey(softwareFolder);
            foreach (string progKeyString in allProgs64.GetSubKeyNames())
            {
                using RegistryKey? progKey = allProgs64.OpenSubKey(progKeyString, false);

                object? name = progKey?.GetValue("DisplayName");
                if (name is string title && VisualCRegex().IsMatch(title))
                {
                    RegistryAccess.SetValue(softwareFolder + "\\" + progKeyString, "SystemComponent", 1);
                }
            }
        }
    }

    [GeneratedRegex(@".*Visual C.*")]
    private static partial Regex VisualCRegex();
    
    private static void DisableForcedPasswordReset()
    {
        Processes.RunPowershellCommand("set-localuser -Name $env:Username -PasswordNeverExpires $true");
    }

    private static void DisableUcpd()
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = @"C:\Program Files\ViVeTool\ViVeTool.exe",
            ArgumentList = { "/disable", "/id:44860385" }
        })?.WaitForExit();
    }
    
    private static void DisableUserChoiceLatest()
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = @"C:\Program Files\ViVeTool\ViVeTool.exe",
            ArgumentList = { "/disable", "/id:43229420" }
        })?.WaitForExit();
        
        Process.Start(new ProcessStartInfo()
        {
            FileName = @"C:\Program Files\ViVeTool\ViVeTool.exe",
            ArgumentList = { "/disable", "/id:27623730" }
        })?.WaitForExit();
    }
    
    private static void HideUnwantedFolders()
    {
        Hide(@"C:\$WinREAgent");
        Hide(@"C:\PerfLogs");
        Hide(@"C:\AMD");
        Hide($@"C:\Users\{Environment.UserName}\.dotnet");
        Hide($@"C:\Users\{Environment.UserName}\.nuget");
        Hide($@"C:\Users\{Environment.UserName}\.templateengine");
        Hide($@"C:\Users\{Environment.UserName}\AppData");
        Hide($@"C:\Users\{Environment.UserName}\Pictures\Camera Roll");
        Hide($@"C:\Users\{Environment.UserName}\Pictures\Saved Pictures");
        Hide($@"C:\Users\{Environment.UserName}\Videos\Captures");
    }
    
    private static void Hide(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                DirectoryInfo di = new(path);
                di.Attributes |= FileAttributes.Hidden;
                di.Attributes |= FileAttributes.System;
                return;
            }

            if (!File.Exists(path))
                return;

            FileInfo fi = new(path);
            fi.Attributes |= FileAttributes.Hidden;
            fi.Attributes |= FileAttributes.System;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static void AddRestartExplorerToDesktopContextMenu()
    {
        const string path = $@"{RegistryAccess.Classes}\DesktopBackground\Shell\RestartExplorer";
        RegistryAccess.SetValue(path, "", "Restart Explorer");
        RegistryAccess.SetValue(path, "Extended", "");
        RegistryAccess.SetValue(path, "Icon", "\"%SystemRoot%\\System32\\Shell32.dll\",-161");
        RegistryAccess.SetValue(path, "Position", "Bottom");
        RegistryAccess.SetValue(path, "SeparatorBefore", "");
        RegistryAccess.SetValue($"{path}\\Command", "", "cmd /c \"taskkill /f /im explorer.exe && start C:\\Windows\\explorer.exe\"");
    }

    private static void RemoveAmdSettingsFromDesktopContextMenu()
    {
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\Directory\Background\shellex\ContextMenuHandlers\ACE", "", "--{5E2121EE-0300-11D4-8D3B-444553540000}");
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked", "{6767B3BC-8FF7-11EC-B909-0242AC120002}", "");
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked", "{FDADFEE3-02D1-4E7C-A511-380F4C98D73B}", "");
    }

    private static void RemoveAskCopilotFromContextMenu()
    {
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked", "{CB3B0003-8088-4EDE-8769-8B354AB2FF8C}", "Ask Copilot");
    }

    private static void RemoveOpenWithVisualStudioFromContextMenus()
    {
        RegistryAccess.DeleteKey($@"{RegistryAccess.Classes}\Directory\Background\Shell\AnyCode\command");
        RegistryAccess.DeleteKey($@"{RegistryAccess.Classes}\Directory\Background\Shell\AnyCode");
        RegistryAccess.DeleteKey($@"{RegistryAccess.Classes}\Directory\Shell\AnyCode\command");
        RegistryAccess.DeleteKey($@"{RegistryAccess.Classes}\Directory\Shell\AnyCode");
    }

    private static void SetInactiveTitlebarColor()
    {
        RegistryAccess.SetValue(@"HKCU:\SOFTWARE\Microsoft\Windows\DWM", "AccentColorInactive", 0xff2f2722);
    }

    private static void DisableWin11NewContextMenu()
    {
        RegistryAccess.SetValue(@"HKCU:\SOFTWARE\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InProcServer32", "", "");
    }

    private static void RemoveModernSharing()
    {
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked", "{e2bf9676-5f8f-435c-97eb-11607a5bedf7}", "");
    }

    private static void RemoveOpenInNewProcess()
    {
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Classes\Folder\Shell\opennewprocess", "LegacyDisable", "");
    }

    private static void MoveOpenInNewWindowToExtendedContext()
    {
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Classes\Folder\Shell\opennewwindow", "Extended", "");
    }

    private static void MoveOpenInNewTabToExtendedContext()
    {
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Classes\Folder\Shell\opennewtab", "Extended", "");
    }

    private static void RemovePinToHomeFromDrives()
    {
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Classes\Drive\Shell\pintohome", "LegacyDisable", "");
    }

    private static void MovePinToHomeInFoldersToExtendedContextMenu()
    {
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Classes\Folder\Shell\pintohome", "Extended", "");
    }

    private static void RemoveOpenWithContext()
    {
        RegistryAccess.DeleteKey(@"HKLM:\SOFTWARE\Classes\*\shellex\ContextMenuHandlers\Open With");
    }
    
    private static void RemoveRestorePreviousVersion()
    {
         RegistryAccess.DeleteKey(@"HKLM:\SOFTWARE\Classes\AllFilesystemObjects\shellex\PropertySheetHandlers\{596AB062-B4D2-4215-9F74-E9109B0A8153}");
         RegistryAccess.DeleteKey(@"HKLM:\SOFTWARE\Classes\CLSID\{450D8FBA-AD25-11D0-98A8-0800361B1103}\shellex\PropertySheetHandlers\{596AB062-B4D2-4215-9F74-E9109B0A8153}");
         RegistryAccess.DeleteKey(@"HKLM:\SOFTWARE\Classes\Directory\shellex\PropertySheetHandlers\{596AB062-B4D2-4215-9F74-E9109B0A8153}");
         RegistryAccess.DeleteKey(@"HKLM:\SOFTWARE\Classes\Drive\shellex\PropertySheetHandlers\{596AB062-B4D2-4215-9F74-E9109B0A8153}");
         RegistryAccess.DeleteKey(@"HKLM:\SOFTWARE\Classes\AllFilesystemObjects\shellex\ContextMenuHandlers\{596AB062-B4D2-4215-9F74-E9109B0A8153}");
         RegistryAccess.DeleteKey(@"HKLM:\SOFTWARE\Classes\CLSID\{450D8FBA-AD25-11D0-98A8-0800361B1103}\shellex\ContextMenuHandlers\{596AB062-B4D2-4215-9F74-E9109B0A8153}");
         RegistryAccess.DeleteKey(@"HKLM:\SOFTWARE\Classes\Directory\shellex\ContextMenuHandlers\{596AB062-B4D2-4215-9F74-E9109B0A8153}");
         RegistryAccess.DeleteKey(@"HKLM:\SOFTWARE\Classes\Drive\shellex\ContextMenuHandlers\{596AB062-B4D2-4215-9F74-E9109B0A8153}");
    }

    private static void RemovePinToStart()
    {
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Classes\Folder\shellex\ContextMenuHandlers\PintoStartScreen", "", "-{470C0EBD-5D73-4d58-9CED-E91E22E23282}");
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Classes\exefile\shellex\ContextMenuHandlers\PintoStartScreen", "", "-{470C0EBD-5D73-4d58-9CED-E91E22E23282}");
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Classes\Microsoft.Website\shellex\ContextMenuHandlers\PintoStartScreen", "", "-{470C0EBD-5D73-4d58-9CED-E91E22E23282}");
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Classes\mscfile\shellex\ContextMenuHandlers\PintoStartScreen", "", "-{470C0EBD-5D73-4d58-9CED-E91E22E23282}");
    }
    
    private static void RemovePinToTaskbar()
    {
        RegistryAccess.DeleteKey(@"HKLM:\SOFTWARE\Classes\*\shellex\ContextMenuHandlers\{90AA3A4E-1CBA-4233-B8BB-535773D48449}"); 
    }
    
    private static void RemoveScanWithDefenderContext()
    {
        RegistryAccess.DeleteKey(@"HKLM:\SOFTWARE\Classes\CLSID\{09A47860-11B0-4DA5-AFA5-26D86198A780}"); 
    }
    
    private static void RemoveSendToContext()
    {
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Classes\AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo", "", "-{7BA4C740-9E81-11CF-99D3-00AA004AE837}"); 
    }
    
    private static void RemoveTroubleshootCompat()
    {
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked", "{1D27F844-3A1F-4410-85AC-14651078412D}", ""); 
    }

    private static void RemoveCastToDeviceContext()
    {
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked", "{7AD84985-87B4-4a16-BE58-8B72A5B390F7}", "");
    }
    
    private static void RemoveGiveAccessToContext()
    {
        RegistryAccess.SetValue(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "SharingWizardOn", 0); 
    }
    
    private static void RemoveIncludeInLibraryContext()
    {
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Classes\Folder\ShellEx\ContextMenuHandlers\Library Location", "", "-{3dad6c5d-2167-4cae-9914-f99e41c12cfa}");
    }

    private static void RemoveAddToFavorites()
    {
        RegistryAccess.DeleteKey(@"HKLM:\SOFTWARE\Classes\*\shell\pintohomefile");
    }
    
    // Can use CTRL + SHIFT + C instead now
    private static void RemoveCopyAsPath()
    {
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Classes\AllFilesystemObjects\shellex\ContextMenuHandlers\CopyAsPathMenu", "", "-{f3d06e7c-1e45-4a26-847e-f9fcdee59be0}");
    }
    
    private static void HideRecycleBinFromDesktop()
    {
        RegistryAccess.TakeOwnership(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel");
        RegistryAccess.TakeOwnership(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\ClassicStartMenu");
        RegistryAccess.SetValue(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{645FF040-5081-101B-9F08-00AA002F954E}", 1);
        RegistryAccess.SetValue(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\ClassicStartMenu", "{645FF040-5081-101B-9F08-00AA002F954E}", 1);
    }
    
    private static void DisableNewAppAlert()
    {
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Policies\Microsoft\Windows\Explorer", "NoNewAppAlert", 1);
    }

    private static void DisableRegeditHistory()
    {
        const string regeditSettingsPath = @"HKCU:\Software\Microsoft\Windows\CurrentVersion\Applets\Regedit";
        try
        {
            RegistryAccess.DeleteKey(regeditSettingsPath);
            Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Applets\Regedit");
            RegistryAccess.DenyUserAccess(regeditSettingsPath);
        }
        catch
        {
            // Fix already applied
        }
    }

    private static void AlignTaskbarToLeft()
    {
        RegistryAccess.SetValue(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarAl", 0);
    }

    private static void ShowExplorerFileExtensions()
    {
        RegistryAccess.SetValue(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", 0);
    }

    private static void HideTaskbarSearch()
    {
        RegistryAccess.SetValue(@"HKCU:\Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1);
        RegistryAccess.SetValue(@"HKCU:\Software\Policies\Microsoft\Windows\DisableSearch", "DisableSearchBoxSuggestions", 1);
        RegistryAccess.SetValue(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 0);
    }

    private static void HideTaskViewButton()
    {
        RegistryAccess.SetValue(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowTaskViewButton", 0);
        RegistryAccess.SetValue(@"HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa", 0);
    }

    private static void HideNewsAndInterests()
    {
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Microsoft\PolicyManager\default\NewsAndInterests\AllowNewsAndInterests", "value", 0);
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 0);
    }

    private static void HideOpenPowershellHere()
    {
        RegistryAccess.TakeOwnership(@"HKLM:\SOFTWARE\Classes\Directory\background\shell\Powershell");
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Classes\Directory\background\shell\Powershell", "ProgrammaticAccessOnly", "");
        
        RegistryAccess.TakeOwnership(@"HKLM:\SOFTWARE\Classes\Directory\shell\Powershell");
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Classes\Directory\shell\Powershell", "ProgrammaticAccessOnly", "");
        
        RegistryAccess.TakeOwnership(@"HKLM:\SOFTWARE\Classes\Drive\shell\Powershell");
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Classes\Drive\shell\Powershell", "ProgrammaticAccessOnly", "");
    }
}