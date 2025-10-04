using Microsoft.Win32;

namespace SystemTweaks.Utils;

public static class ProgId
{
    public static void NewProgId(string ext, string progId)
    {
        RegistryAccess.DeleteKey($@"{RegistryAccess.Classes}\{progId}");
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\{ext}", "", progId);
    }

    public static void SetDescription(string progId, string desc)
    {
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\{progId}", "", desc);
    }
    
    public static void SetIcon(string progId, string icon, bool isExpanded = false)
    { 
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\{progId}\DefaultIcon", "", icon, isExpanded ? RegistryValueKind.ExpandString : RegistryValueKind.String);
    }

    public static void SetAction(string progId, string action, string command, string? label = null,
        bool extended = false, bool shieldIcon = false, bool isDefault = false, bool hidden = false, bool isExpanded = false)
    {
        string actionLabel = label ?? action;
        
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\{progId}\Shell\{action}", "", actionLabel);
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\{progId}\Shell\{action}\Command", "", command, isExpanded ? RegistryValueKind.ExpandString : RegistryValueKind.String);

        if (isDefault)
        {
            RegistryAccess.SetValue($@"{RegistryAccess.Classes}\{progId}\Shell", "", action);
        }
        
        SetExtended(progId, action, extended);
        SetShieldIcon(progId, action, shieldIcon);
        SetHidden(progId, action, hidden);
    }
    
    public static void SetHidden(string progId, string action, bool enabled = true)
    {
        string regKey = $@"{RegistryAccess.Classes}\{progId}\Shell\{action}";
        if (RegistryAccess.KeyExists(regKey))
        {
            if (enabled)
            {
                RegistryAccess.SetValue(regKey, "LegacyDisable", "");
            }
            else
            {
                RegistryAccess.DeleteValue(regKey, "LegacyDisable");
            }
        }
    }
    
    public static void SetExtended(string progId, string action, bool enabled = true)
    {
        if (enabled)
        {
            RegistryAccess.SetValue($@"{RegistryAccess.Classes}\{progId}\Shell\{action}", "Extended", "");
        }
        else
        {
            RegistryAccess.DeleteValue($@"{RegistryAccess.Classes}\{progId}\Shell\{action}", "Extended");
        }
    }
    
    public static void SetShieldIcon(string progId, string action, bool enabled = true)
    {
        if (enabled)
        {
            RegistryAccess.SetValue($@"{RegistryAccess.Classes}\{progId}\Shell\{action}", "HasLUAShield", "");
        }
        else
        {
            RegistryAccess.DeleteValue($@"{RegistryAccess.Classes}\{progId}\Shell\{action}", "HasLUAShield");
        }
    }

    public static void SetActionLabel(string progId, string action, string label)
    {
        RegistryAccess.SetValue($@"{RegistryAccess.Classes}\{progId}\Shell\{action}", "", label);
    }

    public static void BlockShellExtensions(string[] extensionIds)
    {
        foreach (string extensionId in extensionIds)
        {
            RegistryAccess.SetValue(@"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked", extensionId, "");
            RegistryAccess.SetValue(@"HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked", extensionId, "");
        }
    }
}