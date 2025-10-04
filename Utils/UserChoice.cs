using System.Runtime.InteropServices.ComTypes;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;

namespace SystemTweaks.Utils;

public static class UserChoice
{
    private static class HashFuncs
    {
        public static uint[] WordSwap(byte[] a, int sz, byte[] md5)
        {
            if (sz < 2 || (sz & 1) == 1) {
                throw new ArgumentException($"Invalid input size: {sz}", nameof(sz));
            }

            unchecked {
                uint o1 = 0;
                uint o2 = 0;
                int ta = 0;
                int ts = sz;
                int ti = ((sz - 2) >> 1) + 1;

                uint c0 = (BitConverter.ToUInt32(md5, 0) | 1) + 0x69FB0000;
                uint c1 = (BitConverter.ToUInt32(md5, 4) | 1) + 0x13DB0000;

                for (uint i = (uint)ti; i > 0; i--) {
                    uint n = BitConverter.ToUInt32(a, ta) + o1;
                    ta += 8;
                    ts -= 2;

                    uint v1 = 0x79F8A395 * (n * c0 - 0x10FA9605 * (n >> 16)) + 0x689B6B9F * ((n * c0 - 0x10FA9605 * (n >> 16)) >> 16);
                    uint v2 = 0xEA970001 * v1 - 0x3C101569 * (v1 >> 16);
                    uint v3 = BitConverter.ToUInt32(a, ta - 4) + v2;
                    uint v4 = v3 * c1 - 0x3CE8EC25 * (v3 >> 16);
                    uint v5 = 0x59C3AF2D * v4 - 0x2232E0F1 * (v4 >> 16);


                    o1 = 0x1EC90001 * v5 + 0x35BD1EC9 * (v5 >> 16);
                    o2 += o1 + v2;
                }

                if (ts == 1) {
                    uint n = BitConverter.ToUInt32(a, ta) + o1;

                    uint v1 = n * c0 - 0x10FA9605 * (n >> 16);
                    uint v2 = 0xEA970001 * (0x79F8A395 * v1 + 0x689B6B9F * (v1 >> 16)) -
                              0x3C101569 * ((0x79F8A395 * v1 + 0x689B6B9F * (v1 >> 16)) >> 16);
                    uint v3 = v2 * c1 - 0x3CE8EC25 * (v2 >> 16);

                    o1 = 0x1EC90001 * (0x59C3AF2D * v3 - 0x2232E0F1 * (v3 >> 16)) +
                         0x35BD1EC9 * ((0x59C3AF2D * v3 - 0x2232E0F1 * (v3 >> 16)) >> 16);
                    o2 += o1 + v2;
                }

                uint[] ret = new uint[2];
                ret[0] = o1;
                ret[1] = o2;
                return ret;
            }
        }

        public static uint[] Reversible(byte[] a, int sz, byte[] md5)
        {
            if (sz < 2 || (sz & 1) == 1) {
                throw new ArgumentException($"Invalid input size: {sz}", nameof(sz));
            }

            unchecked {
                uint o1 = 0;
                uint o2 = 0;
                int ta = 0;
                int ts = sz;
                int ti = ((sz - 2) >> 1) + 1;

                uint c0 = BitConverter.ToUInt32(md5, 0) | 1;
                uint c1 = BitConverter.ToUInt32(md5, 4) | 1;

                for (uint i = (uint)ti; i > 0; i--) {
                    uint n = (BitConverter.ToUInt32(a, ta) + o1) * c0;
                    n = 0xB1110000 * n - 0x30674EEF * (n >> 16);
                    ta += 8;
                    ts -= 2;

                    uint v1 = 0x5B9F0000 * n - 0x78F7A461 * (n >> 16);
                    uint v2 = 0x1D830000 * (0x12CEB96D * (v1 >> 16) - 0x46930000 * v1) +
                              0x257E1D83 * ((0x12CEB96D * (v1 >> 16) - 0x46930000 * v1) >> 16);
                    uint v3 = BitConverter.ToUInt32(a, ta - 4) + v2;

                    uint v4 = 0x16F50000 * c1 * v3 - 0x5D8BE90B * (c1 * v3 >> 16);
                    uint v5 = 0x2B890000 * (0x96FF0000 * v4 - 0x2C7C6901 * (v4 >> 16)) +
                              0x7C932B89 * ((0x96FF0000 * v4 - 0x2C7C6901 * (v4 >> 16)) >> 16);

                    o1 = 0x9F690000 * v5 - 0x405B6097 * (v5 >> 16);
                    o2 += o1 + v2;
                }

                if (ts == 1) {
                    uint n = BitConverter.ToUInt32(a, ta) + o1;

                    uint v1 = 0xB1110000 * c0 * n - 0x30674EEF * ((c0 * n) >> 16);
                    uint v2 = 0x5B9F0000 * v1 - 0x78F7A461 * (v1 >> 16);
                    uint v3 = 0x1D830000 * (0x12CEB96D * (v2 >> 16) - 0x46930000 * v2) +
                              0x257E1D83 * ((0x12CEB96D * (v2 >> 16) - 0x46930000 * v2) >> 16);
                    uint v4 = 0x16F50000 * c1 * v3 - 0x5D8BE90B * ((c1 * v3) >> 16);
                    uint v5 = 0x96FF0000 * v4 - 0x2C7C6901 * (v4 >> 16);

                    o1 = 0x9F690000 * (0x2B890000 * v5 + 0x7C932B89 * (v5 >> 16)) -
                         0x405B6097 * ((0x2B890000 * v5 + 0x7C932B89 * (v5 >> 16)) >> 16);
                    o2 += o1 + v2;
                }

                uint[] ret = new uint[2];
                ret[0] = o1;
                ret[1] = o2;
                return ret;
            }
        }

