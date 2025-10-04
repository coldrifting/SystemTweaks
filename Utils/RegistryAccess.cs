using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace SystemTweaks.Utils;

public static partial class RegistryAccess
{
    public const string Classes = @"HKLM:\SOFTWARE\Classes";
    
    public static RegistryKey GetKey(string path, RegistryKeyPermissionCheck? perms = null, RegistryRights? rights = null)
    {
        string subpath = RootRegistryRegex().Replace(path, "");

        RegistryKey root;
        
        if (path.StartsWith("HKLM:\\") || path.StartsWith("HKEY_LOCAL_MACHINE\\"))
        {
            root = Registry.LocalMachine;
        }
        else if (path.StartsWith("HKCU:\\") || path.StartsWith("HKEY_CURRENT_USER\\"))
        {
            root = Registry.CurrentUser;
        }
        else if (path.StartsWith("HKCR:\\") || path.StartsWith("HKEY_CLASSES_ROOT\\"))
        {
            root = Registry.ClassesRoot;
        }
        else if (path.StartsWith("HKU:\\") || path.StartsWith("HKEY_USERS\\"))
        {
            root = Registry.Users;
        }
        else if (path.StartsWith("HKCC:\\") || path.StartsWith("HKEY_CURRENT_CONFIG\\"))
        {
            root = Registry.CurrentConfig;
        }
        else
        {
            throw new ArgumentException($"Invalid registry path root specified: {path}");
        }
        
        if (perms is not null && rights is not null)
        {
            return root.OpenSubKey(subpath, perms.Value, rights.Value) ?? throw new KeyNotFoundException($"Could not find path {path}");
        }
        return root.OpenSubKey(subpath) ?? throw new KeyNotFoundException($"Could not find path: {path} with root: {root}");
    }
    
    public static void AddKey(string keyPath)
    {
        Logger.Log($"Adding Registry Key: {keyPath}");
        try
        {
            int lastIndex = keyPath.LastIndexOf('\\');
            if (lastIndex == -1)
            {
                throw new KeyNotFoundException();
            }

            string parentKeyPath = keyPath[..lastIndex];
            string subKeyToAdd = keyPath[(lastIndex + 1)..];
            
            using RegistryKey parentKey = GetKey(parentKeyPath, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl);
            if (parentKey.GetSubKeyNames().Contains(subKeyToAdd))
            {
                Logger.Log("Registry Key already exists, skipping");
                return;
            }
            
            parentKey.CreateSubKey(subKeyToAdd);
        }
        catch (Exception e)
        {
            switch (e)
            {
                case KeyNotFoundException:
                    Logger.Warn("Key not found");
                    break;
                case SecurityException or UnauthorizedAccessException:
                    Logger.Error("Invalid rights to delete key");
                    break;
                default:
                    Logger.Error("Error: Invalid key");
                    Logger.Error(e.Message);
                    break;
            }
        }
    }
    
    public static void DeleteKey(string keyPath)
    {
        Logger.Log($"Deleting Registry Key: {keyPath}");
        try
        {
            int lastIndex = keyPath.LastIndexOf('\\');
            if (lastIndex == -1)
            {
                throw new KeyNotFoundException();
            }

            string parentKeyPath = keyPath[..lastIndex];
            string subKeyToDelete = keyPath[(lastIndex + 1)..];
            
            using RegistryKey parentKey = GetKey(parentKeyPath, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl);
            parentKey.DeleteSubKeyTree(subKeyToDelete, false);
        }
        catch (Exception e)
        {
            switch (e)
            {
                case KeyNotFoundException:
                    //Logger.Warn("Key not found");
                    break;
                case SecurityException or UnauthorizedAccessException:
                    Logger.Error("Invalid rights to delete key");
                    break;
                default:
                    Logger.Error("Error: Invalid key");
                    Logger.Error(e.Message);
                    break;
            }
        }
    }
    
    public static object? GetValue(string keyPath, string valueName)
    {
        return GetKey(keyPath).GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
    }
    
    public static void SetValue(string keyPath, string valueName, object value, RegistryValueKind? valueType = null)
    {
        Logger.Log($"Setting Registry Value: Key: {keyPath}, Name: {valueName}, Data: {value}, Type:  {valueType.ToString()} ");
        try
        {
            if (valueType is { } valueTypeNotNull)
            {
                Registry.SetValue(ConvertPath(keyPath), valueName, value, valueTypeNotNull);
            }
            else
            {
                // Automatically detect expand strings
                if (value is string s && s.Split('%').Length - 1 > 1)
                {
                    Registry.SetValue(ConvertPath(keyPath), valueName, value, RegistryValueKind.ExpandString);
                }
                else if (int.TryParse(value.ToString(), out int i))
                {
                    Registry.SetValue(ConvertPath(keyPath), valueName, i, RegistryValueKind.DWord);
                }
                else
                {
                    Registry.SetValue(ConvertPath(keyPath), valueName, value, RegistryValueKind.String);
                }
            }
        }
        catch (Exception e)
        {
            switch (e)
            {
                case KeyNotFoundException:
                    Logger.Warn("Key not found");
                    break;
                case SecurityException or UnauthorizedAccessException:
                    Logger.Error("Invalid rights to set key value");
                    break;
                default:
                    Logger.Error("Error: Invalid key");
                    Logger.Error(e.Message);
                    break;
            }
        }
    }
    
