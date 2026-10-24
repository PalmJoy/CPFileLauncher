using System.Runtime.Versioning;
using CPClipboard.Core.Paste;
using CPClipboard.Platform.MacOS.Native;

namespace CPClipboard.Platform.MacOS;

[SupportedOSPlatform("macos")]
public sealed class MacPasteGuard : IPasteGuard
{
    public PasteGuardResult Check(nint target)
    {
        if (!Accessibility.AXIsProcessTrusted()) return PasteGuardResult.NoPermission;
        return Accessibility.IsSecureEventInputEnabled() ? PasteGuardResult.SecureInput : PasteGuardResult.Ok;
    }
}
