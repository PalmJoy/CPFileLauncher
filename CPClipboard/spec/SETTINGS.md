# CPClipboard 設定項目一覧

`settings.json`（SPEC §9）に保存される設定項目の一覧。**設定項目の正はこのファイル**で、SPEC §7 には方針だけを置く。
項目を足す・消す・既定値や範囲を変えるときは、先にこの表を直してから `src/CPClipboard.Core/Model/AppSettings.cs` と設定画面を合わせる。
定型文とグループは `settings.json` ではなく `templates.json`（SPEC §9.3）に入る。編集は独立した「編集」ウィンドウ（SPEC §6.3）。設定には定型文タブを置かない。

## 読み方

- **JSON キー**: `settings.json` 内のパス（camelCase。enum は文字列、フラグ enum は `"ctrl, alt"` のようにカンマ区切り）。
- **既定値**: ファイルにキーが無いときの値。範囲外の値は読み込み時に範囲へ丸める（`AppSettings.Normalize`）。未知のキーは無視され、次の保存で消える。
- **OS**: 共通 / Win のみ / Mac のみ。片方の OS で使えない項目は設定画面に**出さない**（SPEC §1.2）。注記を付けるのは「使えるが制約がある」項目だけ。
- 単位: 座標・サイズは Windows では物理 px、macOS では pt。盤面の幅・文字の大きさは論理 px。
- 「LOOK §n」は `spec/LOOK.md`、「EFFECTS」は `spec/EFFECTS.md`。

## ファイル全体

| JSON キー | 型 | 既定値 | 備考 |
|---|---|---|---|
| `schemaVersion` | int | **1** | 現行より新しい値のファイルは読み取り専用（保存しない）。無ければ 1 とみなす |

## 一般タブ（`general`）

| 項目名（UI） | JSON キー | 型 | 既定値 | 範囲・選択肢 | OS | 備考 |
|---|---|---|---|---|---|---|
| OS ログイン時に自動起動 | `general.autoStart` | bool | false | | 共通 | 値は OS 側の登録状態と二重になるので、起動時と設定画面を開くたびに OS 側（`IAutoStartService.IsEnabled`）を正として読み直す。登録する実行ファイルは `Environment.ProcessPath`。Mac で `.app` 以外から起動中（`NSBundle.mainBundle.bundleIdentifier` が無い）は行ごと非表示（無効表示 + 注記にはしない。SPEC §1.2） |
| 言語 | `general.language` | string | `system` | `system` / `ja` / `en`（それ以外の値は読み込み時に `system` へ丸める） | 共通 | UI 表示: システム / 日本語 / English（言語名は各言語の自称で、UI 言語に関わらず固定）。**変更は再起動後に反映**: 注記「再起動後に反映されます。」に加え、今の表示言語と選択が違う間だけ「今すぐ再起動」ボタンを行の右に出す（押すと `hub.Flush` → 既存の再起動処理）。`system` は OS の UI 言語（.NET の `CultureInfo.CurrentUICulture` 初期値）が日本語なら日本語、それ以外は英語（英語が既定リソース）。`Program` が Avalonia 起動前に `SettingsPeek` でこのキーだけ先読みして `CurrentUICulture` / `DefaultThreadCurrentUICulture` を設定する（`CurrentCulture` = 数・日付の書式は変えない）。開発・確認用の起動引数 `--lang ja|en` は設定より優先され、保存されない。訳語は `spec/TERMS.md`、方針は SPEC §7、macOS `.app` 起動時の判定は  |
| 起動時のガイドを表示しない | `general.hideStartupGuide` | bool | false | | 共通 | SPEC §1「起動時のガイド」。ガイドの「次回以降表示しない」と同じ値。設定画面にも出す（注記「起動時に呼び出し方の案内を出しません。」） |
| 権限の設定…（ボタン） | — | — | — | | Mac のみ | 権限ガイドを開く（SPEC §4.4）。値は持たない。行名「アクセシビリティ」、注記「呼び出しと自動の貼り付けに必要です。」 |

## 表示タブ（`appearance` / `board`）

