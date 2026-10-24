# CPFileLauncher 設定項目一覧

`settings.json`（SPEC §9）に保存される設定項目の一覧。**設定項目の正はこのファイル**で、SPEC §7 には方針だけを置く。
項目を足す・消す・既定値や範囲を変えるときは、先にこの表を直してから `src/FileLauncher.Core/Model/AppSettings.cs` と設定画面を合わせる。

## 読み方

- **JSON キー**: `settings.json` 内のパス（camelCase。enum は文字列、フラグ enum は `"ctrl, alt"` のようにカンマ区切り）。
- **既定値**: ファイルにキーが無いときの値。範囲外の値は読み込み時に範囲へ丸める（`AppSettings.Normalize`）。未知のキーは無視され、次の保存で消える。
- **OS**: 共通 / Win のみ / Mac のみ。片方の OS で使えない項目は設定画面に**出さない**（無効表示 + 注記にはしない。SPEC §1.2 の原則）。注記を付けるのは「使えるが制約がある」項目だけ。
- 単位: 座標・サイズは Windows では物理 px、macOS では pt（SPEC §3.5）。

## ファイル全体

| JSON キー | 型 | 既定値 | 備考 |
|---|---|---|---|
| `schemaVersion` | int | **3**（board.json は 1 のまま） | 現行より新しい値のファイルは読み取り専用（保存しない）。無ければ 1 とみなす。1 → 2: 旧 `popup.position` / `x` / `y` を `popup.keyboard` と `popup.mouse` の両方へ複写して旧キーを削除。**2 → 3**（`SettingsMigrationV3Tests`）: `appearance.theme` を削除、`appearance.accent` をそのプリセットの 2 色の `appearance.colors` に置き換えて削除（不明な値・無ければシアン）。`appearance.effects` は触らない（SPEC §9.3 / §3.6「配色」の移行） |

## 一般タブ（`general`）

| 項目名（UI） | JSON キー | 型 | 既定値 | 範囲・選択肢 | OS | 備考 |
|---|---|---|---|---|---|---|
| OS ログイン時に自動起動 | `general.autoStart` | bool | false | | 共通 | 値は OS 側の登録状態と二重になるので、起動時と設定画面を開くたびに OS 側（`IAutoStartService.IsEnabled`）を正として読み直す。登録する実行ファイルは `Environment.ProcessPath`。Mac で `.app` 以外から起動中（`NSBundle.mainBundle.bundleIdentifier` が無い）は行ごと非表示（無効表示 + 注記にはしない。SPEC §1.2） |
| 言語 | `general.language` | string | `system` | `system` / `ja` / `en`（それ以外の値は読み込み時に `system` へ丸める） | 共通 | UI 表示: システム / 日本語 / English（言語名は各言語の自称で、UI 言語に関わらず固定）。**変更は再起動後に反映**: 注記「再起動後に反映されます。」に加え、今の表示言語と選択が違う間だけ「今すぐ再起動」ボタンを行の右に出す（押すと `hub.Flush` → 既存の再起動処理）。`system` は OS の UI 言語（.NET の `CultureInfo.CurrentUICulture` 初期値）が日本語なら日本語、それ以外は英語（英語が既定リソース）。`Program` が Avalonia 起動前に `SettingsPeek` でこのキーだけ先読みして `CurrentUICulture` / `DefaultThreadCurrentUICulture` を設定する（`CurrentCulture` = 数・日付の書式は変えない）。開発・確認用の起動引数 `--lang ja|en` は設定より優先され、保存されない。訳語は `spec/TERMS.md`、方針は SPEC §7、macOS `.app` 起動時の判定は  |
| フォルダを開く先 | `general.folderOpenTarget` | enum | `existingTab` | `existingTab` / `newWindow` / `system`（**Mac の UI は `existingTab` / `newWindow` の 2 択**。`system` は Mac では `newWindow` と同じ動作で、保存されていれば「新しいウィンドウ」として表示（選び直すまで値は書き換えない）） | **共通** | SPEC §5.2。UI 表示: 開いている Explorer の新しいタブ / 新しいウィンドウ / Windows に任せる、: **開いている Finder ウィンドウの新しいタブ / 新しいウィンドウ**（`ComboBox`。`EnumNames.Of<FolderOpenTarget>`。Mac の 1 つ目は `Enum_FolderOpenTarget_ExistingTab_Mac` キー。「言語」の下・「移動時に確認」の上に置く）。注記（Win、`Settings_General_FolderOpenTarget_Note_Win`）「Ctrl+クリック（Ctrl+Enter）は、この設定に関係なく新しいウィンドウで開きます。タブの追加は Explorer の非公開の仕組みを使うため、Windows の更新で動かなくなったら「Windows に任せる」を選んでください。」、注記（Mac、`_Note_Mac`）「⌘クリック（⌘Enter）は、この設定に関係なく新しいウィンドウで開きます。タブを足すには Finder の制御（システム設定 → プライバシーとセキュリティ → オートメーション）の許可が必要で、初めて使うときに macOS が確認します。許可しない場合は新しいウィンドウで開きます。」（`spec/TERMS.md`）。効く経路（両 OS）: フォルダアイテムの起動（クリック / Enter / ショートカットキー / メニュー「起動」）・「格納フォルダを開く / Finder で表示」（ファイルを選択した状態）・「設定フォルダを開く」「ログフォルダを開く」・「について」のデータフォルダ。**Windows**: `existingTab` は手前の Explorer（`CabinetWClass`）に `WM_COMMAND 0xA21B` でタブを足し、失敗・タイムアウト・タブ非対応の Windows では新しいウィンドウにフォールバック（ログのみ）。`newWindow` は `ShellExecuteEx` の verb `opennewwindow`。`system` は従来の `ShellExecute` / `explorer.exe /select,`（OS 設定に従う。Explorer 以外のファイラを既定にしている環境向け）。**macOS**: `existingTab` は Finder の手前のウィンドウに「新規タブ」（AX でメニューを押す）+ Apple Events（`set target` / `select` / `activate`。自動化の許可が要る）。Finder のウィンドウが無い・許可なし・失敗は従来の `open`（新しいウィンドウ）にフォールバック（ログのみ）。`newWindow` / `system` は `open` / `open -R`（SPEC §5.2「macOS: Finder のタブ」）。変更通知は `None`（使うたびに `hub.Current` を読む）。キーが無ければ `existingTab`（省略可能な追加。`schemaVersion` は上げない）。不正な enum 値は他の enum と同じく読み込み失敗（SPEC §9.3） |
| 移動時に確認（フォルダへの Shift+ドロップ） | `editing.confirmMoveOnDrop` | bool | true | | 共通 | SPEC §5.3。同名競合の規則（連番の別名・同一内容はスキップ）は設定項目を持たず固定（SPEC §5.3） |
| ホイールでページを切り替える | `board.wheelSwitchesPage` | bool | true | | 共通 | SPEC §6.5。下 = 次のページ。ヘッダー（タブ）の上でも同じ（`Frame` でトンネル）。OFF ならホイールは何もしない。アイテムをドラッグ中も効かない。マウストリガー「ホイールクリック + 回転」（トリガータブ）はこの設定と無関係に効く |
| Alt+1〜9 でページを切り替える（Mac: ⌥1〜9 でページを切り替える） | `board.altNumberSwitchesPage` | bool | true | | 共通 | SPEC §6.5。Alt+1〜9（Mac は ⌥1〜9）で 1〜9 ページ目へ。数字は物理キー（`PhysicalKey.Digit1..9`）で判定（Mac の ⌥ は文字が変わるため）。Ctrl+Tab は設定項目を持たず常に有効 |
| 権限の設定…（ボタン） | — | — | — | | Mac のみ | 権限ガイドを開く（SPEC §4.3）。値は持たない。行名「アクセシビリティ」、注記「ホットキーとマウス操作に必要です。」はそのまま（自動化は任意の権限なので一般タブには行を足さず、「フォルダを開く先」の注記と権限ガイドで触れる） |

