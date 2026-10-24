using System.Runtime.InteropServices;

namespace CPClipboard.Platform.Windows.Native;

internal static class PasteNative
{
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(nint window, out uint processId);
    [DllImport("kernel32.dll")] public static extern nint OpenProcess(uint access, bool inherit, uint processId);
    [DllImport("kernel32.dll")] public static extern bool CloseHandle(nint handle);
    [DllImport("advapi32.dll")] public static extern bool OpenProcessToken(nint process, uint access, out nint token);
    [DllImport("advapi32.dll")] public static extern bool GetTokenInformation(nint token, int kind, out int value, int size, out int required);
}