| 項目名（UI） | JSON キー | 型 | 既定値 | 範囲・選択肢 | OS | 備考 |
|---|---|---|---|---|---|---|
| 配色 | （`appearance.colors` の表示専用） | — | — | シアン / レッド / ブルー / グリーン / パープル / カスタム | 共通 | `ComboBox`。各項目に主色・副色の色見本 12×12 を 2 つ + 名前。プリセットを選ぶと下の 2 色を書き換える。2 色がプリセットと完全一致すればそのプリセット、違えば「カスタム」を表示（「カスタム」は選べない）。値は保存しない。LOOK §3.6「配色」 |
| 　主色 | `appearance.colors.primary` | string（`#RRGGBB`） | `#00E5FF` | 任意の色。読み込み時: 大文字小文字を問わない、8 桁ならアルファを捨てる、不正ならシアンの値に戻してログ 1 行、表記は大文字の `#RRGGBB` にそろえる。**色の値そのものは変えない**（自動補正しない。暗い色を選んだときの見え方はユーザーの責任、LOOK §3.6「読みやすさ」） | 共通 | UI: `ColorPicker`+ 右に hex の表示（等幅）。枠・発光・タブ・ホバー・角ブラケット（左上右下）・グリッチの影・光の玉・設定画面のアクセント・**背景と文字の色相**がすべて追従（導出規則は LOOK §3.6）。即時反映（辞書の差し替え。ドラッグ中は 1 フレームに 1 回に間引く）。変更時の通知は `Colors`（`Render` 不要） |
| 　副色 | `appearance.colors.secondary` | string（`#RRGGBB`） | `#FF2BD6` | 同上 | 共通 | UI: 同上 + 行の右端に「⇄」（主色と副色を入れ替える）。角ブラケット（右上左下）・グリッチの影の片方・`orbTwin` の 2 個目に使う。背景・文字は副色からは導かない |
| 不透明度 | `appearance.opacity` | int (%) | **80** | **30〜100**、5 刻み（スライダー。範囲外は丸め、5 の倍数でない値は最も近い 5 の倍数に） | 共通 | 既定は 80。既存の settings.json に `opacity` が書かれていればその値を尊重（既定はキーが無いときだけ）。盤面全体（背景・ラベル）にかかる。両モード有効。即時反映（SPEC §3.1）。「ホバー中は不透明」は §12 |
| 背景画像 | `appearance.background.image` | string / null | null（なし） | データフォルダからの相対パス `background/<ファイル名>`（設定画面から選んだとき）または絶対パス（手編集。`~` / 環境変数を展開）。対応形式 png / jpg / jpeg / bmp / gif / webp | 共通 | LOOK §3.7。UI: プレビュー（120×68）+ ファイル名 + 「選択…」「クリア」。「選択…」はファイルを `background/` へコピーして相対パスを保存、「クリア」は null にしてコピーを削除。**プレビュー（サムネイル 120×68）は Explorer / Finder からの画像ファイルのドロップ先でもある**（LOOK §3.7「ドロップで設定」）: 落とすと「選択…」と同じ処理。対応形式の最初の 1 ファイルだけ、フォルダ / 非対応 / テキストだけならトースト。ドラッグ中はサムネイルの枠が `FlDropBorder` / `FlDropBackground`、画像なしなら「ここにドロップ」。行の下に注記「サムネイルに画像ファイルをドロップしても設定できます。」。盤面の面（`Frame` の内側）だけ・全ページ共通。見つからない / 読めないときは面の色で表示し、値は消さずこの行に `FlError` の注記（トーストは出さない）。長辺 2048 px に縮小してバックグラウンドでデコード（表示遅延に影響しない）。**表示する大きさ（面 × 拡大率）に一度だけ縮小した画像を作り置きし、毎フレームは等倍で貼る**（LOOK §3.7「作り置き」。常時の演出と重なったときの CPU 対策。見た目・設定値は変わらない）。変更時の通知は `Background`（`Render` 不要）。既存の settings.json にキーが無ければ「なし」 |
| 　表示方法 | `appearance.background.fit` | enum | `fill` | `fill` / `fit` / `stretch` / `tile` / `center` | 共通 | UI 表示: 埋める / 収める / 引き伸ばす / 並べる / 中央（`ComboBox`）。`ImageBrush` の対応は LOOK §3.7。画像があるときは面の地の色を描かないので、「収める」「中央」で画像が届かない余白は覆いの色だけになる（透ける）。画像が無いときは無効表示 |
| 　画像の覆い | `appearance.background.overlay` | int (%) | 40 | 0〜90、5 刻み（スライダー。範囲外は丸め） | 共通 | 画像の上に盤面の面の色（`FlBoardBackground`）をこの不透明度で重ねる（テーマ・基調色に追従）。画像があるときは面の地の色を描かないので、**盤面の暗さはこの覆いだけで決まる**（LOOK §3.7 描画の層）。注記「盤面の色を画像の上に重ねます。大きいほど画像が薄くなり、アイコンと文字が読みやすくなります。」。画像が無いときは無効表示 |
| 　画像の不透明度 | `appearance.background.imageOpacity` | int (%) | 100 | 10〜100、5 刻み（スライダー。範囲外は丸め） | 共通 | 画像の層（`Backdrop`）の `Opacity`。画像があるときは面の地の色を描かないため、下げると**画像の部分からデスクトップが透ける**（覆いの色は残る）。100 なら埋める / 引き伸ばす / 並べるの見た目は従来どおり。注記「下げると画像の部分からデスクトップが透けます。」。「画像の覆い」の直下に置く。画像が無いときは無効表示。ウィンドウ全体の `appearance.opacity` とは独立に掛かる（両方が効く）。変更時の通知は `Background`（`Render` 不要）。`schemaVersion` は 2 のまま（省略可能な項目の追加。キーが無ければ 100） |
| ステータス行 | `appearance.hud.statusBar` | bool | true | | 共通 | LOOK §3.9 H1。盤面下部に `GRP 2 · 名前`（左）/ `ITEMS 4`（中央）/ 日時（右端）の 1 行。ON のときウィンドウが **21 px** 高い。ToggleSwitch。注記「盤面の下にグループ・件数・時刻を出します。」。変更時の通知は `Hud`（`Render` あり: 高さが変わる） |
| 　時刻 | `appearance.hud.clock` | enum | `minutes` | `off` / `minutes` / `seconds` | 共通 | UI 表示: 表示しない / 時:分 / 時:分:秒（`ComboBox`）。24 時間表記で固定。`minutes` は分の変わり目だけ、`seconds` は 1 秒ごとに描き直す。盤面が見えている間だけ動く。ステータス行 OFF のときは無効表示（状態による無効化。LOOK §3.7 の表示方法と同じ扱い）。変更時の通知は `Hud`（`Render` 不要。文字列の組み直しとタイマーの付け替えだけ） |
| 　日付（年/月/日） | `appearance.hud.date` | bool | true | | 共通 | LOOK §3.9 H1。時刻の前に `yyyy/MM/dd` を付ける（`2026/10/04 15:05`。書式は固定、OS の日付書式には従わない）。時刻が「表示しない」のときは日付だけ `2026/10/04` を出し、タイマーは日付の変わり目に合わせる。時刻・日付とも OFF なら右端の区画は出ない。ToggleSwitch（「時刻」の下、注記なし）。ステータス行 OFF のときは無効表示。変更時の通知は `Hud`（`Render` 不要）。キーが無ければ ON（省略可能な追加。`schemaVersion` は上げない） |
| 背景グリッド | `appearance.hud.grid` | bool | true | | 共通 | LOOK §3.9 H6「背景グリッドの仕様」。盤面の面（背景画像の覆いの上・タブとアイコンの下）に 24 px 間隔の 1 px の線（色は主色 `FlHudGrid`、濃さは下の「グリッドの濃さ」）。ToggleSwitch。注記「行の下に主色の細い線を敷きます。」。変更時の通知は `Hud`（`Render` の中で層の `IsVisible` を付け直す。ウィンドウの大きさは変わらない）。省略可能な追加なので `schemaVersion` は上げない |
| 　グリッドの濃さ | `appearance.hud.gridOpacity` | int (%) | **3** | 0〜10、1 刻み（スライダー。範囲外は読み込み時に 0〜10 へ丸める） | 共通 | 背景グリッドの線の不透明度。`GridLayer.Opacity = gridOpacity / 100`（トークン `FlHudGrid` はアルファなしの主色。配色を変えても濃さは保たれる）。**0 なら層ごと非表示**。`ValueSlider`（`%` 表示、注記なし）。「背景グリッド」の直下に置き、背景グリッド OFF の間は無効表示（状態による無効化。ステータス行 OFF で「時刻」「日付」が無効になるのと同じ扱い）。変更時の通知は `Hud`（`Render` の中で `Opacity` を付け直す）。キーが無ければ 3（省略可能な追加。`schemaVersion` は上げない） |
| 盤面の幅 | `appearance.boardWidth` | int (px) | **420** | 280〜800、20 刻み（スライダー） | 共通 | SPEC §3.1。発光の余白（40 × 2）を含まない面の幅。即時反映（`Render`） |
| 最大行数 | `appearance.maxRows` | int | **12** | 4〜30 | 共通 | SPEC §3.1。これより多い定型文はスクロール。盤面の高さは `clamp(件数, 3, maxRows)` 行 |
| 文字の大きさ | `appearance.fontSize` | int (px) | **13** | 11〜20 | 共通 | 行の文字。行の高さは × 2（SPEC §3.1） |
| 全文のプレビュー | `board.preview` | bool | true | | 共通 | SPEC §3.4。注記「行に 0.4 秒とどまると全文を横に出します。」 |
| ホイールでグループを切り替える | `board.wheelSwitchesGroup` | bool | true | | 共通 | SPEC §6.1。一覧があふれているときの一覧の上ではスクロールが優先 |
| 前回のグループ | `board.lastGroup` | int | 1 | 0〜9 | 共通 | UI には出さない（閉じるたびに自動保存。SPEC §3.2） |