## 表示タブ（`appearance`）

| 項目名（UI） | JSON キー | 型 | 既定値 | 範囲・選択肢 | OS | 備考 |
|---|---|---|---|---|---|---|
| 配色 | （`appearance.colors` の表示専用） | — | — | シアン / レッド / ブルー / グリーン / パープル / カスタム | 共通 | `ComboBox`。各項目に主色・副色の色見本 12×12 を 2 つ + 名前。プリセットを選ぶと下の 2 色を書き換える。2 色がプリセットと完全一致すればそのプリセット、違えば「カスタム」を表示（「カスタム」は選べない）。値は保存しない。SPEC §3.6「配色」 |
| 　主色 | `appearance.colors.primary` | string（`#RRGGBB`） | `#00E5FF` | 任意の色。読み込み時: 大文字小文字を問わない、8 桁ならアルファを捨てる、不正ならシアンの値に戻してログ 1 行、表記は大文字の `#RRGGBB` にそろえる。**色の値そのものは変えない**（自動補正しない。暗い色を選んだときの見え方はユーザーの責任、SPEC §3.6「読みやすさ」） | 共通 | UI: `ColorPicker`+ 右に hex の表示（等幅）。枠・発光・タブ・ホバー・角ブラケット（左上右下）・グリッチの影・光の玉・設定画面のアクセント・**背景と文字の色相**がすべて追従（導出規則は SPEC §3.6）。即時反映（辞書の差し替え。ドラッグ中は 1 フレームに 1 回に間引く）。変更時の通知は `Colors`（`Render` 不要） |
| 　副色 | `appearance.colors.secondary` | string（`#RRGGBB`） | `#FF2BD6` | 同上 | 共通 | UI: 同上 + 行の右端に「⇄」（主色と副色を入れ替える）。ドロップ先・角ブラケット（右上左下）・グリッチの影の片方・`orbTwin` の 2 個目に使う。背景・文字は副色からは導かない |
| 不透明度 | `appearance.opacity` | int (%) | **80** | **30〜100**、5 刻み（スライダー。範囲外は丸め、5 の倍数でない値は最も近い 5 の倍数に） | 共通 | 既定は 80。既存の settings.json に `opacity` が書かれていればその値を尊重（既定はキーが無いときだけ）。盤面全体（背景・アイコン・ラベル）にかかる。両モード有効。即時反映（SPEC §3.1）。「ホバー中は不透明」は §12 |
| 背景画像 | `appearance.background.image` | string / null | null（なし） | データフォルダからの相対パス `background/<ファイル名>`（設定画面から選んだとき）または絶対パス（手編集。`~` / 環境変数を展開）。対応形式 png / jpg / jpeg / bmp / gif / webp | 共通 | SPEC §3.7。UI: プレビュー（120×68）+ ファイル名 + 「選択…」「クリア」。「選択…」はファイルを `background/` へコピーして相対パスを保存、「クリア」は null にしてコピーを削除。**プレビュー（サムネイル 120×68）は Explorer / Finder からの画像ファイルのドロップ先でもある**（SPEC §3.7「ドロップで設定」）: 落とすと「選択…」と同じ処理。対応形式の最初の 1 ファイルだけ、フォルダ / 非対応 / テキストだけならトースト。ドラッグ中はサムネイルの枠が `FlDropBorder` / `FlDropBackground`、画像なしなら「ここにドロップ」。行の下に注記「サムネイルに画像ファイルをドロップしても設定できます。」。盤面の面（`Frame` の内側）だけ・全ページ共通。見つからない / 読めないときは面の色で表示し、値は消さずこの行に `FlError` の注記（トーストは出さない）。長辺 2048 px に縮小してバックグラウンドでデコード（表示遅延に影響しない）。**表示する大きさ（面 × 拡大率）に一度だけ縮小した画像を作り置きし、毎フレームは等倍で貼る**（SPEC §3.7「作り置き」。常時の演出と重なったときの CPU 対策。見た目・設定値は変わらない）。変更時の通知は `Background`（`Render` 不要）。既存の settings.json にキーが無ければ「なし」 |
| 　表示方法 | `appearance.background.fit` | enum | `fill` | `fill` / `fit` / `stretch` / `tile` / `center` | 共通 | UI 表示: 埋める / 収める / 引き伸ばす / 並べる / 中央（`ComboBox`）。`ImageBrush` の対応は SPEC §3.7。画像があるときは面の地の色を描かないので、「収める」「中央」で画像が届かない余白は覆いの色だけになる（透ける）。画像が無いときは無効表示 |
| 　画像の覆い | `appearance.background.overlay` | int (%) | 40 | 0〜90、5 刻み（スライダー。範囲外は丸め） | 共通 | 画像の上に盤面の面の色（`FlBoardBackground`）をこの不透明度で重ねる（テーマ・基調色に追従）。画像があるときは面の地の色を描かないので、**盤面の暗さはこの覆いだけで決まる**（SPEC §3.7 描画の層）。注記「盤面の色を画像の上に重ねます。大きいほど画像が薄くなり、アイコンと文字が読みやすくなります。」。画像が無いときは無効表示 |
| 　画像の不透明度 | `appearance.background.imageOpacity` | int (%) | 100 | 10〜100、5 刻み（スライダー。範囲外は丸め） | 共通 | 画像の層（`Backdrop`）の `Opacity`。画像があるときは面の地の色を描かないため、下げると**画像の部分からデスクトップが透ける**（覆いの色は残る）。100 なら埋める / 引き伸ばす / 並べるの見た目は従来どおり。注記「下げると画像の部分からデスクトップが透けます。」。「画像の覆い」の直下に置く。画像が無いときは無効表示。ウィンドウ全体の `appearance.opacity` とは独立に掛かる（両方が効く）。変更時の通知は `Background`（`Render` 不要）。`schemaVersion` は 2 のまま（省略可能な項目の追加。キーが無ければ 100） |
| ボタンサイズ | `appearance.buttonSize` | int (px) | 48 | 32 / 48 / 64 / 96（小 / 中 / 大 / 特大） | 共通 | ページ個別の `buttonSize`（board.json）が null のときに使う。変更時はアイコンをそのサイズで取り直す（SPEC §5.4） |
| ステータス行 | `appearance.hud.statusBar` | bool | true | | 共通 | SPEC §3.9 H1。盤面下部に `PAGE 02/05`（左）/ `ITEMS 24`（中央）/ 日時（右端）の 1 行。ON のときウィンドウが **21 px** 高い。ToggleSwitch。注記「盤面の下にページ・アイテム数・時刻を出します。」。変更時の通知は `Hud`（`Render` あり: 高さが変わる）。省略可能な追加なので `schemaVersion` は上げない |
| 　時刻 | `appearance.hud.clock` | enum | `minutes` | `off` / `minutes` / `seconds` | 共通 | UI 表示: 表示しない / 時:分 / 時:分:秒（`ComboBox`）。24 時間表記で固定。`minutes` は分の変わり目だけ、`seconds` は 1 秒ごとに描き直す。盤面が見えている間だけ動く。ステータス行 OFF のときは無効表示（状態による無効化。SPEC §3.7 の表示方法と同じ扱い）。変更時の通知は `Hud`（`Render` 不要。文字列の組み直しとタイマーの付け替えだけ） |
| 　日付（年/月/日） | `appearance.hud.date` | bool | true | | 共通 | SPEC §3.9 H1。時刻の前に `yyyy/MM/dd` を付ける（`2026/10/04 15:05`。書式は固定、OS の日付書式には従わない）。時刻が「表示しない」のときは日付だけ `2026/10/04` を出し、タイマーは日付の変わり目に合わせる。時刻・日付とも OFF なら右端の区画は出ない。ToggleSwitch（「時刻」の下、注記なし）。ステータス行 OFF のときは無効表示。変更時の通知は `Hud`（`Render` 不要）。キーが無ければ ON（省略可能な追加。`schemaVersion` は上げない） |
| 背景グリッド | `appearance.hud.grid` | bool | true | | 共通 | SPEC §3.9 H6「背景グリッドの仕様」。盤面の面（背景画像の覆いの上・タブとアイコンの下）に 24 px 間隔の 1 px の線（色は主色 `FlHudGrid`、濃さは下の「グリッドの濃さ」）。ToggleSwitch。注記「スロットの下に主色の細い線を敷きます。」。変更時の通知は `Hud`（`Render` の中で層の `IsVisible` を付け直す。ウィンドウの大きさは変わらない）。省略可能な追加なので `schemaVersion` は上げない |
| 　グリッドの濃さ | `appearance.hud.gridOpacity` | int (%) | **3** | 0〜10、1 刻み（スライダー。範囲外は読み込み時に 0〜10 へ丸める） | 共通 | 背景グリッドの線の不透明度。`GridLayer.Opacity = gridOpacity / 100`（トークン `FlHudGrid` はアルファなしの主色。配色を変えても濃さは保たれる）。**0 なら層ごと非表示**。`ValueSlider`（`%` 表示、注記なし）。「背景グリッド」の直下に置き、背景グリッド OFF の間は無効表示（状態による無効化。ステータス行 OFF で「時刻」「日付」が無効になるのと同じ扱い）。変更時の通知は `Hud`（`Render` の中で `Opacity` を付け直す）。キーが無ければ 3（省略可能な追加。`schemaVersion` は上げない） |
| ラベル表示 | `appearance.label` | enum | `below` | `none` / `below` / `hover` | 共通 | `hover` = セルの高さは `none` と同じ。マウスを乗せたスロットだけ、アイコン下部に名前を半透明の帯で重ねて出す（演出は EFFECTS.md `labelHover`）。盤面下部の 1 行にまとめて出す案もあり、動くものを見てから決める |
| 既定の行数 | `appearance.defaultRows` | int | 4 | 1〜12 | 共通 | SPEC §3.1 / §6.5。メニュー「新規ページ」（`PageEditing.Add`）がこの値で作る。既存ページには影響しない（注記「新しいページを作るときの初期値です。」）。変更通知は `None`（描き直し不要） |
| 既定の列数 | `appearance.defaultCols` | int | 6 | 1〜12 | 共通 | |
| 画像はサムネイルで表示 | `appearance.imageThumbnails` | bool | true | | 共通 | SPEC §5.4。OFF にすると種類アイコン |

