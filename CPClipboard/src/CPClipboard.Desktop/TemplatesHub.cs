using Avalonia.Threading;
using CPClipboard.Core.Model;
using CPClipboard.Core.Storage;

namespace CPClipboard.App;

/// <summary>
/// 定型文の保持・変更・保存（SPEC §6.4）。変更は Edit で行い、Changed で盤面などへ知らせ、保存は 500 ms まとめて書く。
/// 読み取り専用（新しい版のデータ）なら書かない。UI スレッドから呼ぶこと。
/// </summary>
internal sealed class TemplatesHub
{
    private static readonly TimeSpan SaveDelay = TimeSpan.FromMilliseconds(500);

    private readonly AppDataStore _store;
    private readonly DispatcherTimer _saveTimer;
    private bool _dirty;

    public TemplatesHub(AppDataStore store, TemplateBook book)
    {
        _store = store;
        Current = book;
        _saveTimer = new DispatcherTimer { Interval = SaveDelay };
        _saveTimer.Tick += (_, _) => Flush();
    }

    public TemplateBook Current { get; }

    public bool IsReadOnly => _store.TemplatesStore.IsReadOnly;

    public event Action? Changed;

    /// <summary>1 段階の元に戻す（一覧操作だけ。SPEC §6.3）。</summary>
    public TemplateHistory History { get; } = new();

    public bool CanUndo => History.CanUndo;

    /// <summary>
    /// 定型文タブの編集（SPEC §6.3）。undoable = 一覧操作（追加・複製・削除・並べ替え・移動・グループを空にする）なら操作の直前を控える。
    /// 本文・名前の編集（undoable = false）で変わったら控えを捨てる（戻すとその編集まで消えるため）。変わったら知らせて保存を予約。
    /// </summary>
    public EditResult Edit(Func<TemplateBook, EditResult> op, Guid? selected, bool undoable, int? selectedGroup = null)
    {
        var before = undoable ? Current.Clone() : null;
        var result = op(Current);
        if (!result.Changed) return result;
        if (before is not null) History.Record(before, selected, selectedGroup);
        else History.Discard();
        Commit();
        return result;
    }

    /// <summary>直前の一覧操作を戻す。戻したら true（選び直す定型文は selected）。</summary>
    public bool Undo(out Guid? selected) => Undo(out selected, out _);

    public bool Undo(out Guid? selected, out int? selectedGroup)
    {
        if (!History.Undo(Current, out selected, out selectedGroup)) return false;
        Commit();
        return true;
    }

    /// <summary>空の定型文を取り除く（元に戻すの控えは消さない。SPEC §6.3）。</summary>
    public int RemoveEmpty(Guid? keep = null)
    {
        int removed = TemplateEditing.RemoveEmpty(Current, keep);
        if (removed > 0) Commit();
        return removed;
    }

    public TemplateBook DisplayBook()
    {
        var display = Current.Clone();
        TemplateEditing.RemoveEmpty(display);
        return display;
    }

    private void Commit()
    {
        Current.Normalize();
        Changed?.Invoke();
        ScheduleSave();
    }

    /// <summary>予約中の保存を今すぐ書く（設定画面を閉じたとき・終了時）。</summary>
    public void Flush()
    {
        _saveTimer.Stop();
        if (!_dirty) return;
        _dirty = false;
        if (IsReadOnly)
        {
            AppLog.Info("定型文は読み取り専用（新しい版のデータ）なので保存しません");
            return;
        }
        _store.SaveTemplates(Current);
        AppLog.Info("定型文を保存");
    }

    private void ScheduleSave()
    {
        _dirty = true;
        _saveTimer.Stop();
        _saveTimer.Start();
    }
}

/// <summary>初回の例文（SPEC §9.3）。文言は UI 言語のリソースから（Core は文言を持たない）。</summary>
internal static class TemplateSamples
{
    /// <summary>グループ 1 が空なら例文を 3 件入れる。</summary>
    public static void Fill(TemplateBook book)
    {
        var group = book[1];
        if (group.Snippets.Count > 0) return;
        foreach (var text in new[] { Strings.Templates_Sample_1, Strings.Templates_Sample_2, Strings.Templates_Sample_3 })
            group.Snippets.Add(new Snippet { Text = TemplateBook.NormalizeNewlines(text) });
    }
}
