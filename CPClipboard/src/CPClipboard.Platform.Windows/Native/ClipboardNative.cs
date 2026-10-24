using System.Runtime.InteropServices;

namespace CPClipboard.Platform.Windows.Native;

internal static class ClipboardNative
{
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] public static extern bool OpenClipboard(nint owner);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] public static extern bool CloseClipboard();
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] public static extern bool EmptyClipboard();
    [DllImport("user32.dll", SetLastError = true)] public static extern uint EnumClipboardFormats(uint format);
    [DllImport("user32.dll")] public static extern nint GetClipboardData(uint format);
    [DllImport("user32.dll", SetLastError = true)] public static extern nint SetClipboardData(uint format, nint memory);
    [DllImport("user32.dll")] public static extern uint GetClipboardSequenceNumber();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern uint RegisterClipboardFormat(string name);
    [DllImport("kernel32.dll", SetLastError = true)] public static extern nint GlobalAlloc(uint flags, nuint bytes);
    [DllImport("kernel32.dll")] public static extern nint GlobalFree(nint memory);
    [DllImport("kernel32.dll")] public static extern nint GlobalLock(nint memory);
    [DllImport("kernel32.dll")] public static extern bool GlobalUnlock(nint memory);
    [DllImport("kernel32.dll")] public static extern nuint GlobalSize(nint memory);
    [DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern nint CreateWindowEx(uint style, string className, string name, uint windowStyle, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint param);
    [DllImport("user32.dll")] public static extern bool DestroyWindow(nint window);
    [StructLayout(LayoutKind.Sequential)] public struct MSG { public nint Hwnd; public uint Message; public nuint WParam; public nint LParam; public uint Time; public User32.POINT Point; public uint Private; }
    [DllImport("user32.dll")] public static extern bool PeekMessage(out MSG message, nint window, uint min, uint max, uint remove);
    [DllImport("user32.dll")] public static extern bool TranslateMessage(ref MSG message);
    [DllImport("user32.dll")] public static extern nint DispatchMessage(ref MSG message);
}