## 演出タブ（`appearance.animation` / `appearance.effects` / `appearance.glitchIntensity` / `appearance.vsync` と、静止した走査線 `appearance.hud.scanlines` / `scanlineOpacity` / `scanlinePitch`）

| 項目名（UI） | JSON キー | 型 | 既定値 | 範囲・選択肢 | OS | 備考 |
|---|---|---|---|---|---|---|
| アニメーション | `appearance.animation` | bool | true | | 共通 | **全演出の ON/OFF**。演出の一覧・既定値（種類 / 時間 / イージング）は **`spec/EFFECTS.md` が正**。OFF なら全演出が「なし」（時間 0）で常時の演出も止まる。注記「OFF にすると下の演出がすべて止まります。」。タブの先頭 |
| 光の玉（小見出し「常時の演出」の下） | `appearance.effects.frameOrb` | object | **`orb / 8000`**（既定 ON） | 種類 なし / 1 個 / 2 個、時間 2000〜20000 ms（500 刻み、秒で表示） | 共通 | EFFECTS.md `frameOrb`。**折りたたまずに普通の行**（項目名は演出名だけ、ID は付けない）。コントロール = 種類 `ComboBox` + 時間スライダー + 「既定に戻す」（この ID を辞書から消す。既定のときは無効）。**イージング欄は出さない**（常時の演出では使わない。詳細設定の行では従来どおり無効表示）。値は `appearance.effects` の中なので新しいキーは増えない |
| 発光の明滅 | `appearance.effects.glowPulse` | object | **`pulse / 4000`**（既定 ON） | 種類 なし / 弱 / 強、時間 1000〜10000 ms（250 刻み） | 共通 | EFFECTS.md `glowPulse`。行の形は光の玉と同じ |
| 走査線の帯 | `appearance.effects.scanBeam` | object | `beam / 4000`（既定 ON、 午前から） | 種類 なし / 帯、時間 2000〜12000 ms（500 刻み） | 共通 | EFFECTS.md `scanBeam`。行の形は光の玉と同じ。静止した走査線とは別の要素だが、ユーザーには「走査線」の 2 面なので同じタブに置く |
| 詳細設定（折りたたみ。旧「演出の調整…」。常時の演出 3 つ以外の演出） | `appearance.effects` | object | `{}`（空 = すべて既定） | キー = EFFECTS.md の演出 ID、値 = `{ "kind": none / fade / zoom / slide / highlight / glitch / glow / orb / orbTwin / pulse / pulseStrong / beam, "durationMs": 0〜1000（常時の演出 `frameOrb` は 2000〜20000、`glowPulse` は 1000〜10000、`scanBeam` は 2000〜12000。範囲は `EffectDefinition`）, "easing": linear / easeOut / easeIn / easeInOut / backOut }`。演出ごとに選べる `kind` は EFFECTS.md の「選べる種類」列 | 共通 | **既定値と異なる演出だけ保存する**（既定値はコード内 `EffectCatalog` で、EFFECTS.md と一致させる）。**既定値は 1 組**。未知の ID・その演出で選べない `kind` は読み込み時に捨てて既定に戻す。UI は演出タブの VSync・注記の下に折りたたみ「詳細設定」（`Settings_Effects_Advanced`。日「詳細設定」/ 英 "Advanced effects"）で、**常時の演出 3 つを除く** 11 演出が 1 件につき 1 行（`EffectCatalog.All.Where(d => !d.IsAmbient)` の順）: 項目名 = 演出名 + ID（書式 `Settings_Effects_Row`「{0}（{1}）」、旧 `Settings_Appearance_EffectRow`）/ 種類ドロップダウン / 時間スライダー（0〜1000、10 刻み）/ イージングドロップダウン（`glitch` ではイージングを使わないので無効表示）/ 「既定に戻す」（= その ID を辞書から削除）。手編集の例: `"effects": { "boardShow": { "kind": "zoom", "durationMs": 100, "easing": "easeOut" } }` |
| 　グリッチの強さ（折りたたみ「詳細設定」の中、`boardHide` の行の直下） | `appearance.glitchIntensity` | int (%) | **100** | **0〜200、10 刻み**（スライダー。範囲外は読み込み時に 0〜200 へ丸め、10 の倍数でなければ最も近い 10 の倍数に = `Math.Round(Math.Clamp(v, 0, 200) / 10.0, MidpointRounding.AwayFromZero) * 10`。不透明度と同じ形。非整数は他の int と同じく読み込み失敗） | 共通 | 設定は表示・非表示で共通。強さの計算は Core `GlitchLook`、詳細は EFFECTS.md「強さの倍率」。設定変更は即時反映し、`GlitchIntensity` の変更通知を出す。 |
| VSync | `appearance.vsync` | int | **2** | **1 / 2 / 3**（`ComboBox`。1 = 60 fps / 2 = 30 fps / 3 = 20 fps。範囲外（0、4、−1 …）は読み込み時に 1〜3 へ丸める = `Math.Clamp`。非整数は他の int と同じく読み込み失敗） | 共通 | **常時の演出（光の玉・発光の明滅・走査線の帯）の更新間隔**（`spec/EFFECTS.md`「常時の演出の詳細」の「時計」）。ユーザーの言葉「1=60fps で、1-3 にしましょう」。意味は「60 Hz のフレーム何枚ぶんに 1 回更新するか」で、**時間で間引く**（`MinFrameMs = {1: 13, 2: 30, 3: 47}` = `N × 16.67 ms` − 約 3 ms のマージン。Core `AmbientMath.FrameIntervalMs(vsync)`）ので 120 Hz のモニタでも 1 = 60 fps / 2 = 30 fps / 3 = 20 fps（144 Hz では 72 / 29 / 21 fps の近似）。既定 2 は  午前に定数 30 ms で実装・計測した値と同じ（全部オン 30%、玉だけ 24%）。UI: 演出タブで常時の演出 3 行（光の玉 / 発光の明滅 / 走査線の帯）の直後・注記 `Settings_Effects_AmbientNote` の直前に 1 行（折りたたみの外）。ラベル "VSync" は**両言語とも同じ・翻訳しない**（`Settings_Effects_Vsync`。TERMS.md §5 の HUD 装飾語と同じ扱い）。`ComboBox` の項目は書式 `Settings_Effects_Vsync_Item`（日「{0}（{1} fps）」/ 英「{0} ({1} fps)」。`{0}` = 1〜3、`{1}` = 60 / 30 / 20）。注記 `Settings_Effects_Vsync_Note`「光の玉・発光の明滅・走査線の帯を描き直す間隔です。数字が大きいほど CPU を使いませんが、動きが粗くなります。」。「アニメーション」OFF で無効表示。**タブ末尾の「すべて既定に戻す」は `effects` を空にするのに加えてこの値も 2 に戻す**。変更時の通知は `Effects`（`BoardWindow.ApplyEffects` → `AmbientAnimator.Vsync`。次のフレームから効く。`Render` 不要）。キーが無ければ 2（省略可能な追加。`schemaVersion` は 3 のまま）。常時の演出がすべて「なし」のときは効果が無いが行は出したまま（状態による無効化はしない。選ぶ前に分かるように）。**macOS: 常時の演出を OS の Core Animation が描くので VSync は使わない**（`AmbientAnimator` を始めない。値は読まない。JSON キーは残す = Windows 用・エクスポート / インポートの互換）。設定画面では `Platform.Ambient.IsSupported` が true のとき **この行（ComboBox + 注記）を出さない**。「すべて既定に戻す」が `vsync` を 2 に戻す動作は OS によらず同じ。**macOS の合成の頻度は CA の `preferredFrameRateRange` で 30 fps 固定** |
| （注記）常時の演出の CPU / 視差効果 | — | — | — | | CPU の注記は Windows だけ、視差効果は共通 | VSync の直下に `Settings_Effects_AmbientNote` **「光の玉・発光の明滅・走査線の帯は、盤面が見えている間ずっと動き、CPU を多く使います。」**を常に出し、OS の「視差効果を減らす」がオンなら `Settings_Effects_ReducedMotionNote` も（TERMS.md）。「アニメーション」OFF でも読める。**macOS（`Platform.Ambient.IsSupported` が true）では CPU の注記の項目自体を出さない**（常時の演出を OS の Core Animation が描き CPU を使わないため。macOS 用の別文言 `_Mac` は作らない）。視差効果の注記は両 OS で従来どおり |
| すべて既定に戻す（ボタン） | — | — | — | | 共通 | 演出の並びの末尾、折りたたみ「詳細設定」の外。`appearance.effects` を空にし（常時の演出 3 つも含めて既定 = 3 つオンに戻る）、`appearance.vsync` を 2 に、**`appearance.glitchIntensity` を 100 に**。「アニメーション」と**静止した走査線（`appearance.hud.*`）は触らない**。通知は `Effects` 1 回。データタブの「設定をリセット」とは別（あちらは全項目） |
| 走査線（小見出し「静止した走査線」の下） | `appearance.hud.scanlines` | bool | **true** | | 共通 | SPEC §3.9 H14「走査線の仕様」。盤面の面に主色の 1 px の横線を「走査線の間隔」おきに敷く。層は背景グリッドの直後・スロットの下（面・背景画像・覆い・グリッドの上、アイコン・文字の下。八角形で切れる）。ToggleSwitch、注記「スロットの下に主色の横線を敷きます。動かないので CPU を使わず、「アニメーション」がオフでも消えません。」（後半の 1 文は演出タブへの移動で追加）。**「すべて既定に戻す」の直下に小見出し「静止した走査線」（`Settings_Effects_ScanlinesHeading`）を置き、その下にこの行**。変更時の通知は `Hud`（`Render` の中で層の `IsVisible` を付け直す。ウィンドウの大きさは変わらない）。静的で CPU は増えない。**`appearance.animation` とは無関係（OFF でも消えず、設定画面の行も無効にしない）。「すべて既定に戻す」の対象外**。**既定 ON・25% は で確定**。キーが無ければ ON（省略可能な追加。`schemaVersion` は上げない）。リソースキー `Settings_Effects_Scanlines` / `_Note`（旧 `Settings_Appearance_Hud_Scanlines*`） |
| 　走査線の濃さ | `appearance.hud.scanlineOpacity` | int (%) | **25** | 0〜50、5 刻み（スライダー。範囲外は読み込み時に 0〜50 へ丸め、5 の倍数でなければ最も近い 5 の倍数に） | 共通 | 走査線の不透明度。`ScanlineLayer.Opacity = scanlineOpacity / 100`（トークン `FlHudScanline` はアルファなしの主色。配色を変えても濃さは保たれる）。**0 なら層ごと非表示**。`ValueSlider`（`%` 表示、注記なし）。「走査線」の直下に置き、走査線 OFF の間は無効表示（背景グリッド OFF でグリッドの濃さが無効になるのと同じ扱い）。値の根拠（Mac、Retina）: 4% / 2 px と 12% / 2 px は「見えない」、**25% / 3 px** をユーザーが選定。グリッド（0〜10、1 刻み）より範囲が広いのは、横線だけで面積が小さく濃くないと見えないため。変更時の通知は `Hud`（`Render` の中で `Opacity` を付け直す）。キーが無ければ 25（省略可能な追加。`schemaVersion` は上げない）。リソースキー `Settings_Effects_ScanlineOpacity` |
| 　走査線の間隔 | `appearance.hud.scanlinePitch` | int (px) | **3** | 2 / 3 / 4（`ComboBox`。それ以外の値は読み込み時に 3 に戻す = `ButtonSize` と同じ扱い。`HudSettings.ScanlinePitches = [2, 3, 4]`） | 共通 | 横線の間隔（n px ごとに 1 本）。`ScanlineLayer.Pitch`（`StyledProperty`、`AffectsRender`）に入れる。UI 表示は「2 px / 3 px / 4 px」（書式リソース `Common_PixelValue` = `{0} px`、両言語同じ。数値は翻訳しない）。「走査線の濃さ」の直下に置き、走査線 OFF の間は無効表示。2 px は細かく濃さを上げると CRT らしい、4 px は粗く目立ちにくい。変更時の通知は `Hud`（`Render` の中で `Pitch` を付け直す = 層の描き直しだけ）。キーが無ければ 3（省略可能な追加。`schemaVersion` は上げない）。リソースキー `Settings_Effects_ScanlinePitch` |

