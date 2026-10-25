using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using CPClipboard.Core.Model;
using CPClipboard.Core.Storage;
using CPClipboard.Platform;
using CPClipboard.Platform.MacOS;
using CPClipboard.Platform.Windows;

namespace CPClipboard.App;

public partial class App : Application
{
    private IPlatformServices? _platform;
    private SingleInstance? _instance;
    private TrayIcon? _tray;
    private BoardWindow? _board;
    private NativeMenuItem? _permissionItem;
    private PermissionGuideWindow? _permissionGuide;
    private DispatcherTimer? _permissionPoll;
    private IClassicDesktopStyleApplicationLifetime? _desktop;
    private SettingsHub? _hub;
    private TemplatesHub? _templates;
    private PopupController? _popup;
    private SettingsWindow? _settingsWindow;
    private TemplatesWindow? _templatesWindow;
    private HistoryHub? _history;
    private HistoryWindow? _historyWindow;
    private PasteController? _historyPaste;
    private DataPaths? _paths;
    private AppDataStore? _store;
    private readonly BoardBackgroundLoader _backgroundLoader = new();
    private CancellationTokenSource? _backgroundCts;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        AppTheme.Install(this); // テーマのトークン（FluentTheme の後）
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // トレイ常駐アプリ: ウィンドウを全部閉じても終了しない。終了はトレイメニューから
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Start(desktop);
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void Start(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var store = _store = new AppDataStore(_paths = DataPaths.Resolve(DataPaths.ExecutableDirectory(Environment.ProcessPath, AppContext.BaseDirectory)));

        // 単一インスタンスのロックは Program.Main で取得済み（2 重起動はそこで終了している）
        _instance = Program.Instance ?? throw new InvalidOperationException("SingleInstance not acquired"); // i18n:ignore

        var (settingsResult, templatesResult) = store.LoadAll();
        var settings = settingsResult.Value;
        var book = templatesResult.Value;
        AppLog.Init(store.Paths.LogsDirectory, settings.Advanced.Logging);
        AppLog.Info($"起動: data={store.Paths.Root}{(store.Paths.IsPortable ? " (portable)" : "")} settings={settingsResult.Status} templates={templatesResult.Status} lang={Loc.StartupLanguage}({settings.General.Language})");

        if (settingsResult.Status == LoadStatus.CreatedDefault) store.SaveSettings(settings);
        if (templatesResult.Status == LoadStatus.CreatedDefault)
        {
            TemplateSamples.Fill(book); // 初回はグループ 1 に例文（SPEC §9.3）
            store.SaveTemplates(book);
        }
        NotifyLoadProblems(Strings.Notice_Load_Settings, settingsResult);
        NotifyLoadProblems(Strings.Notice_Load_Templates, templatesResult);

        _hub = new SettingsHub(store, settings);
        var hub = _hub;
        _templates = new TemplatesHub(store, book);
        Toast.Appearance = () => hub.Current.Appearance;
        ApplyColors(settings.Appearance.Colors); // 最初の描画の前に、配色で辞書を作り直す

        _desktop = desktop;
        if (OperatingSystem.IsWindows()) _platform = new WindowsPlatformServices();
        else if (OperatingSystem.IsMacOS()) _platform = new MacPlatformServices();
        else
        {
            Notice.Show(Strings.Notice_UnsupportedOs_Title, Strings.Notice_UnsupportedOs);
            return;
        }

        _history = new HistoryHub(store.Paths, _hub, _platform.Clipboard);
        _history.Start();
        _historyPaste = new PasteController(_platform, () => _historyWindow is null && _popup?.IsShown != true);
        if (_platform.InputHook is IHistoryTriggerService historyTriggers)
        {
            historyTriggers.ConfigureHistory(settings.HistoryTriggers);
            historyTriggers.HistoryTriggered += (_, _) => Dispatcher.UIThread.Post(() => OpenHistory(true));
        }

        _board = new BoardWindow { WheelSwitchesGroup = settings.Board.WheelSwitchesGroup };
        _board.ReducedMotion = () => _platform?.Window.PrefersReducedMotion == true;
        _board.AmbientLayers = Program.AvaloniaAmbient ? null : _platform.Ambient;
        _board.ApplyEffects(settings.Appearance);
        ApplyWindowEffects(settings.Appearance);
        _board.Render(book, settings.Appearance, PopupController.StartGroup(book, settings.Board.LastGroup));
        _ = ApplyBackgroundAsync(); // 背景画像は後から（起動・初回表示を待たせない）
        var templates = _templates;
        var popup = _popup = new PopupController(_board, _platform, _hub, () => templates.DisplayBook());
        popup.Attach();
        _board.SettingsRequested += OpenSettings;
        _board.AddRequested += group => OpenEditor(new TemplateEditTarget(group, null));
        _board.EditRequested += (group, id) => OpenEditor(new TemplateEditTarget(group, id));
        _board.UpdateRequested += () => { _popup?.Hide("update", restoreFocus: false); _ = CheckForUpdatesAsync(true); };
        _board.AboutRequested += () =>
        {
            _popup?.Hide("about", restoreFocus: false);
            AboutWindow.Show(_platform.Shell, store.Paths.Root, hub.Current.Appearance, store.Paths, _platform);
        };
        _board.PreviewEnabled = settings.Board.Preview;
        _hub.Changed += OnSettingsChanged;
        _templates.Changed += () => { if (_board.IsVisible) _board.Render(templates.DisplayBook(), hub.Current.Appearance, _board.GroupKey); };

        _platform.Hotkey.Register(settings.Triggers.Hotkey, out _);
        _platform.MouseTrigger.Configure(settings.Triggers);
        _platform.InputHook.ConfigureDoubleTap(settings.Triggers.DoubleTap);
        // フック開始に失敗しても設定画面・ガイドは勝手に開かない（SPEC §4.3）。トーストとトレイ / メニューバーの警告だけ
        _platform.InputHook.Failed += error => Dispatcher.UIThread.Post(() => OnHookFailed(error));

        _ = _instance.Listen(() => Dispatcher.UIThread.Post(() => _popup?.ShowFromExternal("2 重起動"/* i18n:ignore ログ用 */)));

        CreateTray(desktop);
        Dispatcher.UIThread.Post(() => _ = CheckForUpdatesAsync(false), DispatcherPriority.ApplicationIdle);
        AppLog.Info($"input permission: Accessibility={_platform.Permissions.Accessibility}");
        _platform.InputHook.Start();
        popup.Warmup(); // 初回表示の重さ対策
        desktop.ShutdownRequested += (_, e) =>
        {
            if (!_shutdownReady) { e.Cancel = true; Quit(desktop); }
        };
        desktop.Exit += (_, _) => Cleanup();
        if (!settings.General.HideStartupGuide)
            Dispatcher.UIThread.Post(() =>
                WindowActivation.ShowAndActivate(new StartupGuideWindow(hub, store.Paths, _platform), _platform),
                DispatcherPriority.ApplicationIdle);
        // 起動時間（SPEC §11）: プロセス開始からトリガーを受け付けられるまで。最初の描画が落ち着いた時点も出す
        AppLog.Info($"起動完了: {StartupMs():F0} ms（トレイ・入力フック・盤面の準備まで）");
        Dispatcher.UIThread.Post(() => AppLog.Info($"起動後の最初のアイドル: {StartupMs():F0} ms"), DispatcherPriority.ApplicationIdle);
    }