## 演出タブ（`appearance.animation` / `appearance.effects` / `appearance.glitchIntensity` / `appearance.vsync` と、静止した走査線 `appearance.hud.scanlines` / `scanlineOpacity` / `scanlinePitch`）

CPFileLauncher と同じ。演出の ID は EFFECTS の表（`groupSwitch` / `tabHighlight` / `rowHover` / `rowPress` / `pasteFlash` / `previewShow` / `toastShow` / `toastHide` と常時の演出 3 つ、表示 / 非表示）。

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
| 走査線（小見出し「静止した走査線」の下） | `appearance.hud.scanlines` | bool | **true** | | 共通 | LOOK §3.9 H14「走査線の仕様」。盤面の面に主色の 1 px の横線を「走査線の間隔」おきに敷く。層は背景グリッドの直後・行の下（面・背景画像・覆い・グリッドの上、文字の下。八角形で切れる）。ToggleSwitch、注記「行の下に主色の横線を敷きます。動かないので CPU を使わず、「アニメーション」がオフでも消えません。」（後半の 1 文は演出タブへの移動で追加）。**「すべて既定に戻す」の直下に小見出し「静止した走査線」（`Settings_Effects_ScanlinesHeading`）を置き、その下にこの行**。変更時の通知は `Hud`（`Render` の中で層の `IsVisible` を付け直す。ウィンドウの大きさは変わらない）。静的で CPU は増えない。**`appearance.animation` とは無関係（OFF でも消えず、設定画面の行も無効にしない）。「すべて既定に戻す」の対象外**。**既定 ON・25% は で確定**。キーが無ければ ON（省略可能な追加。`schemaVersion` は上げない）。リソースキー `Settings_Effects_Scanlines` / `_Note`（旧 `Settings_Appearance_Hud_Scanlines*`） |
| 　走査線の濃さ | `appearance.hud.scanlineOpacity` | int (%) | **25** | 0〜50、5 刻み（スライダー。範囲外は読み込み時に 0〜50 へ丸め、5 の倍数でなければ最も近い 5 の倍数に） | 共通 | 走査線の不透明度。`ScanlineLayer.Opacity = scanlineOpacity / 100`（トークン `FlHudScanline` はアルファなしの主色。配色を変えても濃さは保たれる）。**0 なら層ごと非表示**。`ValueSlider`（`%` 表示、注記なし）。「走査線」の直下に置き、走査線 OFF の間は無効表示（背景グリッド OFF でグリッドの濃さが無効になるのと同じ扱い）。値の根拠（Mac、Retina）: 4% / 2 px と 12% / 2 px は「見えない」、**25% / 3 px** をユーザーが選定。グリッド（0〜10、1 刻み）より範囲が広いのは、横線だけで面積が小さく濃くないと見えないため。変更時の通知は `Hud`（`Render` の中で `Opacity` を付け直す）。キーが無ければ 25（省略可能な追加。`schemaVersion` は上げない）。リソースキー `Settings_Effects_ScanlineOpacity` |
| 　走査線の間隔 | `appearance.hud.scanlinePitch` | int (px) | **3** | 2 / 3 / 4（`ComboBox`。それ以外の値は読み込み時に 3 に戻す = `ButtonSize` と同じ扱い。`HudSettings.ScanlinePitches = [2, 3, 4]`） | 共通 | 横線の間隔（n px ごとに 1 本）。`ScanlineLayer.Pitch`（`StyledProperty`、`AffectsRender`）に入れる。UI 表示は「2 px / 3 px / 4 px」（書式リソース `Common_PixelValue` = `{0} px`、両言語同じ。数値は翻訳しない）。「走査線の濃さ」の直下に置き、走査線 OFF の間は無効表示。2 px は細かく濃さを上げると CRT らしい、4 px は粗く目立ちにくい。変更時の通知は `Hud`（`Render` の中で `Pitch` を付け直す = 層の描き直しだけ）。キーが無ければ 3（省略可能な追加。`schemaVersion` は上げない）。リソースキー `Settings_Effects_ScanlinePitch` |

