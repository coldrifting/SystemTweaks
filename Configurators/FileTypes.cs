using System.Text.RegularExpressions;
using Microsoft.Win32;
using SystemTweaks.Utils;
using static SystemTweaks.Utils.ProgId;
using RegistryAccess = SystemTweaks.Utils.RegistryAccess;

namespace SystemTweaks.Configurators;

public static partial class FileTypes
{
    private static string GetProgId(Config.FileType fileType, string extension)
    {
        return fileType.ProgId ?? $"{fileType.Type}{extension}";
    }

    private static (string variable, bool isExpanded) ExpandVariable(Config config, string rawString)
    {
        bool isExpanded = false;
        if (rawString.Contains("$("))
        {
            Match variableNameMatch = YamlVarRegex().Match(rawString);
            if (!variableNameMatch.Success)
            {
                return new ValueTuple<string, bool>(rawString, isExpanded);
            }

            string? variableName = variableNameMatch.Captures.FirstOrDefault()?.Value;
            if (variableName is null)
            {
                return new ValueTuple<string, bool>(rawString, isExpanded);
            }

            variableName = variableName.Substring(2, variableName.Length - 3);
            if (config.Resources.TryGetValue(variableName, out string? resource))
            {
                rawString = YamlVarRegex().Replace(rawString, resource);
            }
        }
        isExpanded = EnvVarRegex().IsMatch(rawString);

        return new ValueTuple<string, bool>(rawString, isExpanded);
    }

    private static void SetFileTypeIcon(Config config, string progId, string rawIconString)
    {
        ValueTuple<string, bool> expanded = ExpandVariable(config, rawIconString);
        SetIcon(progId, expanded.Item1, expanded.Item2);
    }

    private static void SetFileTypeAction(Config config, string progId, string actionName, bool isExtended = false, bool isDefault = false)
    {
        if (config.FileTypeActions.TryGetValue(actionName, out Config.FileTypeAction? fileTypeAction))
        {
            (string fileTypeActionCommand, bool isCommandExpanded) = ExpandVariable(config, fileTypeAction.Command);
            SetAction(
                progId, 
                fileTypeAction.Name, 
                fileTypeActionCommand, 
                fileTypeAction.Label, 
                isExtended,
                fileTypeAction.HasShieldIcon, 
                isDefault, 
                isExpanded: isCommandExpanded);
        }
    }

    public static void RunAll(Config config)
    {
        Logger.Print("Applying File Type Configurations...");
        
        foreach (Config.FileType fileType in config.FileTypes)
        {
            Logger.Print($"{fileType.Extension}...");
            string progId = GetProgId(fileType, fileType.Extension);

            if (fileType.TakeOwnership)
            {
                RegistryAccess.TakeOwnership(@$"{RegistryAccess.Classes}\{progId}");
                RegistryAccess.TakeOwnership(@$"{RegistryAccess.Classes}\{progId}\DefaultIcon");
                RegistryAccess.TakeOwnership(@$"{RegistryAccess.Classes}\{progId}\Shell");
                RegistryAccess.TakeOwnership(@$"{RegistryAccess.Classes}\{progId}\Shell\Print");
                RegistryAccess.TakeOwnership(@$"{RegistryAccess.Classes}\{progId}\Shell\Edit");
            }

            if (!fileType.Type.Equals("system", StringComparison.InvariantCultureIgnoreCase))
            {
                NewProgId(fileType.Extension, progId);
            }

            if (fileType.Description is not null)
            {
                SetDescription(progId, fileType.Description);
            }

            if (fileType.Icon is not null)
            {
                SetFileTypeIcon(config, progId, fileType.Icon);
            }

            string defaultAction = fileType.DefaultAction 
                                   ?? fileType.Actions.FirstOrDefault() 
                                   ?? fileType.ActionsExtended.FirstOrDefault() 
                                   ?? "";
            
            foreach (string fileTypeAction in fileType.Actions)
            {
                SetFileTypeAction(config, progId, fileTypeAction, false, fileTypeAction.Equals(defaultAction, StringComparison.InvariantCultureIgnoreCase));
            }
            
            foreach (string fileTypeAction in fileType.ActionsExtended)
            {
                SetFileTypeAction(config, progId, fileTypeAction, true, fileTypeAction.Equals(defaultAction, StringComparison.InvariantCultureIgnoreCase));
            }

            foreach (string fileTypeAction in fileType.ActionsHidden)
            {
                SetHidden(progId, fileTypeAction);
            }

            foreach (string fileTypeAction in fileType.ActionsHidden)
            {
                SetHidden(progId, fileTypeAction);
            }

            switch (fileType.Type)
            {
                case "Image":
                    RegistryAccess.SetValue(
                        $@"{RegistryAccess.Classes}\SystemFileAssociations\{fileType.Extension}\Shell\setdesktopwallpaper", "",
                        "Set Wallpaper");
                    RegistryAccess.SetValue($@"{RegistryAccess.Classes}\SystemFileAssociations\image\Shell\Print",
                        "LegacyDisable", "");
                    break;
                case "Mesh":
                    SetHidden($@"SystemFileAssociations\{fileType.Extension}", "3DPrint");
                    break;
            }

            SetHidden(progId, "print");
            SetHidden(progId, "edit");
            
            UserChoice.SetUserChoice($"{fileType.Extension}", progId);
        }

        // No extension and unknown file types
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\.", "", "Text.NoExtension");
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\.\ShellNew", "NullFile", "");
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\.\ShellNew", "FileName", "File");
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\.\ShellNew", "IconPath", @"C:\Windows\System32\ImageRes.dll,-2");
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\.\ShellNew\Config", "NoExtension", "");
        UserChoice.SetUserChoice(".", "Text.NoExtension");

        NewProgId(".", "Text.NoExtension");
        SetDescription("Text.NoExtension", "File");
        SetIcon("Text.NoExtension", @"C:\Windows\System32\ImageRes.dll,-2");

        SetFileTypeAction(config, "Text.NoExtension", "EditText");

        SetFileTypeAction(config, "Unknown", "EditText");
        SetExtended("Unknown", "openas");
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\Unknown\Shell\Open", "ProgrammaticAccessOnly", "");

        // Hide some default file associations
        SetHidden("SystemFileAssociations\\.ps1", "Edit");
        SetHidden("SystemFileAssociations\\.ps1", "Windows.PowerShell.Run");
        SetHidden("SystemFileAssociations\\.psm1", "Edit");
        
        // Remove edit with Notepad from context menu
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\SystemFileAssociations\Text\Shell\Open", "LegacyDisable", "");
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\SystemFileAssociations\Text\Shell\Edit", "LegacyDisable", "");

        // Remove Add to/Play with Windows Media Player
        RegistryAccess.DeleteKey($@"{RegistryAccess.Classes}\SystemFileAssociations\audio\shell\Enqueue");
        RegistryAccess.DeleteKey($@"{RegistryAccess.Classes}\SystemFileAssociations\audio\shell\Play");
        RegistryAccess.DeleteKey($@"{RegistryAccess.Classes}\SystemFileAssociations\Directory.Audio\shell\Enqueue");
        RegistryAccess.DeleteKey($@"{RegistryAccess.Classes}\SystemFileAssociations\Directory.Audio\shell\Play");
        RegistryAccess.DeleteKey($@"{RegistryAccess.Classes}\SystemFileAssociations\Directory.Image\shell\Enqueue");
        RegistryAccess.DeleteKey($@"{RegistryAccess.Classes}\SystemFileAssociations\Directory.Image\shell\Play");

        // Restore Windows Default Archive Properties
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\Archive.cab\CLSID", "",
            "{0CD7A5C0-9F37-11CE-AE65-08002B2E1262}");

