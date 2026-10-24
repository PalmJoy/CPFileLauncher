using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using InputModifiers = Avalonia.Input.KeyModifiers;

namespace CPClipboard.App;

/// <summary>
/// 盤面の移動（SPEC §3.1）: 行以外の所（タブ・タブの隙間・空のグループの余白・ステータス行・枠の余白）を押したまま動かすとウィンドウが動く。
/// クリックを壊さないよう 4 px を越えてから動かし始め、ポインタを Root に移してタブのクリックを起こさない。
/// </summary>
public partial class BoardWindow
{
    /// <summary>動き始めるまでの距離（論理 px）。</summary>
    internal const double MoveThresholdPx = 4;

    private PixelPoint? _movePressScreen;   // 押した位置（画面座標）
    private PixelPoint _moveStartPosition;  // 押したときのウィンドウ位置
    private bool _windowMoving;

    /// <summary>ウィンドウを動かしている最中か（UI テスト用）。</summary>
    internal bool IsMovingWindow => _windowMoving;

    private void AttachMoveInput()
    {
        Root.AddHandler(PointerPressedEvent, (_, e) =>
        {
            _movePressScreen = null;
            var props = e.GetCurrentPoint(this).Properties;
            if (!props.IsLeftButtonPressed || IsOnRow(e.Source)) return;
            if (OperatingSystem.IsMacOS() && e.KeyModifiers.HasFlag(InputModifiers.Control)) return; // ⌃クリック = 右クリック
            _movePressScreen = this.PointToScreen(e.GetPosition(this));
            _moveStartPosition = Position;
        }, RoutingStrategies.Bubble, handledEventsToo: true);

        Root.AddHandler(PointerMovedEvent, (_, e) =>
        {
            if (_movePressScreen is not { } press) return;
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) { _movePressScreen = null; return; }
            var now = this.PointToScreen(e.GetPosition(this));
            double scale = RenderScaling > 0 ? RenderScaling : 1;
            if (!_windowMoving)
            {
                if (Math.Abs(now.X - press.X) / scale < MoveThresholdPx && Math.Abs(now.Y - press.Y) / scale < MoveThresholdPx) return;
                _windowMoving = true;
                e.Pointer.Capture(Root); // タブのボタンからポインタを外す（離してもクリックにならない）
            }
            Position = new PixelPoint(_moveStartPosition.X + now.X - press.X, _moveStartPosition.Y + now.Y - press.Y);
            e.Handled = true;
        }, RoutingStrategies.Bubble, handledEventsToo: true);

        Root.AddHandler(PointerReleasedEvent, (_, e) =>
        {
            if (_windowMoving)
            {
                e.Pointer.Capture(null);
                e.Handled = true;
            }
            EndWindowMove();
        }, RoutingStrategies.Tunnel, handledEventsToo: true);
        Root.PointerCaptureLost += (_, _) => { if (_windowMoving) EndWindowMove(); };
    }

    private static bool IsOnRow(object? source)
    {
        for (var v = source as Visual; v is not null; v = v.GetVisualParent())
            if (v is Button b && b.Classes.Contains("row")) return true;
        return false;
    }

    private void EndWindowMove()
    {
        _movePressScreen = null;
        _windowMoving = false;
    }
}