## ポップアップタブ（`popup`）

表示位置（キーボード → マウス）→ カーソルの位置 → 閉じる条件の順。

| 項目名（UI） | JSON キー | 型 | 既定値 | 範囲・選択肢 | OS | 備考 |
|---|---|---|---|---|---|---|
| 表示位置（キーボードで開いたとき） | `popup.keyboard.position` | enum | `cursor` | `cursor` / `lastPosition` / `screenCenter` / `fixed` | 共通 | ホットキーで開いたときに使う。UI 表示: カーソル位置 / 前回の位置 / 画面中央 / 固定座標。「カーソル位置」はキーボードでも**マウスカーソル**の位置（キャレット位置は SPEC §12）。カーソル位置は画面内に収まるよう補正（SPEC §3.3） |
| 　座標 X / Y（キーボード） | `popup.keyboard.x`, `popup.keyboard.y` | int? | null | 画面内 | 共通 | `fixed` のときの座標。`lastPosition` のときは閉じるたびに前回位置として上書き保存される欄（**`lastPosition` を選んでいる組だけ**に書く。`fixed` の組の座標は閉じても変わらない）。単位は OS で異なる（Win: 物理 px、Mac: pt）。UI は `fixed` のときだけ有効 + 「今の盤面の位置を使う」ボタン |
| 表示位置（マウス操作で開いたとき） | `popup.mouse.position` | enum | `cursor` | 同上 | 共通 | マウストリガーで開いたときに使う。**トレイ／メニューバーアイコンのクリック・メニュー（盤面を表示 / グループ ▸）・2 重起動からの表示もこちら**（SPEC §3.3）。UI は 2 組を同じ見た目で縦に並べ、下に注記「トレイアイコンやメニューから開いたときはマウス操作の設定を使います」 |
| 　座標 X / Y（マウス） | `popup.mouse.x`, `popup.mouse.y` | int? | null | 画面内 | 共通 | キーボード側と同じ規則 |
| 盤面上のカーソルの位置 | `popup.cursorAnchor` | enum | `top` | `topLeft` / `top` / `topRight` / `left` / `center` / `right` / `bottomLeft` / `bottom` / `bottomRight` | 共通 | SPEC §3.3「カーソルのアンカー」。**キーボード・マウス共通の 1 項目**（旧 `popup.keyboard.cursorAnchor` / `popup.mouse.cursorAnchor` は未リリースのためマイグレーションなし、残っていても読み飛ばす）。盤面（ウィンドウ）のどの点がカーソルに来るか。辺・角はウィンドウ外形から `20 × 拡大率` 内側（= 見える枠の 20 外側）、`center` はちょうど中心。`top` が従来の挙動。どちらの組で開いても、その組の表示位置が `cursor` のとき（および `lastPosition` / `fixed` で座標が未保存のため退避したとき）に使う。UI はキーボード・マウスの 2 組の下に独立した 1 行（字下げなし）で 3 × 3 のトグルボタン（`AnchorPicker`。選択中は主色の枠 + 薄い面 + 主色の点、各ボタンのツールチップとアクセシブル名 = 位置の名前 `Enum_CursorAnchor_*`）。**キーボード・マウスの表示位置のどちらかが `cursor` なら有効、どちらも `cursor` 以外なら無効表示**（非表示にはしない）。注記 `Settings_Triggers_CursorAnchor_Note`（`spec/TERMS.md`）。変更通知は `Popup`（次に開いたときから効く）。キーが無ければ `top`（`schemaVersion` は 3 のまま）。不正な enum 値は他の enum と同じく読み込み失敗（SPEC §9.3） |
| 盤面外クリック／他ウィンドウで閉じる | `popup.closeOnOutsideClick` | bool | true | | 共通 | フック座標で判定（SPEC §3.3） |
| トリガー再実行で閉じる（トグル） | `popup.toggleOnTrigger` | bool | true | | 共通 | |
| マウスが離れたら閉じる | `popup.closeOnMouseLeave` | bool | false | | 共通 | SPEC §3.3。表示中だけ 100ms 間隔の `DispatcherTimer` で `IWindowService.GetCursorPosition` を見て、盤面矩形からの距離（Core `MouseLeaveTracker`。矩形の外側までの最短距離）が閾値を超えたら閉じる（フックの移動イベントは使わない）。**一度カーソルが盤面に入ってから**判定を始める（画面中央・固定座標の表示でカーソルが遠い場合に即閉じしないため）。モーダル中（`RunModalAsync`）は判定しない |
| 離れたとみなす距離 | `popup.mouseLeaveDistance` | int | 200 | 50〜1000（SPEC は「距離設定可」のみ。範囲はここで仮決め） | 共通 | 単位は OS に従う。「マウスが離れたら閉じる」OFF のときスライダーは無効表示 |
| Esc で閉じる | — | — | 常に有効 | | 共通 | 設定項目ではない（SPEC §3.3） |

