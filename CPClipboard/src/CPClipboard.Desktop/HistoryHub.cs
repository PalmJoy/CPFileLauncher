using Avalonia.Threading;
using CPClipboard.Core.Model;
using CPClipboard.Core.Storage;
using CPClipboard.Platform;

namespace CPClipboard.App;

internal sealed class HistoryHub : IDisposable
{
    private readonly VersionedJsonStore<HistoryBook> _store;
    private readonly SettingsHub _settings;
    private readonly IClipboardService _clipboard;
    private readonly DispatcherTimer _timer;
    private readonly TimeProvider _time;
    private long _token = -1;
    private int _generation;
    private bool _polling, _disposed, _readOnly;
    internal HistoryBook Book { get; }
    public event Action? Changed;

    public HistoryHub(DataPaths paths, SettingsHub settings, IClipboardService clipboard, TimeProvider? time = null)
    {
        _settings = settings; _clipboard = clipboard; _time = time ?? TimeProvider.System;
        _store = new(Path.Combine(paths.Root, "history.json"), 1, () => new HistoryBook());
        var loaded = _store.Load(); Book = loaded.Value;
        _readOnly = loaded.Status == LoadStatus.NewerSchemaReadOnly;
        if (Book.Prune(_time.GetUtcNow(), settings.Current.History)) Save();
        _settings.Changed += SettingsChanged;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += async (_, _) => await PollAsync();
    }
    public void Start() => _timer.Start();
    internal async Task PollAsync()
    {
        if (_polling || _disposed || _readOnly) return;
        _polling = true;
        int generation = _generation;
        try
        {
            bool changed = Book.Prune(_time.GetUtcNow(), _settings.Current.History);
            if (changed) { Save(); Changed?.Invoke(); }
            long token = _clipboard.ChangeToken;
            if (token == _token) return;
            var text = await _clipboard.GetHistoryTextAsync();
            if (_disposed || generation != _generation || token != _clipboard.ChangeToken) return;
            _token = token;
            if (Book.Add(text, _time.GetUtcNow(), _settings.Current.History)) { Save(); Changed?.Invoke(); }
        }
        catch (Exception ex) { AppLog.Info($"history: {ex.GetType().Name}"); }
        finally { _polling = false; }
    }
    public void Clear()
    {
        _generation++;
        _token = _clipboard.ChangeToken;
        Book.Entries.Clear(); Save(); Changed?.Invoke();
    }
    private void SettingsChanged(SettingsChange change)
    {
        if (!change.HasFlag(SettingsChange.History)) return;
        _generation++;
        if (Book.Prune(_time.GetUtcNow(), _settings.Current.History)) { Save(); Changed?.Invoke(); }
    }
    private void Save()
    {
        if (_readOnly) return;
        try { _store.Save(Book); }
        catch (Exception ex) { AppLog.Info($"history save: {ex.GetType().Name}"); }
    }
    public void Dispose()
    {
        _disposed = true; _generation++; _timer.Stop(); _settings.Changed -= SettingsChanged;
    }
}