## ポップアップタブ（`popup`）

| 項目名（UI） | JSON キー | 型 | 既定値 | 範囲・選択肢 | OS | 備考 |
|---|---|---|---|---|---|---|
| 表示位置（キーボードで開いたとき） | `popup.keyboard.position` | enum | `cursor` | `cursor` / `lastPosition` / `screenCenter` / `fixed` | 共通 | ホットキーで開いたときに使う。UI 表示: カーソル位置 / 前回の位置 / 画面中央 / 固定座標。「カーソル位置」はキーボードでも**マウスカーソル**の位置（キャレット位置は SPEC §12）。カーソル位置は画面内に収まるよう補正（SPEC §3.3） |
| 　座標 X / Y（キーボード） | `popup.keyboard.x`, `popup.keyboard.y` | int? | null | 画面内 | 共通 | `fixed` のときの座標。`lastPosition` のときは閉じるたびに前回位置として上書き保存される欄（**`lastPosition` を選んでいる組だけ**に書く。`fixed` の組の座標は閉じても変わらない）。単位は OS で異なる（Win: 物理 px、Mac: pt）。UI は `fixed` のときだけ有効 + 「今の盤面の位置を使う」ボタン |
| 表示位置（マウス操作で開いたとき） | `popup.mouse.position` | enum | `cursor` | 同上 | 共通 | マウストリガーで開いたときに使う。**トレイ／メニューバーアイコンのクリック・メニュー（盤面を表示 / ページ一覧）・2 重起動からの表示もこちら**（SPEC §3.3）。UI は 2 組を同じ見た目で縦に並べ、下に注記「トレイアイコンやメニューから開いたときはマウス操作の設定を使います」 |
| 　座標 X / Y（マウス） | `popup.mouse.x`, `popup.mouse.y` | int? | null | 画面内 | 共通 | キーボード側と同じ規則 |
| 盤面上のカーソルの位置 | `popup.cursorAnchor` | enum | `top` | `topLeft` / `top` / `topRight` / `left` / `center` / `right` / `bottomLeft` / `bottom` / `bottomRight` | 共通 | SPEC §3.3「カーソルのアンカー」。**キーボード・マウス共通の 1 項目**（旧 `popup.keyboard.cursorAnchor` / `popup.mouse.cursorAnchor` は未リリースのためマイグレーションなし、残っていても読み飛ばす）。盤面（ウィンドウ）のどの点がカーソルに来るか。辺・角はウィンドウ外形から `20 × 拡大率` 内側（= 見える枠の 20 外側）、`center` はちょうど中心。`top` が従来の挙動。どちらの組で開いても、その組の表示位置が `cursor` のとき（および `lastPosition` / `fixed` で座標が未保存のため退避したとき）に使う。UI はキーボード・マウスの 2 組の下に独立した 1 行（字下げなし）で 3 × 3 のトグルボタン（`AnchorPicker`。選択中は主色の枠 + 薄い面 + 主色の点、各ボタンのツールチップとアクセシブル名 = 位置の名前 `Enum_CursorAnchor_*`）。**キーボード・マウスの表示位置のどちらかが `cursor` なら有効、どちらも `cursor` 以外なら無効表示**（非表示にはしない）。注記 `Settings_Popup_CursorAnchor_Note`（`spec/TERMS.md`）。変更通知は `Popup`（次に開いたときから効く）。キーが無ければ `top`（`schemaVersion` は 3 のまま）。不正な enum 値は他の enum と同じく読み込み失敗（SPEC §9.3） |
| アイテムを起動したら閉じる | `popup.closeOnLaunch` | bool | true | | 共通 | SPEC §3.3 |
| 盤面外クリック／他ウィンドウで閉じる | `popup.closeOnOutsideClick` | bool | true | | 共通 | フック座標で判定（SPEC §3.3） |
| トリガー再実行で閉じる（トグル） | `popup.toggleOnTrigger` | bool | true | | 共通 | |
| マウスが離れたら閉じる | `popup.closeOnMouseLeave` | bool | false | | 共通 | SPEC §3.3。表示中だけ 100ms 間隔の `DispatcherTimer` で `IWindowService.GetCursorPosition` を見て、盤面矩形からの距離（Core `MouseLeaveTracker`。矩形の外側までの最短距離）が閾値を超えたら閉じる（フックの移動イベントは使わない）。**一度カーソルが盤面に入ってから**判定を始める（画面中央・固定座標の表示でカーソルが遠い場合に即閉じしないため）。モーダル中（`RunModalAsync`）は判定しない |
| 離れたとみなす距離 | `popup.mouseLeaveDistance` | int | 200 | 50〜1000（SPEC は「距離設定可」のみ。範囲はここで仮決め） | 共通 | 単位は OS に従う。「マウスが離れたら閉じる」OFF のときスライダーは無効表示 |
| Esc で閉じる | — | — | 常に有効 | | 共通 | 設定項目ではない（SPEC §3.3） |