    public static void DeleteValue(string keyPath, string valueName)
    {
        Logger.Log($"Deleting Registry Value: Key: {keyPath}, Name: {valueName}");
        try
        {
            using RegistryKey key = GetKey(keyPath, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl);

            if (!key.GetValueNames().Contains(valueName))
            {
                throw new KeyNotFoundException();
            }

            key.DeleteValue(valueName);
        }
        catch (Exception e)
        {
            switch (e)
            {
                case KeyNotFoundException:
                    //Logger.Warn("Key or key value not found");
                    break;
                case SecurityException or UnauthorizedAccessException:
                    Logger.Error("Invalid rights to delete key value");
                    break;
                default:
                    Logger.Error("Error: Invalid key");
                    Logger.Error(e.Message);
                    break;
            }
        }
    }
    
    public static void DenyUserAccess(string keyPath)
    {
        try
        {
            NTAccount user = new(Environment.UserName);
            
            // Take ownership
            RegistryKey subKey = GetKey(keyPath, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.TakeOwnership);
            
            RegistrySecurity s = subKey.GetAccessControl(AccessControlSections.All);
            RegistryAccessRule denyAccess = new(user, RegistryRights.FullControl,
                InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Deny);
            s.AddAccessRule(denyAccess);
            subKey.SetAccessControl(s);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error:  {e.Message}");
        }
    }
    
    public static void TakeOwnership(string keyPath)
    {
        Logger.Log($"Taking ownership of the key: {keyPath}");
        try
        {
            bool blRc = PermisionEnabler.MySetPrivilege(PermisionEnabler.TakeOwnership, true);
            if (!blRc)
              throw new PrivilegeNotHeldException(PermisionEnabler.TakeOwnership);

            /* Add the Restore Privilege (must be done to change the owner)
             */
            blRc = PermisionEnabler.MySetPrivilege(PermisionEnabler.Restore, true);
            if (!blRc)
              throw new PrivilegeNotHeldException(PermisionEnabler.Restore);
            
            NTAccount user = new(Environment.UserName);
            NTAccount admins = new("Administrators");
            
            // Take ownership
            RegistryKey subKey = GetKey(keyPath, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.TakeOwnership);
            
            RegistrySecurity s = subKey.GetAccessControl(AccessControlSections.All);
            s.SetOwner(admins);
            subKey.SetAccessControl(s);

            RegistryAccessRule fullAccess = new(admins, RegistryRights.FullControl,
                InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow);
            s.AddAccessRule(fullAccess);
            s.RemoveAccessRuleAll(new RegistryAccessRule(user, RegistryRights.SetValue, InheritanceFlags.None,
                PropagationFlags.None, AccessControlType.Deny));
            subKey.SetAccessControl(s);
        }
        catch (Exception e)
        {
            switch (e)
            {
                case KeyNotFoundException:
                    Logger.Warn("Key not found");
                    break;
                case SecurityException or UnauthorizedAccessException:
                    Logger.Error("Invalid rights to modify key");
                    break;
                default:
                    Logger.Error("Error: Invalid key");
                    Logger.Error(e.Message);
                    break;
            }
        }
    }
    
    // Internal Helper Functions

    private static string ConvertPath(string keyPath)
    {
        if (keyPath.StartsWith("HKLM:\\"))
        {
            return keyPath.Replace("HKLM:\\", "HKEY_LOCAL_MACHINE\\");
        }
        if (keyPath.StartsWith("HKCU:\\"))
        {
            return keyPath.Replace("HKCU:\\", "HKEY_CURRENT_USER\\");
        }
        if (keyPath.StartsWith("HKCR:\\"))
        {
            return keyPath.Replace("HKCR:\\", "HKEY_CLASSES_ROOT\\");
        }
        if (keyPath.StartsWith("HKU:\\"))
        {
            return keyPath.Replace("HKU:\\", "HKEY_USERS\\");
        }
        if (keyPath.StartsWith("HKCC:\\"))
        {
            return keyPath.Replace("HKCC:\\", "HKEY_CURRENT_CONFIG\\");
        }

        throw new ArgumentException("Could not convert the path because it's root key is invalid");
    }
    
    [GeneratedRegex(@"^H\w{2,3}:\\|^HKEY_\w+\\")]
    private static partial Regex RootRegistryRegex();

    public static bool KeyExists(string rootKey)
    {
        try
        {
            GetKey(rootKey);
            return true;
        }
        catch
        {
            return false;
        }
    }
}