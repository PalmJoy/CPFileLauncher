# CPClipboard UI 用語の対訳表と文言の規則（日本語 / English）

UI 文字列（SPEC §7）の **英訳の正**。`src/CPClipboard.Desktop/Resources/Strings.resx`（英語、既定）と `Strings.ja.resx`（日本語）の訳語はこの表に合わせる。
訳語を変えたいときは**先にこの表を直してから** resx を直す。CPFileLauncher から持ち込んだ文言（演出・配色・背景画像・HUD・権限ガイドなど）は、ここに無ければ CPFileLauncher の訳語をそのまま使い、製品名だけ置き換える。

## 1. 英語の文体

- **Sentence case**（最初の語だけ大文字。固有名詞・製品名は除く）。項目名・ボタン・メニュー・注記・トーストのすべて。
- 続きがある操作（ダイアログを開く・選ぶ）は日英とも「…」（U+2026）を付ける。
- 注記・トーストは完全な文にしてピリオドを付ける。項目名・ボタン・メニューには付けない。
- 引用符は日本語「　」、英語 “ ”。引用符はリソースの文字列の中に書く。
- 件数が入る文は `_One` / `_Other` の 2 キー（日本語は同文、`Loc.Count`）。
- 数・日付の書式は OS の設定（`CurrentCulture`）に従う。HUD の時刻・日付は固定書式（§5）。

## 2. 用語

### 2.1 基本概念（SPEC §2）

| 日本語 | English（推奨） | 候補 | 備考 |
|---|---|---|---|
| 定型文 | snippet | template / fixed phrase | 識別子 `Snippet` に合わせる。編集ウィンドウの対象として使用 |
| グループ | group | set | 「グループ 2」= "Group 2"。名前付きは "2 · Greetings" |
| 盤面 | board | panel | 「盤面を表示」= "Show board" |
| 行 | row | | UI にはほぼ出ない |
| トリガー / 呼び出し | trigger | | 「呼び出し」タブ = "Triggers" |
| 修飾キーの 2 回押し | Double-tap modifier key | Double-press | `Settings_Triggers_DoubleTap` |
| マウス操作 | mouse trigger | | |
| ホットキー | hotkey | | |
| 貼り付け | paste | | |
| コピーだけ | Copy only | Copy without pasting | メニュー・注記 |
| 貼り付け先 | target app | | UI にはほぼ出ない |
| 全文のプレビュー | Full-text preview | Preview | |
| 演出 / 光の玉 / 発光の明滅 / 走査線の帯 | effect / Light orb / Glow pulse / Scan beam | | CPFileLauncher と同じ |

### 2.2 トレイ / メニューバー・右クリックメニュー（SPEC §6.2 / §8）

| 日本語 | English（推奨） | 備考 |
|---|---|---|
| 盤面を表示 | Show board | |
| グループ ▸ | Groups ▸ | 項目は「0」「1 · 仕事 (12)」= "0", "1 · Work (12)" |
| 設定… | Settings… | |
| 権限の設定… | Permissions… | macOS のみ |
| 終了 / CPClipboard を終了 | Exit / Quit CPClipboard | OS 別キー（`_Win` / `_Mac`） |
| CPClipboard について | About CPClipboard | 見出しは製品名 "CPClipboard"、副題 "CyberPunk Clipboard"（両言語とも英字） |
| 貼り付け | Paste | 行のメニュー |
| コピーだけ | Copy only | 行のメニュー。ショートカット表示 Shift+Enter / ⇧↩ |
| 編集… | Edit… | 対象の定型文を独立編集ウィンドウで選択 |

### 2.3 設定画面（項目の定義は `spec/SETTINGS.md`）

| 日本語 | English（推奨） | 備考 |
|---|---|---|
| 一般 / 表示 / 演出 / トリガー / 貼り付け / データ / 詳細 | General / Appearance / Effects / Triggers / Paste / Data / Advanced | 7 タブ。設定本体620×600、独立した編集本体820×600 |
| 起動時のガイドを表示しない | Don't show the startup guide | |
| 盤面の幅 / 最大行数 / 文字の大きさ | Board width / Maximum rows / Text size | |
| 全文のプレビュー | Show full-text preview | |
| ホイールでグループを切り替える | Scroll wheel switches groups | |
| 修飾キーの 2 回押し / キー / 判定の間隔 | Double-tap modifier key / Key / Double-tap interval | |
| マウスのボタン | 中 = Middle / 左 = Left / 右 = Right / サイド 1 = Side 1 / サイド 2 = Side 2 | |
| 左・右ボタンには修飾キーが必要です。 | Left and right buttons require a modifier key. | `Settings_Triggers_LeftWithoutModifier`。左・右の両方に共通のエラー |
| 自動で貼り付ける | Paste automatically | |
| 貼り付け後にクリップボードを元に戻す | Restore the clipboard after pasting | |
| OFF（既定）のままなら、貼り付け後も定型文がクリップボードに残ります。 | Leave this off (the default) to keep the snippet on the clipboard after pasting. | 貼り付け後の復元設定の注記 |
| 元に戻すまでの時間 | Restore delay | |
| （編集ウィンドウ）追加 / クリップボードから追加 / 複製 / 削除 / 移動 ▸ / 元に戻す | Add / Add from clipboard / Duplicate / Delete / Move to ▸ / Undo | |
| グループの名前 | Group name | |
| このグループには定型文を500件まで登録できます。 | This group can contain up to 500 snippets. | `Settings_Snippets_GroupFull` |
| 定型文は10,000文字以内にしてください。 | Snippets must be 10,000 characters or fewer. | `Settings_Snippets_TooLong` |
| クリップボードに追加できる文字がありません。 | The clipboard contains no text to add. | `Settings_Snippets_ClipboardEmpty`。空白・改行だけも含む |
| 新しい版の定型文データのため、変更はこのセッションだけに反映され、保存されません。 | This snippet data was created by a newer version. Changes apply only to this session and won’t be saved. | `Settings_Snippets_ReadOnlyBanner` |
| グループを空にする… | Clear group… | 確認「グループ {0} の定型文 {1} 件を削除します。よろしいですか？」= "Delete {1} snippet from group {0}?" / "Delete {1} snippets from group {0}?" |
| {0} 文字 | {0} character / {0} characters | 編集欄の文字数 |