## トリガータブ（`triggers`）

「編集用」→「履歴用」の2段。各段は修飾キーの 2 回押し → ホットキー → マウス操作の順。編集用は既存の`triggers`、履歴用は同じ構造の`historyTriggers`を使う。履歴用の既定は2回押し・ホットキーOFF、マウス操作なし。同一操作なら編集用を優先。

| 項目名（UI） | JSON キー | 型 | 既定値 | 範囲・選択肢 | OS | 備考 |
|---|---|---|---|---|---|---|
| 修飾キーの 2 回押し | `triggers.doubleTap.enabled` | bool | **true** | | 共通 | SPEC §4.1。注記「他のアプリが同じ操作を使っていると両方が反応することがあります。」 |
| 　キー | `triggers.doubleTap.modifier` | enum | `ctrl` | `ctrl` / `alt` / `shift` / `meta` | 共通 | UI 表示は OS で変える: Win「Ctrl / Alt / Shift / Win」、Mac「⌃ / ⌥ / ⇧ / ⌘」（`Loc.Shortcut`）。2 回押しが OFF の間は無効表示 |
| 　判定の間隔 | `triggers.doubleTap.intervalMs` | int (ms) | **300** | 200〜500、10 刻み | 共通 | 1 回目を離してから 2 回目を押すまでの上限（SPEC §4.1）。押している時間の上限 250 ms は定数で設定にしない |
| ホットキーを使う | `triggers.hotkey.enabled` | bool | **false** | | 共通 | SPEC §4.3 |
| 　ホットキー | `triggers.hotkey.modifiers` / `triggers.hotkey.key` | flags / string | `ctrl, alt` / `V` | 修飾キー 1 つ以上 + SharpHook の `KeyCode` 名から `Vc` を除いたもの | 共通 | 記録欄をクリックしてキーを押して決める（CPFileLauncher と同じ記録欄）。記録中はトリガーの判定を止める |
| マウス操作（複数） | `triggers.mouse[]` | 配列 | `[{ enabled: true, button: middle, modifiers: alt }]` | 要素: `enabled`(bool, 既定 true) / `button`(`middle` / `left` / `right` / `x1` / `x2`) / `modifiers`(flags) | 共通 | SPEC §4.2。行ごとに ON/OFF・ボタン・修飾キー、「追加」「削除」。`left` / `right` は修飾キーなしを選べない（UI で弾き、読み込み時は捨ててログ）。`x1` / `x2` は Mac で Mission Control 等と競合する注記。注記「CPFileLauncher の既定（Ctrl + 中クリック）と同じ操作にすると両方が反応します。」 |