## トリガータブ（`triggers`）

| 項目名（UI） | JSON キー | 型 | 既定値 | 範囲・選択肢 | OS | 備考 |
|---|---|---|---|---|---|---|
| 修飾キーの2回押し | `triggers.doubleTap.enabled` | bool | **false** | | 共通 | SPEC §4.4。CPClipboardの既定Ctrl×2との競合を避けるため既定OFF。既存の設定値は変えない |
| キー | `triggers.doubleTap.modifier` | enum | `ctrl` | ctrl / alt / shift / meta | 共通 | WinはCtrl/Alt/Shift/Win、Macは⌃/⌥/⇧/⌘。2回押しOFFの間は無効表示 |
| 判定の間隔 | `triggers.doubleTap.intervalMs` | int(ms) | **300** | 200〜500・10刻み | 共通 | 1回目離上→2回目押下。押下時間上限250msは固定。OFFの間は無効表示 |
| ホットキーを使う | `triggers.hotkey.enabled` | bool | true | | 共通 | |
| ホットキーの修飾キー | `triggers.hotkey.modifiers` | flags | `ctrl, alt` | `ctrl` / `alt` / `shift` / `meta` の組合せ（1 つ以上） | 共通 | UI 表示は OS で変える: Win「Ctrl / Alt / Shift / Win」、Mac「⌃ / ⌥ / ⇧ / ⌘」。設定画面ではキー入力で記録: 記録欄をクリックすると「キーを押してください…」になり、Avalonia の `KeyDown`（修飾キー + キー）から記録する。記録中は現在のホットキーの判定を止める（`IHotkeyService.Unregister` → 記録後に `Register`）。修飾キーなし・修飾キーのみは受け付けない（SPEC §4.1）。Esc で中止。Avalonia `Key` → SharpHook のキー名への対応表に無いキーは「このキーは使えません」 |
| ホットキーのキー | `triggers.hotkey.key` | string | `Space` | SharpHook の `KeyCode` 名から `Vc` を除いたもの（`Space`, `A`, `F1` …） | 共通 | 修飾キー単独の2回押しは SPEC §4.4 |
| マウス操作（複数） | `triggers.mouse[]` | 配列 | `[{ gesture: click, button: middle, modifiers: ctrl }]` | 要素: `enabled`(bool, 既定 true) / `gesture`(`click` / `longPress` / `leftRightTogether` / `wheelClickRotate`) / `button`(`left` / `middle` / `x1` / `x2`) / `modifiers`(flags) | 共通 | SPEC §4.2。修飾キーなしの `left` クリックは選べない（UI で弾く）。`x1` / `x2` は Mac で Mission Control 等と競合する注記。`wheelClickRotate` は中ボタンを押しながらの回転でページ切替（盤面が出ていなければ出してから切替）。`button` は `middle` 固定で、UI ではボタンのドロップダウンを**非表示**にする（`leftRightTogether` と同じ扱い。SPEC §1.2 の原則）。中ボタンの押下・離上は下のアプリへ通す（回さずに離せば普通の中クリック） |
| デスクトップ上でのみ有効 | `triggers.desktopOnly` | bool | false | | 共通 | SPEC §4.2 |
| 長押し時間 | `triggers.longPressMs` | int (ms) | 400 | 200〜1000 | 共通 | |
| 左右同時押しの判定時間 | `triggers.simultaneousWindowMs` | int (ms) | 100 | 50〜200 | 共通 | |