### 2.4 盤面・トースト

| 日本語 | English（推奨） | キー |
|---|---|---|
| このグループには定型文がありません | This group has no snippets | `Board_EmptyGroup` |
| 定型文を追加… | Add snippets… | `Board_EmptyGroup_Add` |
| クリップボードにコピーしました。 | Copied to the clipboard. | `Toast_Copied` |
| 管理者として動いているアプリには貼り付けられないため、コピーだけしました。 | Can't paste into an app running as administrator, so the snippet was only copied. | `Toast_CopiedOnly_Elevated`（Win） |
| パスワードの入力中は貼り付けられないため、コピーだけしました。 | Can't paste while secure input is on, so the snippet was only copied. | `Toast_CopiedOnly_SecureInput`（Mac） |
| アクセシビリティの許可が無いため、コピーだけしました。 | Accessibility permission is off, so the snippet was only copied. | `Toast_CopiedOnly_NoPermission`（Mac） |
| 修飾キーが押されたままのため、コピーだけしました。 | A modifier key was still held down, so the snippet was only copied. | `Toast_CopiedOnly_ModifierHeld` |
| 元のアプリに戻れなかったため、コピーだけしました。 | Couldn't switch back to the app, so the snippet was only copied. | `Toast_CopiedOnly_Focus` |
| クリップボードが変更されたため、貼り付けを中止しました。 | The clipboard changed, so pasting was cancelled. | `Toast_ClipboardChanged` |
| 貼り付け可能か確認できなかったため、コピーだけしました。 | Couldn’t check whether pasting is available, so the snippet was only copied. | `Toast_CopiedOnly_CheckFailed` |
| 貼り付けのキーを送れなかったため、コピーだけしました。 | Couldn’t send the paste shortcut, so the snippet was only copied. | `Toast_CopiedOnly_SendFailed` |
| クリップボードを使えませんでした。もう一度お試しください。 | The clipboard is busy. Try again. | `Toast_ClipboardBusy`（エラー） |

## 3. OS で表記が変わるもの

- キーの名前・記号は翻訳しない（`Ctrl` / `Alt` / `Shift` / `Win` と `⌃` / `⌥` / `⇧` / `⌘`）。`Loc.Shortcut` が OS の流儀で組む。
- OS で呼び名が違う語だけ `_Win` / `_Mac` の 2 キー（終了 / CPClipboard を終了、トレイアイコン / メニューバーアイコン）。
- その OS に無い機能の文言はキーごと作らない（SPEC §1.2）。

## 4. リソースキーの命名（`Strings.resx`）

- `領域_要素[_部位]`。領域: `Common` / `Tray` / `Menu` / `Settings` / `Effect` / `Enum` / `Toast` / `Dialog` / `Notice` / `Permission` / `About` / `Board` / `Templates` / `Load` / `Archive` / `Guide`。
- 設定画面は `Settings_<タブ>_<項目>`（タブ: `General` / `Appearance` / `Effects` / `Triggers` / `Paste` / `Data` / `Advanced`）、注記は `_Note`、選択肢は `_<値>`。
- enum の表示名は `Enum_<型名>_<値>`（macOS で名前が変わる値だけ `_Mac`）。演出名は `Effect_<演出 ID>`。
- 初回の例文は `Templates_Sample_1..3`（SPEC §9.3）。
- ログ・例外の開発者向けメッセージはリソースにしない（日本語のまま）。

## 5. 製品名と HUD の装飾語

- 製品名 **CPClipboard**、副題 **CyberPunk Clipboard** は両言語とも英字のまま文言に直接書く。
- HUD の装飾語は翻訳しない: ステータス行の `GRP` / `ITEMS`、プレビューの `LEN` / `LINES`（Core の固定文字列）、ダイアログのタグ `CFG` / `AUTH` / `SYS` / `CONFIRM` / `INFO`、トーストの状態コード `>` / `!`。
- 時刻 `HH:mm` / `HH:mm:ss`（24 時間）、日付 `yyyy/MM/dd`（`InvariantCulture`）で固定。

## 編集ウィンドウと共通呼称

| 日本語 | English | 用途 |
|---|---|---|
| 編集… | Edit… | トレイ・macOSアプリメニュー・盤面の編集導線。Launcherと統一 |
| 編集 | Edit | 独立編集ウィンドウのタイトル |
| トリガー | Triggers | 設定タブ名。JSONキーや既存Settings_Triggers_*は変更しない |

Launcherと同じ機能は同じ呼称を使う。Clipboard固有のグループ・定型文・貼り付けをLauncherのページ・アイテム・起動へ言い換えない。トリガーはタブ/機能名で、文章中の「呼び出す」まで機械的に置換しない。

## 盤面の自動ショートカット

- a〜zは翻訳しない。現在グループの先頭26件へ自動で割り当てるキー表示であり、定型文本文に含めない。
- 設定や編集の入力欄では文字入力を優先する。既存の「貼り付け」「コピーだけ」の文言を再利用し、新しい手動割り当て項目を作らない。