## 貼り付けタブ（`paste`）

| 項目名（UI） | JSON キー | 型 | 既定値 | 範囲・選択肢 | OS | 備考 |
|---|---|---|---|---|---|---|
| 自動で貼り付ける | `paste.autoPaste` | bool | **true** | | 共通 | SPEC §5.2。OFF なら選ぶとクリップボードにコピーするだけ。注記「Shift を押しながら選ぶと、この設定に関係なくコピーだけになります。」 |
| 貼り付け後にクリップボードを元に戻す | `paste.restoreClipboard` | bool | **false** | | 共通 | SPEC §5.3。既定では選んだ定型文を残し、後から Ctrl+V / ⌘V でも貼り付けられる。キーがない場合は false。既存設定の明示 true / false は維持し、移行処理で上書きしない。自動で貼り付けるが OFF の間は無効表示。注記「OFF（既定）のままなら、貼り付け後も定型文がクリップボードに残ります。」 |
| 　元に戻すまでの時間 | `paste.restoreDelayMs` | int (ms) | **500** | 200〜3000、100 刻み | 共通 | SPEC §5.1 の 8。貼り付けが間に合わないアプリ向けに延ばせる |
| （注記）貼り付けられない場面 | — | — | — | | Win / Mac で文が違う | SPEC §5.5。Win「管理者として動いているアプリには貼り付けられません（コピーだけになります）。」、Mac「パスワードの入力欄には貼り付けられません（コピーだけになります）。」 |

