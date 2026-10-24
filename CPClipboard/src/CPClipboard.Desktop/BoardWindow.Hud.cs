using Avalonia.Threading;
using CPClipboard.Core.Hud;
using CPClipboard.Core.Model;

namespace CPClipboard.App;

/// <summary>盤面のステータス行（HUD 段階 A。SPEC §3.9）。時刻は分（または秒）の変わり目だけ書き換え、見えていない間は止める。</summary>
public partial class BoardWindow
{
    private readonly DispatcherTimer _clock = new();
    private bool _clockAttached;

    /// <summary>時刻のタイマーが動いているか（テスト用）。</summary>
    internal bool ClockRunning => _clock.IsEnabled;

    private void UpdateStatus()
    {
        var mode = _appearance.Hud.StatusBar ? _appearance.Hud.Clock : ClockMode.Off;
        bool date = _appearance.Hud.StatusBar && _appearance.Hud.Date;
        var group = _book[_groupKey];
        var (label, items, time) = StatusLine.Format(group.Key, group.Name, group.Snippets.Count, DateTime.Now, mode, date);
        StatusGroup.Text = label;
        StatusItems.Text = items;
        StatusClock.Text = time ?? "";
        UpdateClock();
    }

    /// <summary>時刻のタイマーを動かす / 止める（見えている・時刻を出す設定のときだけ動かす）。</summary>
    private void UpdateClock()
    {
        if (!_clockAttached)
        {
            _clockAttached = true;
            _clock.Tick += (_, _) =>
            {
                UpdateStatus(); // 次の変わり目までの間隔は UpdateClock が付け直す
            };
        }
        var mode = _appearance?.Hud is { StatusBar: true } hud ? hud.Clock : ClockMode.Off;
        bool date = _appearance?.Hud is { StatusBar: true, Date: true };
        bool run = (mode != ClockMode.Off || date) && IsVisible;
        if (!run)
        {
            _clock.Stop();
            return;
        }
        // 次の分（秒）の変わり目に合わせる。少し遅らせて、変わり目の直前に発火して同じ時刻を書くのを避ける
        _clock.Interval = StatusLine.NextTick(DateTime.Now, mode, date) + TimeSpan.FromMilliseconds(20);
        if (!_clock.IsEnabled) _clock.Start();
    }
}
