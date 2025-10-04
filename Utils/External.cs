using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace SystemTweaks.Utils;

// Code related to calling native dll libraries here
public static partial class External
{
    private const int HwndBroadcast = 0xffff;
    private const uint WmSettingchange = 0x001a;

    [LibraryImport("User32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial void SendNotifyMessageW(IntPtr hWnd, uint msg, UIntPtr wParam, string lParam);
    
    [LibraryImport("Shell32.dll")] 
    private static partial void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);
    
    public static void RefreshShell()
    {
        SendNotifyMessageW(HwndBroadcast, WmSettingchange, 0, "Environment");
        SHChangeNotify(0x8000000, 0, IntPtr.Zero, IntPtr.Zero);
    }
    
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int RegQueryInfoKey(
        SafeRegistryHandle hKey,
        StringBuilder lpClass,
        [In, Out] ref uint lpcbClass,
        uint lpReserved,
        out uint lpcSubKeys,
        out uint lpcbMaxSubKeyLen,
        out uint lpcbMaxClassLen,
        out uint lpcValues,
        out uint lpcbMaxValueNameLen,
        out uint lpcbMaxValueLen,
        out uint lpcbSecurityDescriptor,
        out FILETIME lpftLastWriteTime
    );

    public static FILETIME GetRegFileTime(SafeRegistryHandle keyHandle)
    {
        uint len = 16384;
        StringBuilder builder = new((int)len);
        
        int x = RegQueryInfoKey(keyHandle, builder, ref len, 
            0, out uint _, out uint _, out uint _, 
            out uint _, out uint _, out uint _, out uint _, 
            out FILETIME lastWriteTime
        );

        return x == 0 ? lastWriteTime : new FILETIME();
    }
}