## 編集ウィンドウ（設定タブとは独立）

値は `templates.json`（SPEC §9.3）。UI は SPEC §6.3。設定キーは持たない。

## 履歴タブ（`history`）

| 項目名（UI） | JSON キー | 型 | 既定値 | 範囲・選択肢 | 備考 |
|---|---|---|---|---|---|
| 履歴を消去 | — | ボタン | — | 確認して消去 | 現在のクリップボードは変更しない |
| 履歴の自動消去 | `history.autoClear` | bool | false | ON/OFF | コピーから保持時間が経過した項目を消す |
| 保持時間（分） | `history.autoClearMinutes` | int | 60 | 1〜2,147,483,647、自由入力 | 自動消去OFF時は無効表示 |
| 最大保存件数 | `history.maxItems` | int | 1000 | 1〜1000、自由入力 | 「1000件まで保存できます。」。超過分は古い順に消去 |

## データタブ（`data`）

| 項目名（UI） | JSON キー | 型 | 既定値 | 範囲・選択肢 | OS | 備考 |
|---|---|---|---|---|---|---|
| バックアップ世代数 | `data.backupGenerations` | int | 10 | 0〜100 | 共通 | templates.json の世代バックアップ。0 で世代バックアップなし（SPEC §6.4） |
| 設定フォルダを開く（ボタン） | — | — | — | | 共通 | 値は持たない。`IShellService.Open(DataPaths.Root)` |
| エクスポート / インポート（ボタン） | — | — | — | | 共通 | SPEC §9.5。エクスポート = `settings.json` + `templates.json`（+ 背景画像のコピー）を `CPClipboard-backup-yyyyMMdd-HHmmss.zip` に保存（OS 標準の保存ダイアログ）。インポートは確認ダイアログ → 置き換え → 再起動後に反映 |

## 詳細タブ（`advanced`）

| 項目名（UI） | JSON キー | 型 | 既定値 | 範囲・選択肢 | OS | 備考 |
|---|---|---|---|---|---|---|
| ログを出力する | `advanced.logging` | bool | false | | 共通 | `logs/` へ。起動時に 14 日より古いログを削除（設定項目にはしない） |
| ログフォルダを開く（ボタン） | — | — | — | | 共通 | 値は持たない。Mac の確認でログを貼ってもらうときに使う。「設定フォルダを開く」と同じ経路 |
| 設定をリセット（ボタン） | — | — | — | | 共通 | settings.json を既定に戻す（board.json は触らない）。確認ダイアログあり。`AppSettings` のインスタンスを新しい既定値に差し替えて全項目の変更通知を出す（即時反映。設定ウィンドウも描き直す） |

## 入力データの制限

- 定型文・グループ・`templates.json` の上限は SPEC §9.4。
- ZIP の取り込みは信頼できるバックアップだけを対象とする。ZIP は圧縮状態 64 MiB 以下、128 項目以下、中央ディレクトリ 256 KiB 以下。展開後は JSON 各 16 MiB 以下、各ファイル 32 MiB 以下、全項目合計 64 MiB 以下。ZIP64・分割 ZIP には対応しない。
- 背景画像の読み込みは、ファイル 32 MiB 以下、幅・高さ各 16,384 px 以下、総画素数 4,000 万以下。ヘッダーの寸法検査後に必要な表示サイズへ縮小デコードする。