        public static long MakeLong(uint left, uint right) {
           return (long)left << 32 | right;
        }
    }

    private static string GetRootUserChoiceKeyPath(string extension)
    {
        return @$"HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{extension}";
    }

    private static string GetKeyWriteTimeForUser(string extension)
    {
        RegistryKey key = RegistryAccess.GetKey($@"{GetRootUserChoiceKeyPath(extension)}\UserChoice");

        FILETIME lastWriteTime = External.GetRegFileTime(key.Handle);
        
        uint ftHigh = BitConverter.ToUInt32(BitConverter.GetBytes(lastWriteTime.dwHighDateTime));
        uint ftLow = BitConverter.ToUInt32(BitConverter.GetBytes(lastWriteTime.dwLowDateTime));
        DateTime ft = DateTime.FromFileTime(((long)ftHigh << 32) | ftLow);
        long fttrunc = new DateTime(ft.Year, ft.Month, ft.Day, ft.Hour, ft.Minute, 0, ft.Kind).ToFileTime();
        
        return $"{fttrunc >> 32:x8}{fttrunc & uint.MaxValue:x8}";
    }

    private static string GetInputStringForUser(string extension, string progId, string writeTime)
    {
        string sid = WindowsIdentity.GetCurrent().User?.AccountDomainSid?.ToString() ?? "";
        const string text = "User Choice set via Windows User Experience {D18B6DD5-6124-4341-9318-804003BAFA0B}";
        string final = $"{extension}{sid}-1000{progId}{writeTime}{text}";
        return final.ToLowerInvariant();
    }

    private static long GetPatentHash(byte[] arr, byte[] md5)
    {
        int size = arr.Length;
        int shiftedSize = (size >> 2) - (size >> 2 & 1) * 1;

        uint[] a1 = HashFuncs.WordSwap(arr, shiftedSize, md5);
        uint[] a2 = HashFuncs.Reversible(arr, shiftedSize, md5);

        return HashFuncs.MakeLong(a1[1] ^ a2[1], a1[0] ^ a2[0]);
    } 
    
    public static void SetUserChoice(string extension, string progId)
    {
        string rootKey = GetRootUserChoiceKeyPath(extension);

        if (RegistryAccess.KeyExists(rootKey))
        {
            RegistryAccess.TakeOwnership(rootKey);
        }
        else
        {
            RegistryAccess.AddKey(rootKey);
        }

        RegistryAccess.DeleteKey(@$"{rootKey}\OpenWithList");
        RegistryAccess.AddKey(@$"{rootKey}\OpenWithList");
        
        RegistryAccess.DeleteKey(@$"{rootKey}\OpenWithProgids");
        RegistryAccess.AddKey(@$"{rootKey}\OpenWithProgids");
        RegistryAccess.SetValue(@$"{rootKey}\OpenWithProgids", progId, Array.Empty<byte>(), RegistryValueKind.None);
        
        
        RegistryAccess.AddKey(@$"HKLM:\SOFTWARE\Classes\{extension}\OpenWithProgIds");
        RegistryAccess.SetValue(@$"HKLM:\SOFTWARE\Classes\{extension}\OpenWithProgIds", progId, Array.Empty<byte>(), RegistryValueKind.None);
        RegistryAccess.SetValue(@$"HKCU:\SOFTWARE\Classes\{extension}\OpenWithProgIds", progId, Array.Empty<byte>(), RegistryValueKind.None);
        
        RegistryAccess.SetValue(@$"HKLM:\SOFTWARE\Classes\{extension}", "", progId, RegistryValueKind.String);
        
        RegistryAccess.TakeOwnership(@$"{rootKey}\UserChoice");
        RegistryAccess.DeleteKey(@$"{rootKey}\UserChoice");
        RegistryAccess.AddKey(@$"{rootKey}\UserChoice");
        RegistryAccess.SetValue(@$"{rootKey}\UserChoice", "ProgId", progId);
        
        string writeTime = GetKeyWriteTimeForUser(extension);
        string inputString = GetInputStringForUser(extension, progId, writeTime);
        
        List<byte> x = Encoding.Unicode.GetBytes(inputString).ToList();
            x.Add(0);
            x.Add(0);

        byte[] arr = x.ToArray();
        byte[] md5 = System.Security.Cryptography.MD5.HashData(arr);
        long patentHash = GetPatentHash(arr, md5);
        
        string hash = Convert.ToBase64String(BitConverter.GetBytes(patentHash));
        
        RegistryAccess.SetValue(@$"{rootKey}\UserChoice", "Hash", hash);
    }
}