## 常駐タブ（`resident`）

| 項目名（UI） | JSON キー | 型 | 既定値 | 範囲・選択肢 | OS | 備考 |
|---|---|---|---|---|---|---|
| 常駐モード | `general.displayMode` | enum（UI は ToggleSwitch） | `popup`（= OFF） | ON = `resident` / OFF = `popup` | 共通 | **常駐タブの先頭の行**。JSON キー・値・既定値は変えない（`general.*` のまま。キーは保存先であって UI の位置ではない）。変更は即時反映で、通知は従来どおり `SettingsChange.DisplayMode`（切替の振る舞いは SPEC §3.4）。トレイ / メニューバーのメニュー「表示モード ▸」からも切り替えられ、**設定画面が開いていればトグルが追従する**（`hub.Changed` の `DisplayMode` でトグルの `IsChecked` を付け直す。付け直しで再度 `hub.Update` を呼ばないこと）。注記 `Settings_Pinned_Enabled_Note`（文言は TERMS.md）。リソースキー `Settings_Pinned_Enabled` / `_Note`（旧 `Settings_General_DisplayMode` / `_Note`、常駐タブ先頭の注記 `Settings_Pinned_CurrentPinned` / `Settings_Pinned_CurrentPopup` は削除） |
| 重ね順 | `resident.zOrder` | enum | `normal` | `topmost` / `normal` / `bottommost` | 共通 | SPEC §3.2。Mac の `bottommost` はデスクトップアイコン層の直上（SPEC §10.3）。`IWindowService.SetZOrder` で即時反映 |
| 自動で隠す | `resident.autoHide` | bool | false | | 共通 | 画面端に収納・スライドイン（SPEC §3.2）。演出は EFFECTS.md `residentAutoHide` |
| 位置をロック | `resident.lockPosition` | bool | false | | 共通 | ON のときドラッグで動かない（ヘッダーの隙間・タブ・空きスロット・枠の余白のいずれからも。SPEC §3.1 / §3.2） |
| トリガーで盤面をカーソル位置へ移動 | `resident.moveToCursorOnTrigger` | bool | false | | 共通 | SPEC §3.4。演出は EFFECTS.md `residentMove` |
| 前回の位置・サイズ | `resident.bounds` | `{x, y, width, height}` / null | null | 画面内 | 共通 | UI には出さない（自動保存のみ。移動終了から 500ms 後と終了時）。null は初回（画面中央に出す）。読み込み時に画面外なら画面内へ寄せる（Core `PopupPlacement` の補正を流用） |

