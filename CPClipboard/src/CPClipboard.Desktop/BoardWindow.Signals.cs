using Avalonia.Controls;

namespace CPClipboard.App;

public partial class BoardWindow
{
    private SelectionLock? _selectionLock;
    private SendSignal? _sendSignal;

    private static void MoveSignal(SignalPulse signal, Grid destination)
    {
        if (ReferenceEquals(signal.Parent, destination)) return;
        signal.Stop();
        if (signal.Parent is Grid previous) previous.Children.Remove(signal);
        destination.Children.Add(signal);
    }

    private void DetachRowSignals()
    {
        if (_selectionLock is { } target)
        {
            target.Active = false;
            target.Stop();
            if (target.Parent is Grid grid) grid.Children.Remove(target);
        }
        if (_sendSignal is { } send)
        {
            send.Stop();
            if (send.Parent is Grid grid) grid.Children.Remove(send);
        }
        if (!_hoverEffect.IsActive) _selectionLock = null;
        if (!_pasteEffect.IsActive) _sendSignal = null;
    }
}
