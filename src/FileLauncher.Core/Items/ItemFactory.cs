using FileLauncher.Core.Model;

namespace FileLauncher.Core.Items;

/// <summary>.lnk（Win）/ エイリアス（Mac）を読んだ結果。Target が空ならシェル項目へのリンク（エクスプローラー等）。</summary>
public sealed record ShortcutInfo(string Target, string Arguments, string WorkingDirectory);

/// <summary>
/// ドロップされたパス・テキストからアイテムを作る（SPEC §5.1, §6.1）。
/// .lnk はリンク先を解決して登録し、元の .lnk は LinkPath に保持する（アイコンは .lnk 自身から取る。SPEC §5.4）。
/// </summary>
public sealed class ItemFactory
{
    /// <summary>「アプリケーション」として扱う拡張子（それ以外のファイルは「ファイル」）。</summary>
    private static readonly HashSet<string> AppExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".bat", ".cmd", ".com", ".msc", ".cpl", ".appref-ms",
    };

    private static readonly string[] UrlSchemes = ["http", "https", "ftp", "mailto"];

    private readonly Func<string, ShortcutInfo?> _readShortcut;
    private readonly bool _fileKindTakesArgs;

    /// <param name="readShortcut">ショートカットを読む（IShellService.ReadShortcut）。ショートカットでなければ null。</param>
    /// <param name="fileKindTakesArgs">ファイル種別に引数を渡せるか（macOS は不可。SPEC §5.1）。null は実行中の OS で決める。</param>
    public ItemFactory(Func<string, ShortcutInfo?> readShortcut, bool? fileKindTakesArgs = null)
    {
        _readShortcut = readShortcut;
        _fileKindTakesArgs = fileKindTakesArgs ?? !OperatingSystem.IsMacOS();
    }

    /// <summary>ファイル・フォルダのパスから。存在しなければ null。</summary>
    public LauncherItem? FromPath(string path)
    {
        path = path.Trim().Trim('"');
        if (path.Length == 0) return null;

        if (Directory.Exists(path))
        {
            // Finder からは "/Applications/Safari.app/" のように末尾区切り付きで届く（SPEC §5.1）。ルート "/" や "C:\" は残す
            path = TrimTrailingSeparator(path);
            return new LauncherItem
            {
                Kind = KindOf(path),
                Target = path,
                Name = DisplayNameOf(path),
            };
        }
        if (!File.Exists(path)) return null;

        // ショートカットか（Win: .lnk、Mac: Finder エイリアス）は Platform が判定する。違えば null
        if (_readShortcut(path) is { } link)
        {
            var item = new LauncherItem { Name = Path.GetFileNameWithoutExtension(path) is { Length: > 0 } ln ? ln : Path.GetFileName(path), LinkPath = path };
            if (string.IsNullOrEmpty(link.Target))
            {
                // シェル項目へのリンク（エクスプローラー、ストアアプリ等）: .lnk 自体を起動する
                item.Kind = ItemKind.App;
                item.Target = path;
                return item;
            }
            item.Target = Directory.Exists(link.Target) ? TrimTrailingSeparator(link.Target) : link.Target;
            item.Kind = KindOf(item.Target);
            item.Args = link.Arguments;
            item.WorkingDir = string.IsNullOrEmpty(link.WorkingDirectory) ? null : link.WorkingDirectory;
            return item;
        }

        return new LauncherItem
        {
            Kind = KindOf(path),
            Target = path,
            Name = Path.GetFileNameWithoutExtension(path) is { Length: > 0 } n ? n : Path.GetFileName(path),
        };
    }

    /// <summary>
    /// テキストから（ブラウザからの URL / リンク、パス文字列、「パス + 引数」）。解釈できなければ null。
    /// 規則は SPEC §6.1「テキストの解釈」。
    /// </summary>
    public LauncherItem? FromText(string text)
    {
        var line = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        if (line is null) return null;
        var unquoted = line.Trim('"');

        // Uri は "C:\a.exe -x" や "/Applications/X.app" も暗黙の file URI として受け付けるので、file URL は "file:" で始まるときだけ
        if (unquoted.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            return Uri.TryCreate(unquoted, UriKind.Absolute, out var fileUri) && fileUri.IsFile ? FromPath(fileUri.LocalPath) : null;

        if (Uri.TryCreate(unquoted, UriKind.Absolute, out var uri) && !uri.IsFile
            && UrlSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase))
        {
            return new LauncherItem
            {
                Kind = ItemKind.Url,
                Target = unquoted,
                Name = uri.Scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase) ? uri.UserInfo + "@" + uri.Host : uri.Host,
            };
        }
        return FromCommandLine(line);
    }

    /// <summary>"パス" 引数 / パス 引数 / パス のいずれか。</summary>
    private LauncherItem? FromCommandLine(string line)
    {
        if (line.StartsWith('"'))
        {
            int close = line.IndexOf('"', 1);
            if (close > 0)
            {
                string quoted = line[1..close], args = line[(close + 1)..].Trim();
                if (args.Length == 0) return FromTextPath(quoted);
                return IsPathCandidate(quoted) ? WithArgs(FromTextPath(quoted), args) : null;
            }
            return FromTextPath(line.Trim('"')); // 閉じる " が無い: 従来どおり行全体
        }

        if (FromTextPath(line) is { } whole) return whole;

        // 空白の位置で前から区切り、左側が最初に候補になった所で分ける（CreateProcess と同じ順序）
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] is not (' ' or '\t')) continue;
            string left = line[..i];
            if (left.Trim().Length == 0 || !IsPathCandidate(left)) continue;
            string args = line[(i + 1)..].Trim();
            return args.Length == 0 ? FromTextPath(left) : WithArgs(FromTextPath(left), args);
        }
        return null;
    }

    /// <summary>引数と組めるパス = 実在するファイルか .app バンドル（ただのフォルダは誤分割を避けるため外す）。</summary>
    private static bool IsPathCandidate(string raw)
    {
        var path = LaunchArgs.ExpandPath(raw.Trim());
        return File.Exists(path) || (Directory.Exists(path) && IsMacAppBundle(path));
    }

    /// <summary>テキスト中のパス: 環境変数を展開して確かめ、保存する Target / LinkPath は展開前のまま。</summary>
    private LauncherItem? FromTextPath(string raw)
    {
        raw = raw.Trim().Trim('"');
        var expanded = LaunchArgs.ExpandPath(raw);
        if (FromPath(expanded) is not { } item) return null;
        if (expanded != raw)
        {
            if (item.LinkPath is { } link)
            {
                if (item.Target == link) item.Target = raw; // シェル項目へのリンクは .lnk 自体が Target
                item.LinkPath = raw;
            }
            else
            {
                item.Target = TrimTrailingSeparator(raw);
            }
        }
        return item;
    }

    /// <summary>貼り付けた引数を付ける。リンク先がフォルダ・Mac のファイル種別には付けない。</summary>
    private LauncherItem? WithArgs(LauncherItem? item, string args)
    {
        if (item is null || item.Kind == ItemKind.Folder) return item;
        if (item.Kind == ItemKind.File && !_fileKindTakesArgs)
        {
            item.Args = string.Empty;
            return item;
        }
        item.Args = args; // .lnk 側の引数より、今見えている文字列を優先する
        return item;
    }

    private static ItemKind KindOf(string path)
    {
        if (Directory.Exists(path)) return IsMacAppBundle(path) ? ItemKind.App : ItemKind.Folder;
        return AppExtensions.Contains(Path.GetExtension(path)) ? ItemKind.App : ItemKind.File;
    }

    private static bool IsMacAppBundle(string path) =>
        path.TrimEnd('/').EndsWith(".app", StringComparison.OrdinalIgnoreCase);

    private static string TrimTrailingSeparator(string path)
    {
        var trimmed = path.TrimEnd('/', '\\');
        if (trimmed.Length == 0) return path;                       // "/"
        if (trimmed.Length == 2 && trimmed[1] == ':') return path;   // "C:\"
        return trimmed;
    }

    /// <summary>
    /// 表示名の既定（編集ダイアログで空のまま OK したとき。SPEC §6.4）: フォルダはフォルダ名（.app は拡張子なし）、
    /// ファイル・アプリは拡張子なしのファイル名、URL はホスト名（mailto はアドレス）、コマンドは先頭の語。
    /// </summary>
    public static string DefaultName(ItemKind kind, string target)
    {
        target = (target ?? "").Trim().Trim('"');
        if (target.Length == 0) return "";
        switch (kind)
        {
            case ItemKind.Url when Uri.TryCreate(target, UriKind.Absolute, out var uri):
                return uri.Scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase) ? uri.UserInfo + "@" + uri.Host
                    : uri.Host.Length > 0 ? uri.Host : target;
            case ItemKind.Command:
                return target.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0];
            case ItemKind.Folder:
                return DisplayNameOf(target);
            default:
                if (target.TrimEnd('/', '\\').EndsWith(".app", StringComparison.OrdinalIgnoreCase)) return DisplayNameOf(target);
                var trimmed = target.TrimEnd('/', '\\');
                return Path.GetFileNameWithoutExtension(trimmed) is { Length: > 0 } n ? n : Path.GetFileName(trimmed) is { Length: > 0 } f ? f : trimmed;
        }
    }

    private static string DisplayNameOf(string directory)
    {
        var trimmed = directory.TrimEnd('\\', '/');
        var name = Path.GetFileName(trimmed);
        if (name.EndsWith(".app", StringComparison.OrdinalIgnoreCase)) name = name[..^4];
        return name.Length > 0 ? name : trimmed; // ドライブ "C:\" → "C:"
    }
}