    private static double StartupMs() =>
        (DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime).TotalMilliseconds;

    private void CreateTray(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var show = new NativeMenuItem(Strings.Tray_ShowBoard);
        show.Click += (_, _) => _popup?.ShowFromExternal("トレイメニュー"); // i18n:ignore

        // グループ一覧（選ぶとそのグループで盤面を表示。SPEC §8）。開くたびに作り直す
        var groupsMenu = new NativeMenu();
        groupsMenu.NeedsUpdate += (_, _) => RebuildGroupsMenu(groupsMenu);
        RebuildGroupsMenu(groupsMenu);
        var groupsItem = new NativeMenuItem(Strings.Tray_Groups) { Menu = groupsMenu };
        var settingsItem = new NativeMenuItem(Strings.Common_Settings)
        {
            Gesture = new KeyGesture(Key.OemComma, OperatingSystem.IsMacOS() ? Avalonia.Input.KeyModifiers.Meta : Avalonia.Input.KeyModifiers.Control),
        };
        settingsItem.Click += (_, _) => OpenSettings();
        var historyItem = new NativeMenuItem(Strings.Menu_History);
        historyItem.Click += (_, _) => OpenHistory();
        var editItem = new NativeMenuItem(Strings.Menu_Edit);
        editItem.Click += (_, _) => OpenEditor();
        _permissionItem = new NativeMenuItem(Strings.Tray_Permissions) { IsVisible = false }; // 未許可のときだけ出す（Mac）
        _permissionItem.Click += (_, _) => ShowPermissionGuide();
        var update = new NativeMenuItem(Strings.Update_Check);
        update.Click += (_, _) => _ = CheckForUpdatesAsync(true);
        var quit = new NativeMenuItem(Loc.Os(Strings.Tray_Quit_Win, Strings.Tray_Quit_Mac)) { Gesture = OperatingSystem.IsMacOS() ? new KeyGesture(Key.Q, Avalonia.Input.KeyModifiers.Meta) : null };
        quit.Click += (_, _) => Quit(desktop);

        if (OperatingSystem.IsMacOS())
        {
            // アプリメニュー（盤面がキーのときに ⌘Q を効かせる）
            var appSettings = new NativeMenuItem(Strings.Common_Settings) { Gesture = new KeyGesture(Key.OemComma, Avalonia.Input.KeyModifiers.Meta) };
            appSettings.Click += (_, _) => OpenSettings();
            var appHistory = new NativeMenuItem(Strings.Menu_History);
            appHistory.Click += (_, _) => OpenHistory();
            var appEdit = new NativeMenuItem(Strings.Menu_Edit);
            appEdit.Click += (_, _) => OpenEditor();
            var appQuit = new NativeMenuItem(Strings.Tray_Quit_Mac) { Gesture = new KeyGesture(Key.Q, Avalonia.Input.KeyModifiers.Meta) };
            appQuit.Click += (_, _) => Quit(desktop);
            var appUpdate = new NativeMenuItem(Strings.Update_Check);
            appUpdate.Click += (_, _) => _ = CheckForUpdatesAsync(true);
            NativeMenu.SetMenu(this, new NativeMenu { Items = { appSettings, appEdit, appHistory, appUpdate, new NativeMenuItemSeparator(), appQuit } });
        }

        _tray = new TrayIcon
        {
            Icon = AppIcon.Create(),
            ToolTipText = AppInfo.ProductName,
            Menu = new NativeMenu { Items = { show, groupsItem, new NativeMenuItemSeparator(), settingsItem, editItem, historyItem, _permissionItem, update, new NativeMenuItemSeparator(), quit } },
            IsVisible = true,
        };
        MacOSProperties.SetIsTemplateIcon(_tray, AppIcon.IsTemplate(warning: false));
        _tray.Clicked += (_, _) => _popup?.Toggle("トレイ"); // i18n:ignore
        TrayIcon.SetIcons(this, new TrayIcons { _tray });
    }

