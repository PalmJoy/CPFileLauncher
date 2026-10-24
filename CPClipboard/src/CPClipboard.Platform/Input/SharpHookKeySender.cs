using SharpHook.Data;
using SharpHook.Providers;
using SharpHook.Simulation;

namespace CPClipboard.Platform.Input;

/// <summary>押下を試みたキーは、途中失敗時も必ず解放を試みる。</summary>
public sealed class SharpHookKeySender(bool macOS, IEventSimulationProvider? provider = null) : IKeySender
{
    public bool SendPaste()
    {
        try
        {
            using var simulator = EventSimulator.Create("CPClipboard", provider);
            var modifier = macOS ? KeyCode.VcLeftMeta : KeyCode.VcLeftControl;
            bool modifierAttempted = false, keyAttempted = false, ok = false;
            try
            {
                modifierAttempted = true;
                if (simulator.SimulateKeyPress(modifier) == UioHookResult.Success)
                {
                    keyAttempted = true;
                    ok = simulator.SimulateKeyPress(KeyCode.VcV) == UioHookResult.Success;
                }
            }
            catch { ok = false; }
            finally
            {
                if (keyAttempted) ok &= Release(KeyCode.VcV);
                if (modifierAttempted) ok &= Release(modifier);
            }
            return ok;
            bool Release(KeyCode key)
            {
                try { return simulator.SimulateKeyRelease(key) == UioHookResult.Success; }
                catch { return false; }
            }
        }
        catch { return false; }
    }
}