常駐タブの「常駐モード」以外の項目（重ね順・自動で隠す・位置をロック・トリガーで盤面をカーソル位置へ移動、今後足す項目も含めてすべて）は、**常駐モードが OFF（`general.displayMode = popup`）の間も表示するが無効表示**（`IsEnabled = false`。値は保持し、ON にすればそのまま使われる。注記は読めるまま）。状態による無効化で、表示タブの「ステータス行」OFF で「時刻」が無効になるのと同じ扱い。トレイ / メニューバーのメニューで切り替えたときも有効・無効が追従する。

## データタブ（`data`）

| 項目名（UI） | JSON キー | 型 | 既定値 | 範囲・選択肢 | OS | 備考 |
|---|---|---|---|---|---|---|
| バックアップ世代数 | `data.backupGenerations` | int | 10 | 0〜100 | 共通 | 0 で世代バックアップなし（SPEC §9.3） |
| 設定フォルダを開く（ボタン） | — | — | — | | 共通 | 値は持たない。`IShellService.Launch` にフォルダアイテム（`DataPaths.Root`）を渡して開く（OS 固有コードを増やさない） |
| エクスポート / インポート（ボタン） | — | — | — | | 共通 | エクスポート = `settings.json` + `board.json` を 1 つの zip（`CPFileLauncher-backup-yyyyMMdd-HHmmss.zip`。 製品名の変更。旧名の zip もインポートできる）に保存（OS 標準の保存ダイアログ）。**背景画像が `background/` のコピー（相対パス参照）なら `background/<ファイル名>` も含める**（SPEC §3.7。絶対パス参照なら含めない）。`icons/` は含めない（キャッシュは再生成される。`iconOverride` は絶対パス参照のまま）。インポート = zip を選び、確認ダイアログの後に 2 ファイル（+ `background/`）を差し替えて**再起動**（既存の「再起動」処理を流用。読み込み済みの盤面・設定を生きたまま差し替える経路は作らない）。差し替え前の現行ファイルは `backup/` に退避 |

