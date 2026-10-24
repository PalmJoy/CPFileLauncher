using System.Runtime.Versioning;
using CPClipboard.Core.Paste;
using static CPClipboard.Platform.Windows.Native.PasteNative;

namespace CPClipboard.Platform.Windows;

[SupportedOSPlatform("windows")]
public sealed class WindowsPasteGuard : IPasteGuard
{
    public PasteGuardResult Check(nint target)
    {
        GetWindowThreadProcessId(target, out uint pid);
        bool? elevated = Elevated(pid), self = Elevated((uint)Environment.ProcessId);
        if (elevated is null || self is null) return PasteGuardResult.Unknown;
        return elevated.Value && !self.Value ? PasteGuardResult.Elevated : PasteGuardResult.Ok;
    }
    private static bool? Elevated(uint pid)
    {
        nint process = OpenProcess(0x1000, false, pid);
        if (process == 0) return null;
        try
        {
            if (!OpenProcessToken(process, 8, out nint token)) return null;
            try { return GetTokenInformation(token, 20, out int elevated, sizeof(int), out _) ? elevated != 0 : null; }
            finally { CloseHandle(token); }
        }
        finally { CloseHandle(process); }
    }
}