        // Restore Windows Explorer ISO Mouting
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\Archive.iso\Shell\Mount", "CommandStateSync", "");
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\Archive.iso\Shell\Mount", "ExplorerCommandHandler",
            "{9ab3b1c9-3225-4bb4-93b6-bfb3c0d93743}");
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\Archive.iso\Shell\Mount", "MultiSelectModel", "Document");
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\Archive.iso\Shell\Mount\Command", "",
            "%SystemRoot%\\Explorer.exe");
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\Archive.iso\Shell\Mount\Command", "DelegateExecute",
            "{9ab3b1c9-3225-4bb4-93b6-bfb3c0d93743}");
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\Archive.iso\tabsets", "selection", 1796);

        // Hide Edit with JetBrains Rider
        RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Classes\*\shell\Open with JetBrains Rider", "LegacyDisable", "");

        string[] blockedShellExtensions =
        [
            // Photos context menus
            "{BFE0E2A4-C70C-4AD7-AC3D-10D1ECEBB5B4}",
            "{1100CBCD-B822-43F0-84CB-16814C2F6B44}",
            "{7A53B94A-4E6E-4826-B48E-535020B264E5}",
            "{9AAFEDA2-97B6-43EA-9466-9DE90501B1B6}",
            
            // Hide Default Extract All Context menus
            "{EE07CEF5-3441-4CFB-870A-4002C724783A}",
            "{b8cdcb65-b1bf-4b42-9428-1dfdb7ee92af}",
            // Remove Edit with Notepad Context menus
            "{CA6CC9F1-867A-481E-951E-A28C5E4F01EA}",
            // Hide 7-Zip Context menus
            "{23170F69-40C1-278A-1000-000100020000}", 
            // Hide Add to / Play with Windows Media Player Context menus
            "{45597c98-80f6-4549-84ff-752cf55e2d29}", 
            "{ed1d0fdf-4414-470a-a56d-cfb68623fc58}"
        ];
        BlockShellExtensions(blockedShellExtensions);
        
        // Cleanup Shell New Menu
        foreach (string shellNewEntry in config.ShellNewEntriesToDelete)
        {
            RegistryAccess.DeleteKey($@"{RegistryAccess.Classes}\{shellNewEntry}\ShellNew");
        }

        // Persistent .bmp and .txt shell new stuff
        using RegistryKey mrtCache = RegistryAccess.GetKey(@"HKCU:\Software\Classes\Local Settings\MrtCache");
        foreach (string s1 in mrtCache.GetSubKeyNames())
        {
            if (!MrtCacheRegex().IsMatch(s1))
            {
                continue;
            }

            using RegistryKey? k1 = mrtCache.OpenSubKey(s1);
            foreach (string s2 in k1?.GetSubKeyNames() ?? [])
            {
                using RegistryKey? k2 = k1?.OpenSubKey(s2);
                foreach (string s3 in k2?.GetSubKeyNames() ?? [])
                {
                    using RegistryKey? k3 = k2?.OpenSubKey(s3, true);
                    foreach (string e in k3?.GetValueNames() ?? [])
                    {
                        if (MrtCachePropRegex().IsMatch(e))
                        {
                            k3?.SetValue(e, "");
                        }
                    }
                }
            }
        }
    }

    [GeneratedRegex(@".*Microsoft\.Paint.*|.*Microsoft\.WindowsNotepad.*")]
    private static partial Regex MrtCacheRegex();

    [GeneratedRegex(".*ShellNewDisplayName_.*")]
    private static partial Regex MrtCachePropRegex();

    [GeneratedRegex(@"%\w+%")]
    private static partial Regex EnvVarRegex();
    
    [GeneratedRegex(@"\$\(\w+\)")]
    private static partial Regex YamlVarRegex();
}