## 詳細タブ（`advanced`）

| 項目名（UI） | JSON キー | 型 | 既定値 | 範囲・選択肢 | OS | 備考 |
|---|---|---|---|---|---|---|
| ログを出力する | `advanced.logging` | bool | false | | 共通 | `logs/` へ。起動時に 14 日より古いログを削除（設定項目にはしない） |
| ログフォルダを開く（ボタン） | — | — | — | | 共通 | 値は持たない。Mac の確認でログを貼ってもらうときに使う。「設定フォルダを開く」と同じ経路 |
| 設定をリセット（ボタン） | — | — | — | | 共通 | settings.json を既定に戻す（board.json は触らない）。確認ダイアログあり。`AppSettings` のインスタンスを新しい既定値に差し替えて全項目の変更通知を出す（即時反映。設定ウィンドウも描き直す） |

## 入力データの制限

- ZIPの取り込みは信頼できるバックアップだけを対象とする。取り込んだ盤面には、実行可能なコマンドやネットワーク上の場所を含められる。確認画面にもこの注意を表示する。
- ZIPは圧縮状態64 MiB以下、128項目以下、中央ディレクトリ256 KiB以下。展開後はJSON各4 MiB以下、各ファイル32 MiB以下、全項目合計64 MiB以下。読み取り中の実バイト数にも上限を適用する。ZIP64・分割ZIPには対応しない。
- 背景画像と上書きアイコンの読み込みは、ファイル32 MiB以下、幅・高さ各16,384 px以下、総画素数4,000万以下。ヘッダーの寸法検査後に必要な表示サイズへ縮小デコードする。
- Drop-to-Openのファイル名はシェル命令へ直接連結しない。macOSのコマンドは位置引数、Windowsのコマンド・バッチは子プロセスの環境変数から渡す。Windowsでは管理者実行するコマンド・バッチへのDrop-to-Openには対応しない。登録したコマンド自身が引数を命令として再評価する使い方は避ける。