    /// <summary>終了。保存待ちを書き切ってから。</summary>
    private bool _quitting;
    private bool _shutdownReady;
    private async void Quit(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_quitting) return;
        _quitting = true;
        _popup?.Hide("quit", restoreFocus: false);
        _history?.Dispose();
        _historyWindow?.Close();
        if (_historyPaste is not null) await _historyPaste.StopAsync();
        if (_popup is not null) await _popup.StopPastingAsync();
        _shutdownReady = true;
        _templates?.RemoveEmpty();
        _templates?.Flush();
        _hub?.Flush();
        desktop.Shutdown();
    }

    /// <summary>トレイの「グループ ▸」: 0〜9 の番号・名前・件数（SPEC §8）。</summary>
    private void RebuildGroupsMenu(NativeMenu menu)
    {
        menu.Items.Clear();
        if (_templates is null) return;
        foreach (var group in _templates.DisplayBook().Groups)
        {
            int key = group.Key;
            string label = group.Name.Length > 0 ? $"{key} · {group.Name} ({group.Snippets.Count})" : $"{key} ({group.Snippets.Count})"; // i18n:ignore
            var item = new NativeMenuItem(label);
            item.Click += (_, _) => _popup?.ShowFromExternal("グループ一覧", key); // i18n:ignore
            menu.Items.Add(item);
        }
    }

    // ---------------- 設定（SPEC §7） ----------------

    /// <summary>設定画面を開く（ユーザーが選んだときだけ。既に開いていれば前面へ）。盤面が出ていれば閉じる（SPEC §3.5）。</summary>
    private void OpenSettings()
    {
        if (_hub is null || _platform is null) return;
        _templates?.Flush();
        _popup?.Hide("settings", restoreFocus: false);
        if (_settingsWindow is null)
        {
            var platform = _platform;
            _settingsWindow = new SettingsWindow(new SettingsContext(_hub, platform, _paths!,
                () => _popup?.IsShown == true && _board is not null ? _board.Position : null,
                dir => platform.Shell.Open(dir),
                ShowPermissionGuide,
                FlushAll: () => { _templates?.Flush(); _hub?.Flush(); },
                ImportAndRestart: ImportAndRestart,
                BackgroundStatus: () => _backgroundLoader.LastStatus,
                BackgroundBitmap: () => _backgroundLoader.Current,
                Restart: Restart,
                ClearHistory: () => _history?.Clear()));
            _settingsWindow.Closed += (_, _) =>
            {
                _settingsWindow = null;
                MemoryTrim.Schedule(() => _settingsWindow is null && _templatesWindow is null && _popup?.IsShown != true); // 設定画面の部品を返す
            };
        }
        WindowActivation.ShowAndActivate(_settingsWindow, _platform);
    }

    private void OpenEditor(TemplateEditTarget? target = null)
    {
        if (_templates is null || _platform is null) return;
        _popup?.Hide("editor", restoreFocus: false);
        if (_templatesWindow is null)
        {
            _templatesWindow = new TemplatesWindow(_templates, _platform.Clipboard, _hub, () => _backgroundLoader.Current);
            _templatesWindow.SetTemplateTarget(target ?? new TemplateEditTarget(_board?.GroupKey ?? 0, null));
            _templatesWindow.Closed += (_, _) =>
            {
                _templatesWindow = null;
                MemoryTrim.Schedule(() => _templatesWindow is null && _settingsWindow is null && _popup?.IsShown != true);
            };
        }
        else if (target is not null) _templatesWindow.SetTemplateTarget(target);
        WindowActivation.ShowAndActivate(_templatesWindow, _platform);
    }

    private void OpenHistory(bool toggle = false)
    {
        if (_quitting || _history is null || _hub is null || _platform is null) return;
        if (_historyWindow is { IsVisible: true })
        {
            if (toggle) _historyWindow.Close();
            else WindowActivation.ShowAndActivate(_historyWindow, _platform);
            return;
        }
        nint target = _platform.Window.CaptureForeground();
        _popup?.Hide("history", restoreFocus: false);
        var window = new HistoryWindow(_history, _hub, () => _backgroundLoader.Current, _platform.Window);
        _historyWindow = window;
        bool chosen = false;
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        window.Chosen += async (text, copyOnly) =>
        {
            if (chosen) return;
            chosen = true;
            window.Close();
            // Close may be delayed by the hide effect; wait until the Closed event clears the window.
            await closed.Task;
            if (_historyPaste is null) return;
            var paste = _hub.Current.Paste;
            await _historyPaste.ExecuteAsync(new CPClipboard.Core.Paste.PasteRequest(text, target, copyOnly,
                paste.AutoPaste, paste.RestoreClipboard, paste.RestoreDelayMs));
        };
        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_historyWindow, window)) _historyWindow = null;
            closed.TrySetResult();
            if (!chosen && target != 0) _platform.Window.RestoreForeground(target);
        };
        window.Show();
        if (window.TryGetPlatformHandle()?.Handle is { } handle)
        {
            _platform.Window.ConfigureBoardWindow(handle);
            _platform.Window.BringToForeground(handle);
        }
        window.Activate();
    }

    /// <summary>設定が変わったら、関係する所だけ反映し直す（即時反映）。</summary>
    private void OnSettingsChanged(SettingsChange change)
    {
        if (_hub is null || _board is null || _platform is null || _templates is null) return;
        var s = _hub.Current;
        if (change.HasFlag(SettingsChange.Colors)) ApplyColors(s.Appearance.Colors);
        if (change.HasFlag(SettingsChange.Opacity)) _board.ApplyOpacity(s.Appearance.Opacity);
        // テーマを変えると演出の既定値と発光の余白（ウィンドウの大きさ）も変わる
        if (change.HasFlag(SettingsChange.Effects)) { _board.ApplyEffects(s.Appearance); ApplyWindowEffects(s.Appearance); }
        if (change.HasFlag(SettingsChange.BoardLayout) || change.HasFlag(SettingsChange.Hud)) _board.Render(_templates.DisplayBook(), s.Appearance, _board.GroupKey);
        _board.WheelSwitchesGroup = s.Board.WheelSwitchesGroup;
        _board.PreviewEnabled = s.Board.Preview;
        if (change.HasFlag(SettingsChange.Triggers))
        {
            _platform.Hotkey.Register(s.Triggers.Hotkey, out _);
            _platform.MouseTrigger.Configure(s.Triggers);
            _platform.InputHook.ConfigureDoubleTap(s.Triggers.DoubleTap);
            if (_platform.InputHook is IHistoryTriggerService history) history.ConfigureHistory(s.HistoryTriggers);
        }
        if (change.HasFlag(SettingsChange.Logging)) AppLog.SetEnabled(s.Advanced.Logging);
        if (change.HasFlag(SettingsChange.Background)) _ = ApplyBackgroundAsync();
    }

    /// <summary>背景画像を読み込んで盤面に敷く（SPEC §3.7）。続けて変わったら前の読み込みは捨てる。</summary>
    private async Task ApplyBackgroundAsync()
    {
        if (_hub is null || _board is null || _paths is null) return;
        _backgroundCts?.Cancel();
        var cts = _backgroundCts = new CancellationTokenSource();
        var bg = _hub.Current.Appearance.Background;
        try
        {
            var bitmap = await _backgroundLoader.LoadAsync(BackgroundImageStore.Resolve(_paths, bg.Image), cts.Token);
            if (cts.IsCancellationRequested) return;
            _board.ApplyBackground(_hub.Current.Appearance.Background, bitmap);
            if (bg.Image is not null) AppLog.Info($"背景画像: {bg.Image} → {_backgroundLoader.LastStatus}");
            _hub.NotifyBackgroundLoaded();
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>設定画面・ダイアログも盤面と同じ表示・非表示の演出（グリッチ）で出し入れする。</summary>
    private static void ApplyWindowEffects(AppearanceSettings appearance)
    {
        ChromeWindow.ShowEffect = Core.Effects.EffectCatalog.Resolve(appearance, Core.Effects.EffectCatalog.BoardShow);
        ChromeWindow.HideEffect = Core.Effects.EffectCatalog.Resolve(appearance, Core.Effects.EffectCatalog.BoardHide);
        ChromeWindow.GlitchLook = Core.Effects.GlitchLook.Scaled(appearance.GlitchIntensity);
    }

    /// <summary>配色（主色・副色。SPEC §3.6）を見た目に反映する。値は Normalize 済み（#RRGGBB）。</summary>
    private void ApplyColors(ColorSettings colors)
    {
        if (Core.Theming.RgbColor.TryParse(colors.Primary) is { } p && Core.Theming.RgbColor.TryParse(colors.Secondary) is { } s)
            AppTheme.SetColors(this, p, s);
    }

    // ---------------- 権限・フック失敗（SPEC §4.3） ----------------

    private void OnHookFailed(string error)
    {
        AppLog.Info($"入力フック開始失敗: {error}");
        if (_platform is null) return;
        SetTriggerWarning(true);

        bool needsPermission = _platform.Permissions.Accessibility != PermissionState.Granted;
        if (_permissionItem is not null) _permissionItem.IsVisible = needsPermission;
        Toast.Show(needsPermission
            ? Strings.Toast_TriggerNeedsPermission
            : Strings.FormatToast_TriggerHookFailed(Loc.Os(Strings.Common_TrayIcon_Win, Strings.Common_TrayIcon_Mac), error), seconds: 10);
        if (needsPermission) StartPermissionPolling();
    }

    /// <summary>許可されたら自動でフックを開始し直す（2 秒ごと）。成功したら警告を消して止める。</summary>
    private void StartPermissionPolling()
    {
        if (_permissionPoll is not null || _platform is null) return;
        var platform = _platform;
        _permissionPoll = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _permissionPoll.Tick += (_, _) =>
        {
            if (platform.InputHook.IsRunning)
            {
                AppLog.Info("入力フック開始（権限許可後）");
                _permissionPoll?.Stop();
                _permissionPoll = null;
                SetTriggerWarning(false);
                if (_permissionItem is not null) _permissionItem.IsVisible = false;
                Toast.Show(Strings.Toast_TriggerEnabled, error: false);
                return;
            }
            if (platform.Permissions.Accessibility == PermissionState.Granted) platform.InputHook.Start();
        };
        _permissionPoll.Start();
    }

    private void SetTriggerWarning(bool warning)
    {
        if (_tray is null) return;
        _tray.Icon = AppIcon.Create(warning);
        MacOSProperties.SetIsTemplateIcon(_tray, AppIcon.IsTemplate(warning));
        _tray.ToolTipText = warning ? Strings.Tray_TriggerDisabled_Tooltip : AppInfo.ProductName;
    }

    private void ShowPermissionGuide()
    {
        if (_platform is null) return;
        if (_permissionGuide is null)
        {
            _permissionGuide = new PermissionGuideWindow(_platform, RetryHook, Restart);
            _permissionGuide.Closed += (_, _) => _permissionGuide = null;
        }
        // 常駐アプリ（Dock に出ない）なので明示的に前面化する。閉じたら直前のアプリへ返す（SPEC §3.5）
        WindowActivation.ShowAndActivate(_permissionGuide, _platform);
    }

    private void RetryHook()
    {
        AppLog.Info("入力フック再試行");
        _platform?.InputHook.Start();
        StartPermissionPolling();
    }

    /// <summary>エクスポートした zip で settings.json / templates.json を差し替えて再起動する（spec/SETTINGS.md データタブ）。</summary>
    private void ImportAndRestart(ArchiveContents contents)
    {
        if (_store is null) return;
        _templates?.RemoveEmpty();
        _templates?.Flush();
        _hub?.Flush();
        try
        {
            _store.Import(contents, DateTime.Now); // 以後このプロセスは保存しない（終了処理で上書きしないため）
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Toast.Show(Strings.FormatToast_ImportFailed(ex.Message));
            return;
        }
        AppLog.Info($"インポート: settings={contents.Settings is not null} templates={contents.Templates is not null}");
        Restart();
    }

    /// <summary>権限の反映に再起動が要る場合のため、自分をもう一度起動して終了する。</summary>
    private async void Restart()
    {
        if (Environment.ProcessPath is not { } exe || _quitting) return;
        _quitting = true;
        if (_popup is not null) await _popup.StopPastingAsync();
        _shutdownReady = true;
        _templates?.RemoveEmpty();
        _templates?.Flush(); // 再起動の前に保存待ちを書き切る（言語の変更など）
        _hub?.Flush();
        AppLog.Info("再起動");
        _instance?.Dispose(); // 新しいプロセスが単一インスタンスのロックを取れるように先に放す
        _instance = null;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe) { UseShellExecute = false });
        _desktop?.Shutdown();
    }

    private static void NotifyLoadProblems<T>(string what, LoadResult<T> result)
    {
        var d = result.Detail ?? new LoadDetail();
        string? message = result.Status switch
        {
            LoadStatus.RecoveredFromBackup => Strings.FormatNotice_Load_Recovered(what, d.BackupFile, d.BrokenFile, d.Error),
            LoadStatus.CorruptReplacedWithDefault => Strings.FormatNotice_Load_ReplacedWithDefault(what, d.BrokenFile, d.Error),
            LoadStatus.NewerSchemaReadOnly => Strings.FormatNotice_Load_NewerSchema(what, d.FileVersion, d.SupportedVersion),
            _ => null,
        };
        if (message is null) return;
        AppLog.Info($"読み込みの問題: {what} {result.Status} {d}");
        Notice.Show(Strings.Notice_Load_Title, message);
    }

    private void Cleanup()
    {
        AppLog.Info("終了");
        _updateCts.Cancel();
        _updateWindow?.Close();
        _backgroundCts?.Cancel();
        _templates?.RemoveEmpty();
        _templates?.Flush();
        _hub?.Flush();
        _historyWindow?.Close();
        _history?.Dispose();
        if (_tray is not null) _tray.IsVisible = false;
        if (_board is not null)
        {
            _board.AllowClose = true;
            _board.Close();
        }
        _platform?.Dispose();
        _instance?.Dispose();
    }
}
