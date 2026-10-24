# CPClipboard 演出（アニメーション）仕様

演出の種類・既定値・動作はこの仕様で定義する。設定項目は `SETTINGS.md` を参照。

## 方針（SPEC §3.1 と同じ内容。実装は App 層 `Effects/EffectRunner.cs`（EffectSpec → Avalonia Animation / Transitions）と Core `Effects/EffectCatalog.cs`（ID・既定値・選べる種類））

- **全体 ON/OFF**: `appearance.animation`（既定 ON）。OFF にすると下表の全演出が「なし」（時間 0）になる。
- **個別パラメータ**: 各演出は「種類 / 時間 ms / イージング」の 3 つで表す。既定値はコード内の `EffectCatalog`（`src/CPClipboard.Core/Effects/`）に持ち、**この表と一致させる**。ユーザーが変えた値だけ `settings.json` の `appearance.effects.<演出 ID>` に保存する（SETTINGS.md 演出タブ）。設定画面の**「演出」タブ**からも変えられる: 常時の演出 3 つは普通の行、他は折りたたみ「詳細設定」（SPEC §7「演出タブの構成」）。
- **表示遅延 100ms（SPEC §11）との関係**: 表示遅延は「トリガー → 盤面の**最初のフレームが描かれるまで**」で計り、表示演出の時間は含めない。表示演出は最初のフレームから始まり、**演出中も盤面はクリック・キー操作を受け付ける**（透明度・拡大の変化は当たり判定を妨げない）。表示演出の時間は 200ms 以下にする（スライダーの上限はそれより大きいが、注記で勧める）。**例外: `glitch` の既定は 250 ms**（グリッチは演出中も入力を受け付け、表示遅延にも含めないので、200 ms を超えても操作は待たされない）。
- **非表示演出**: 盤面を閉じる操作の瞬間に入力の受付をやめ、フォーカスの返却（SPEC §3.3）も**即座に**行う。ウィンドウの `Hide()` だけを演出の終わりまで遅らせる。非表示演出中にトリガーで再表示されたら演出を打ち切って表示に戻す。
- **種類**: `none`（なし）/ `fade`（透明度）/ `zoom`（透明度 + 拡大縮小。表示は 0.96→1.0、非表示は 1.0→0.96）/ `slide`（透明度 + 平行移動。向きは演出ごとに固定。移動量 12 px）/ `highlight`（色の変化。ホバー・押下などの状態変化用）/ **`glitch`**（盤面の出現・消失時に一瞬だけ横ずれ + 色にじみ。同じ時間の一定速度のフェード（表示 = イン、非表示 = アウト）を重ねる。盤面の表示・非表示専用。詳細は下の「glitch の詳細」）/ **`glow`**（`highlight` に発光の強まりを足したもの。ホバー・タブ・ドロップ先用。「glow の詳細」）/ **`orb`**（枠を周回する光の玉 1 個、主色）/ **`orbTwin`**（光の玉 2 個。主色と副色が対角で周回）/ **`pulse`**（発光の柔らかい明滅、弱）/ **`pulseStrong`**（同、強）/ **`beam`**（走査線の帯。高さ 60 px の主色のグラデーションの帯が面を上から下へ流れ続ける）。`orb*` / `pulse*` / `beam` は常時の演出専用（「常時の演出の詳細」）。演出ごとに選べる種類は「選べる種類」列のとおり。
- **既定値は 1 組**。下表の「初期値」列がその値。ユーザーが変えた値は ID 単位で保存される（SETTINGS.md 表示タブの備考）。
- **イージング**: `linear` / `easeOut`（既定。減速して止まる）/ `easeIn`（加速して消える）/ `easeInOut` / `backOut`（少し行き過ぎて戻る。強調用）。Avalonia のクラス対応: `LinearEasing` / `CubicEaseOut` / `CubicEaseIn` / `CubicEaseInOut` / `BackEaseOut`。
- **時間**: 0〜1000 ms（0 は「なし」と同じ）。範囲外は読み込み時に丸める。**常時の演出**（`frameOrb` / `glowPulse` / `scanBeam`）だけは「時間」が **1 周 / 1 周期 / 1 回流れ切る長さ**で、範囲も演出ごとに違う（`EffectDefinition.MinDurationMs` / `MaxDurationMs` / `DurationStepMs`。下表の備考）。種類が「なし」以外で範囲より短ければ下限に、長ければ上限に丸める。
- **常時の演出**: `frameOrb` / `glowPulse` / `scanBeam` は既定 ON。盤面が見えている間だけ動かし、非表示・表示と非表示のグリッチ中・アニメーション OFF・OS の視差効果低減時は停止する。常時の演出のイージングは `linear` 固定。時間は一周・一周期の長さを表す。
- **ウィンドウ透過**: 盤面ウィンドウは背景透過（`TransparencyLevelHint = Transparent`）なので、フェード・ズームは盤面の内容（`Frame`）の `Opacity` / `RenderTransform` で行い、不透明度設定（`Window.Opacity`）とは別の層で掛け合わせる。透過が取れない環境ではフェードが「黒い矩形のフェード」になりうる → 。
- 参考にしたもの: CLaunch / Clibor（ウィンドウ表示時のフェード／スライド、行のホバー強調、トースト）、macOS Spotlight / Alfred（短いフェード + わずかなズーム）、Windows のフライアウト（下からのスライド + フェード）。

## 演出一覧

ID は `appearance.effects` のキー。初期値は種類・時間・イージングの既定値。

| ID | 演出名 | 対象 | きっかけ | 選べる種類 | 初期値（種類 / ms / イージング） | 設定キー |
|---|---|---|---|---|---|---|
| `boardShow` | 盤面の表示 | 盤面ウィンドウ全体（`Frame`）。**加えて設定画面・すべてのダイアログ（`ChromeWindow`。glitch のときだけ）** | トリガー / トレイクリック / 設定画面・ダイアログを開く | none / fade / zoom / slide（下から）/ **glitch**（+ 同時間のフェードイン） | **glitch / 250 / —** | `appearance.effects.boardShow` |
| `boardHide` | 盤面の非表示 | 同上（**設定画面・ダイアログも**。glitch のときだけ） | Esc / 外クリック / 貼り付け・コピーの後 / トグル / マウスが離れた / 設定画面・ダイアログを閉じる（× / Esc / OK / キャンセル。アプリ終了・OS 終了では演出しない） | none / fade / zoom / slide（下へ）/ **glitch**（+ 同時間のフェードアウト） | **glitch / 250 / —** | `appearance.effects.boardHide` |
| `groupSwitch` | グループ切替 | 文章の一覧（`TemplateList`） | ヘッダーのクリック / 数字キー 0〜9 / Ctrl+Tab / ← → / ホイール（SPEC §3.2 / §6.1） | none / fade / slide（切替方向へ。左右） | **fade / 80 / easeOut** | `appearance.effects.groupSwitch` |
| `tabHighlight` | グループタブのホバー・選択の強調 | グループタブ（`ToggleButton.tab`） | タブにマウスを乗せる / 選択が変わる | none / highlight / **glow** | **glow / 80 / linear** | `appearance.effects.tabHighlight` |
| `rowHover` | 行のホバー・選択の強調 | 行（`Button.row`）の背景・枠 | 行にマウスを乗せる / 外す / ↑↓ で選択が移る | none / highlight / **glow** | **glow / 80 / linear** | `appearance.effects.rowHover` |
| `rowPress` | 行の押下 | 行の中身（文字列） | 左ボタン押下 / 離上 | none / zoom（0.97 倍に縮む。行は横長なので FL の 0.92 より控えめ） | **zoom / 60 / easeOut** | `appearance.effects.rowPress` |
| `pasteFlash` | 貼り付けのフィードバック | 選んだ行 | 選択の確定（クリック / Enter）の直後、盤面を隠す前 | none / fade（一瞬明滅）/ **glow**（行の発光が一瞬強まる） | **glow / 120 / easeOut** | `appearance.effects.pasteFlash` |
| `previewShow` | 全文のプレビューの表示 | 全文のプレビュー（SPEC §3.4） | ホバー / 選択が 400 ms 止まる | none / fade | **fade / 100 / linear** | `appearance.effects.previewShow` |
| `toastShow` | トーストの表示 | トーストウィンドウ（LOOK §3.6「各部の見た目」） | トーストが出る | none / fade / slide（下から） | **slide / 150 / easeOut** | `appearance.effects.toastShow` |
| `toastHide` | トーストの消失 | 同上 | 表示時間の経過 / クリック | none / fade | **fade / 150 / easeIn** | `appearance.effects.toastHide` |
| `frameOrb` | 光の玉（枠を周回） | 盤面の枠線の上（`FrameChrome.Overlays` の `OrbLayer`） | **常時**（盤面が見えている間。表示演出の後に始まり、非表示演出・非表示で止まる） | none / **orb**（1 個、主色）/ **orbTwin**（2 個、主色 + 副色が対角） | **orb / 8000 / —** | `appearance.effects.frameOrb` |
| `glowPulse` | 発光の明滅（呼吸） | 盤面の枠の発光 | 同上 | none / **pulse**（弱: 発光が最大 +45%）/ **pulseStrong**（強: 最大 +90%） | **pulse / 4000 / —** | `appearance.effects.glowPulse` |
| `scanBeam` | 走査線の帯 | 盤面の面の上（`AmbientRoot` の `ScanBeamLayer`。行・タブ・ステータス行の上、光の玉の下。面の八角形で切り抜く） | 同上（常時） | none / **beam**（高さ 60 px、主色の縦グラデーションの帯 1 本） | **beam / 4000 / —**（**既定 ON**） | `appearance.effects.scanBeam` |

## glitch の詳細

一瞬だけ、盤面の**静止画**を使って「横ずれ（スライス）+ 色にじみ（RGB 分離）」を見せ、表示なら終わったら本物の盤面を静止表示し、非表示なら終わったらウィンドウを隠す。常時動く演出ではない。
**グリッチの半分の時間・一定速度（linear）のフェードを重ねる**: 表示は前半で `Root.Opacity` 0 → 1（後半は不透明のままブレる）、非表示は後半で 1 → 0（前半はくっきりブレる）。`Root` の `Transitions` に `DoubleTransition`（`Opacity`、Duration = glitch の時間 / 2、非表示は Delay = 時間 / 2、`LinearEasing`）を置いて値を切り替える方式で、静止画層と `Frame` の両方にまとめて掛かる。

| パラメータ | 既定値 | 備考 |
|---|---|---|
| 時間 | **250 ms**（`durationMs`） | 0 なら「なし」。表示遅延の要件には含めない |
| ステップ | 3 回（0 / 約 83 / 約 167 ms で引き直し）→ 250 ms で静止 | 時間を変えると均等に割り直す（時間 ÷ 3。150 ms のときは 0 / 50 / 100 ms） |
| RGB 分離 | 本体の下に **主色の影を -5 px、副色の影を +5 px**（横方向）。影の不透明度 0.55 | 色は `FlAccent` / `FlAccent2`（配色の主色・副色に追従。SPEC §3.6「配色」）。**ずれ・不透明度とも強さの倍率を掛ける**（下の「強さの倍率」。値は 100% のとき） |
| 横ずれ | 本体を**横 3 本の帯**に切り、各帯を **±6〜16 px** 横へずらす。帯の高さは盤面の 6〜18%、位置とずれ量はステップごとに乱数 | 最後のステップ（3 回目。250 ms なら約 167 ms）は帯 1 本・**±6 px** に収束させる。**ずれ量（±6〜16、最後の ±6）に強さの倍率を掛ける**。帯の本数・高さ・位置は倍率で変えない |
| 強さの倍率 | **100%**（`appearance.glitchIntensity`、0〜200%、10 刻み） | 表示・非表示・設定画面 / ダイアログで共通の 1 値。下の「強さの倍率」 |
| 重ねるフェード | **グリッチの半分の時間**（既定 250 ms なら 125 ms。150 ms なら 75 ms）、`linear`。表示 = 前半（0〜時間/2）で `Root.Opacity` 0 → 1、後半は不透明のままブレる。非表示 = 後半（時間/2〜時間）で 1 → 0、前半はくっきりブレる（前半で消すと後半のブレが見えないため） | `DoubleTransition`（Duration = 時間/2。非表示は Delay = 時間/2）。フェードは `Root`、不透明度設定は `Window.Opacity` と層が違うので掛け合わせになる。変えたければユーザーから言う |
| イージング | 使わない（横ずれは段階的に切り替わるステップ関数、フェードは常に linear）。設定画面ではイージング欄を無効表示 | 保存値は `linear` 固定 |
| 乱数 | `Random.Shared`。テストでは種を固定できるようにする | |

**強さの倍率**（設定は SETTINGS.md 演出タブ `appearance.glitchIntensity`。・Mac で）

- 倍率 `k = glitchIntensity / 100`（0.0〜2.0。値は読み込み時に 0〜200・10 刻みへ丸め済み）。
- 掛ける値（100% の値 → k 倍）:

| 値 | 100%（基準） | 式 | 0% | 50% | 200% |
|---|---|---|---|---|---|
| 影のずれ `ShadowOffset` | 5 px | `5 × k` | 0 | 2.5 | 10 |
| 影の不透明度 `ShadowOpacity` | 0.55 | `min(1, 0.55 × k)` | 0 | 0.275 | 1（上限で止まる） |
| 帯のずれ `BandShiftMin`〜`BandShiftMax` | 6〜16 px | `6k`〜`16k` | 0 | 3〜8 | 12〜32 |
| 最後の段のずれ `FinalShift` | 6 px | `6 × k` | 0 | 3 | 12 |

- **変えないもの**: 時間（`durationMs`）・ステップ数・帯の本数（3、最後は 1）・帯の高さ（盤面の 6〜18%）と位置・重ねるフェード（時間の半分、linear）・影の色。乱数の引き方（呼ぶ回数と順）も変えない = 種を固定したテストは倍率 100% で従来と同じ帯の矩形・ずれになる。
- **影の不透明度も比例させる理由**: ずれだけを比例させると 0% で「ずれ 0・不透明度 0.55 の影」が本体の真下に残り、面が半透明のところ（背景画像の不透明度を下げたとき、SPEC §3.7）で主色・副色が濃く透けて見える。比例させると 0% で影が消え、50% は「にじみも薄い」になって、ユーザーの言う「強度」の感覚に合う。上限 1 は不透明度の定義域。
- **0% = 「ずれも色にじみも無いグリッチ」= 静止画にフェードだけ**: 0 を下限にする。理由: 盤面は種類を fade にしても同じに近いが、**設定画面・ダイアログ（`ChromeWindow`）は種類が glitch のときしか演出しない**（下の 13.）ので、「窓もふわっと出入りさせたいがブレは要らない」を 0% で選べる。時間・非表示の待ち（既定 250 ms）は glitch のまま。実装は 0% でも同じ経路でよい（影の不透明度 0・ずれ 0 は見た目に出ない）が、**`GlitchLook.IsStill`（ずれ 0）のときは影 2 枚と帯 3 本の子を作らない**（本体の静止画 1 枚だけ）と軽い。静止画は撮る（フェード中に `Frame` ではなく静止画を見せる手順は同じ）。
- **200% を上限にする理由**: 帯の最大ずれ 32 px・影 10 px が盤面の窓の余白 40（SPEC §3.6「発光のウィンドウ余白」）に収まり、ずれた帯が窓の端で切れない。設定画面・ダイアログも同じ `FrameChrome` の余白（`GetMargin()`）。余白を将来 32 未満にするときはこの上限を見直す。
- 適用範囲: グリッチを使うところすべて（盤面の表示・非表示、設定画面・ダイアログの出入り（13.））。表示と非表示で別の値は持たない。グリッチ以外の種類（fade / zoom / slide）と静止画が撮れないときの代替フェード（7.）には関係しない。
- 反映: `SettingsChange.Effects` で `BoardWindow.ApplyEffects` と `App.ApplyWindowEffects` が読み直す。**次に始まるグリッチから効く**（再生中のものは開始時の値のまま）。「アニメーション」OFF・OS の「視差効果を減らす」のときはグリッチ自体が掛からないので倍率は無関係。
- コードの置き場所: 定数と式は Core `Effects/GlitchLook.cs`（`readonly record struct GlitchLook(double ShadowOffset, double ShadowOpacity, double BandShiftMin, double BandShiftMax, double FinalShift)`、`Base`（100%）、`Scaled(int intensityPercent)`、`IsStill`、範囲の定数 `MinIntensity = 0` / `MaxIntensity = 200` / `IntensityStep = 10` / `DefaultIntensity = 100`）。App の `GlitchPlayer` は自分の定数（`ShadowOffset` / `ShadowOpacity` と `Shuffle` の 6 / 10 / 6）を消し、`Start` の引数で受け取った `GlitchLook` を使う。**グリッチの見た目の数値は `GlitchLook` 以外に書かない**（八角形の `Octagon` と同じ規則）。

**実装方式（Avalonia）**

1. `BoardWindow` の `Frame` を `Root`（`Grid`、`Margin = FlGlowMargin`）で包み、`Frame` の上に `GlitchLayer`（`Canvas`、`IsHitTestVisible = false`、普段は `IsVisible = false`）を重ねる。
2. `PopupController.Show` は今までどおり位置決め → `PrepareShow()` → `Show()` → 前面化 → `PlayShowAsync()`。種類が `glitch` のとき `PrepareShow` は `Root.Opacity = 0`（`Frame` は不透明のまま）にしておく。
3. （表示 = In）`PlayShowAsync` は**同期的に**（最初のフレームが描かれる前に）: `Frame.Bounds` から `RenderTargetBitmap`（`PixelSize = Bounds × RenderScaling`、DPI = 96 × RenderScaling）を作り `Render(Frame)` で盤面の静止画を取る（`Root.Opacity` は `Frame` 自身の描画に影響しない。`Window.Show()` は初回レイアウトを同期的に行うので `Show()` 直後に `Bounds` が決まっている想定。決まっていなければ最初の `LayoutUpdated` で行い、1 フレームの遅れを許容） → `GlitchLayer` に次を置く: 主色の影（`Border`、`Background = FlAccent`、`OpacityMask = ImageBrush(静止画)`、`RenderTransform = translateX(-5)`、`Opacity 0.55`）、副色の影（同、+5 px）、本体（`Image`、静止画）、帯 ×3 → `Frame.Opacity = 0`、`GlitchLayer.IsVisible = true`、`Root.Opacity = 1`（`DoubleTransition`（Duration = 時間/2）により 0 → 1 のフェードインが始まり、時間/2 で不透明になる）。ここまでが最初のフレーム。
4. `Task.Delay(時間 ÷ 3)` ごとに帯の位置とずれ量を引き直し（2 回）、時間が来たら `GlitchLayer.IsVisible = false`、子を破棄（`RenderTargetBitmap.Dispose`）、`Frame.Opacity = 1`（フェードインは時間/2 で済んでいるので、後半は不透明のままブレて静止する）。
4b. （非表示 = Out）`PlayHideAsync` で種類が `glitch` のとき: 盤面はまだ見えているので同じ手順で静止画を撮り `GlitchLayer` を組んで `Frame.Opacity = 0`・`GlitchLayer.IsVisible = true` にし、同時に `Root.Opacity = 0` を設定する（`DoubleTransition` の Delay = 時間/2、Duration = 時間/2 なので、前半はくっきりブレ、後半に 1 → 0 のフェードアウトが掛かる）。以後は In と同じ間隔で帯を引き直し、時間が来たら `GlitchLayer` を消して `PlayHideAsync` を完了させる → `PopupController.FinishHideAsync` が `Hide()` を呼ぶ。入力の受付停止とフォーカス返却は方針どおり演出の前に済んでいる。次の表示のために `ResetFrame()` / `PrepareShow()` が `Root.Opacity` と `Frame.Opacity` を表示演出の開始状態に戻す。
5. 影の色付けは `OpacityMask` 方式（静止画のアルファ形状をアクセント色で塗る）。本物のチャンネル分離（R / GB の抜き出し）ではないが、盤面の面がほぼ不透明なので縁と帯のずれにシアン / マゼンタのにじみとして見える。物足りなければ `Bitmap.CopyPixels` でチャンネルを落とした `WriteableBitmap` を 2 枚作る方式へ差し替える（1 枚 1 MB 以下、数 ms）。
6. 打ち切り（演出中に `Hide` / 再 `Show`）: `CancellationToken` で中断し `ResetFrame()`（`GlitchLayer` を消し `Frame.Opacity = 1`）。
7. フォールバック: `RenderTargetBitmap` が作れない（描画バックエンドが無い・失敗）か、`ActualTransparencyLevel` が透過でないときは**表示 = `fade / 100 / easeOut`、非表示 = `fade / 100 / easeIn`** で代替し、Debug ログに 1 行（代替のフェードは glitch の時間（既定 250 ms）に合わせず短いままでよい）。
8. **表示遅延との関係**: 静止画の生成は数 msを見込む。これは最初のフレームの前に入るので表示遅延（SPEC §11「表示まで n ms」）に含まれる。Debug ログに「スナップショット n ms」を出し、10 ms を超えるなら静止画の解像度を 1/2 に落とす（ぼけるが一瞬なので問題ない）。
9. 演出中も `Frame` は `Opacity 0` のまま当たり判定を持つ（Avalonia のヒットテストは透明度を見ない）ので、グリッチ中のクリック・キー操作は今までどおり効く。不透明度設定（`Window.Opacity`）は静止画層にも等しく掛かる。
10. 打ち切りの対称性: 非表示グリッチ中にトリガーで再表示されたら `CancelHideAnimation` → `ResetFrame()` で `GlitchLayer` を消し、`Root.Opacity` / `Frame.Opacity` を 1 に戻してから表示演出に入る（`Transitions` が途中値から 1 へ滑らかに戻るのは許容）。
11. グループ切替（`groupSwitch`）への `glitch` 適用は v1 ではしない。
12. 常駐モードは持たない（SPEC §12）。グリッチは表示・非表示と設定画面・ダイアログの出入りだけで使う。
13. **設定画面・ダイアログ（`ChromeWindow`）での使い方**（SPEC §3.8「演出」）: `boardShow` / `boardHide` が glitch のときだけ（他の種類なら演出なし。窓専用の ID は作らない）。
    - 部品は盤面と同じ `GlitchPlayer`。`ChromeWindow` が `FrameChrome.Overlays` に自分の `GlitchLayer` を持ち、`Start(chrome, chrome.Frame, layer, spec, FlAccent, FlAccent2, …)` を呼ぶ（`Root` に当たるのは `FrameChrome` 自身）。影の色・帯・ステップ・フェードの半分時間は盤面と同じ値。強さの倍率も盤面と同じ値（`ChromeWindow.GlitchLook`、static）。
    - 表示: コンストラクタで `Chrome.Opacity = 0` にしておき（最初のフレームで素の窓が見えないように）、`Opened` の後 `DispatcherPriority.Loaded` で `Start(appearing: true)`。静止画が撮れなければ `Opacity = 1` にしてそのまま出す（**窓では fade の代替は入れない**。盤面の 7. とは違う）。
    - 非表示: `Closing` で `e.Cancel = true` → `IsHitTestVisible = false` → `Start(appearing: false)` を待つ → `Close(result)` で閉じ直す。`result` は `Close(object)` に渡されたダイアログ結果（Avalonia の `Window._dialogResult` をリフレクションで読む。SPEC §10.5）。2 回目の `Closing` は `_hideGlitchDone` で素通し。`CloseReason` が `ApplicationShutdown` / `OSShutdown` なら演出しない。
    - 設定の反映: `ChromeWindow.ShowEffect` / `HideEffect`（static）に `App.ApplyWindowEffects` が起動時と `SettingsChange.Effects` のたびに解決値（`EffectCatalog.Resolve`。`appearance.animation` OFF・`PrefersReducedMotion` を含む）を入れる。同じところで `ChromeWindow.GlitchLook`（static、既定 `GlitchLook.Base`）に `GlitchLook.Scaled(appearance.GlitchIntensity)` を入れる。開いている窓には次の閉じるときから効く。既定は `EffectSpec.None`（UI テストでは動かない。グリッチを試すテストは自分で入れて `finally` で戻す）。

## glow の詳細（行のホバー・タブ・貼り付け）

- `highlight`（背景・枠の `BrushTransition`）に加えて `BoxShadow` を `BoxShadowsTransition` で変化させる。対象は `Button.row` / `ToggleButton.tab` のテンプレート内 `ContentPresenter`（`BoxShadow` プロパティを持つ）。時間・イージングは `highlight` と同じ値を使う。
- スタイルは「種類が `glow` のとき対象に `.glow` クラスを付ける」方式: `Button.row.glow:pointerover` と `Button.row.glow.selected`（↑↓ の選択）`/template/ ContentPresenter#PART_ContentPresenter { BoxShadow = {DynamicResource FlRowHoverGlow} }`、`ToggleButton.tab.glow:checked …{ FlTabSelectedGlow }`、`Button.row.glow.pasting …{ FlPasteGlow }`（`pasteFlash`）。`highlight` のときはクラスを付けないので発光しない。発光の色・ぼかし量はトークン（SPEC §3.6。主色・副色に追従）。
- 発光が `Frame` の外へはみ出さないように `Frame` 内側の `Padding`（6 px）で足りる範囲。
- `rowPress` / `previewShow` には `glow` を足さない。

## 常時の演出の詳細

**共通の規則**

| 項目 | 規則 |
|---|---|
| 動く条件 | 種類が `none` 以外 **かつ** `appearance.animation` ON **かつ** 盤面が見えている（`IsVisible`）**かつ** 表示 / 非表示の演出（グリッチ・フェード）中でない **かつ** OS の「視差効果を減らす」がオフ（`IWindowService.PrefersReducedMotion`、SPEC §10.3。 採用）。1 つでも外れたら止める（`BoardWindow.UpdateAmbient`: `AmbientSuspended` / 演出中カウンタ / `ReducedMotion` デリゲートで判定）。**描き手（下の行）によらず同じ 1 か所で判定する** |
| 描き方（OS 別） | **Windows**: Avalonia の層（`AmbientRoot` の `PulseLayer` → `ScanBeamLayer` → `OrbLayer`）を `AmbientAnimator` が毎フレーム進める（下の「時計」）。**macOS**: Platform `IAmbientLayerService`（SPEC §10.3）が盤面の `contentView.layer` に足した **Core Animation の層**で描き、アニメーションは OS（render server）が進める = アプリは毎フレーム何もせず、盤面本体は静止する（合成の頻度は **30 fps に上限**。下の「時計」の行）。App の `NativeAmbient` が `UpdateAmbient` の `run` を `SetRunning(true / false)`（true = 表示 + 下の「始まり」の 250 ms フェードイン + 止めた位置から再開、false = 下の「止まり」どおり即座に非表示 + 時間停止）に、`ApplyEffects` の解決値を `SetOrbs` / `SetPulse` / `SetBeam` に、`Frame.SizeChanged` / `ScalingChanged` を `SetGeometry`（作り直しても位相は保つ）に、配色の切替（`ActualThemeVariantChanged`）を色と明滅の画像の渡し直しに、`ApplyOpacity` を `SetOpacity`（`Window.Opacity` は層に掛からない）に写す。ホストがあるとき Avalonia の 3 層は何も描かず `AmbientAnimator` は始めない。見た目の定数（玉の半径・尾・帯のグラデーション・明滅の強さ）と数式（枠線の中心線の八角形・周長・CA の `timeOffset`・明滅のキーフレーム・帯の移動量）は Core `AmbientLook` / `AmbientLayout` にあり、両方の描き手が同じ値を使う。CA の層は Avalonia が描くすべてのものの上に載る（右クリックメニュー・ツールチップ・トースト・ダイアログは別ウィンドウなので従来どおり上。盤面内 D&D のゴーストの上を帯・玉が通るのは許容）。取り付けに失敗したら Avalonia の層に戻る。起動引数 `--ambient avalonia` で macOS でも Avalonia の層にできる（計測・切り分け用。設定項目ではない）。 |
| 始まり | `PlayShowAsync` の完了後（表示演出なしなら `Show()` の直後）。2 層を包む `AmbientRoot`（`Grid`）が **250 ms のフェードイン**（`DoubleTransition`、`Opacity` 0 → 1。`PulseLayer` 自身の `Opacity` は明滅の値なので包む側で行う）で現れる（突然出るのを避ける。表示遅延には含めない） |
| 止まり | `PlayHideAsync` の開始時・`Hide()` で**即座に**非表示（フェードアウトなし。非表示演出の邪魔をしない）。止まっている間はフレーム要求を出さない（CPU 0） |
| 時計 | macOS は Core Animation の `preferredFrameRateRange`（min 15 / max 30 / preferred 30）で合成頻度を制限する。Windows は `AmbientAnimator` が 3 層を駆動し、VSync 1 / 2 / 3 に対して最短間隔 13 / 30 / 47 ms を用いる | 停止中はフレームを要求しない。更新の世代番号で古いコールバックを破棄する |
| 計算 | OS 非依存の数式は Core `Effects/OrbPath.cs`（八角形 / 角丸矩形の周長と「周長上の距離 → 座標」。形は Core `Theming/Octagon`）と `Effects/AmbientMath.cs`（周期と経過時間 → 0〜1 の位相、サイン波の明るさ）に置き、単体テストする。App は描くだけ |
| 色 | テーマのトークンを**毎フレーム引く**（`FlAccent` / `FlAccent2` の `Color`。辞書の参照 1 回で軽い）。色が変わったときだけブラシを作り直す（基調色の切替に追従。`ApplyEffects` 経由の配線を増やさない） |
| グリッチとの関係 | `GlitchPlayer` は `Frame` の静止画を撮り、`OrbLayer` / `PulseLayer` / `ScanBeamLayer` は `Frame` の外（`Overlays`）にあるので**静止画に含まれない**。グリッチ中は止めているので二重にも見えない。**帯を `Frame` の中（`Body`）に置かないのはこのため**（置くと `DuringTransition` が `_transitions` を増やす前に `GlitchPlayer.Start` が静止画を撮る順序になっていて、帯が静止画に写る） |
| 背景画像・不透明度 | 光の玉・帯は画像・覆いの上に描かれる。`Window.Opacity`（不透明度設定）はどの層にも等しく掛かる |
| 設定画面での表示 | 「演出」タブにアニメーション全体の ON/OFF、常時の演出 3 行、Windows の VSync と CPU 注記、折りたたみ「詳細設定」、すべて既定に戻す、静止した走査線の設定を並べる | 常時の演出は種類・時間（秒）・既定に戻すを表示し、イージング欄は出さない。macOS は VSync と CPU 注記を出さない |
| 対象にしないもの（v1） | **文字の発光**（`FlTextGlow` は `DropShadowEffect` = 要素ごとのオフスクリーン描画。毎フレーム変えると文字の数だけラスタライズし直すので重い）、**タブ・選択セル・ホバーの発光**（切替・ホバーのたびに生成 / 破棄される要素なので層を重ねにくく、動きもうるさい）、**トースト**（一瞬出るだけ）、**設定画面・ダイアログの枠**、**ヘッダーの区切り線・タブ下線の上を走る玉**（下の「内側の候補」。枠の玉を見てから決める） |

**`frameOrb`（光の玉）**

| パラメータ | 既定値（仮。実物を見て調整） | 備考 |
|---|---|---|
| 経路 | `Frame` の**枠線の中心線**。面取り（SPEC §3.9 H8、既定 10）のときは**八角形**、`Chamfer = 0`（テスト用）なら従来の角丸矩形（角 `FlBoardCornerRadius` − t/2）。**時計回り**、始点は上辺の始まり（左上の斜辺の終わり）、周長に対して**一定速度**（角でも減速しない） | `OrbPath.Octagon(w, h, c)`: 周長 = 2(w + h) − 8c + 4c√2。4 辺 + 4 斜辺の 8 区間を距離で引く。`OrbPath.RoundedRect(w, h, r)`: 周長 = 2(w + h) − 8r + 2πr（4 辺 + 4 円弧）。盤面の大きさ・面取りが変わったら経路を作り直す |
| 時間 | **8000 ms で 1 周**（2000〜20000、500 刻み。既定の種類が `none` なのでスライダーの初期位置として使う） | 420×330 の盤面なら周長約 1500 px → 約 190 px/s。ゆっくりが上品 |
| 数 | `orb` = 1 個（主色）、`orbTwin` = 2 個（主色 + 副色。位相が半周ずれて対角を走る） | 既定は `orb`。副色の玉は角ブラケットの 2 色使いと揃う |
| 玉 | 芯: 半径 **2.5 px**、色 = 主色を白と 60% 混ぜた色。暈（かさ）: 半径 **9 px** の `RadialGradientBrush`（中心 = 主色 90% → 縁 = 透明） | `DrawingContext.DrawEllipse` 2 回。暈は枠の外側の余白にも、内側の面の上にもはみ出してよい（`Overlays.ClipToBounds = false`） |
| 尾（トレイル） | 玉の後ろ **48 px** ぶんの経路上に **10 点**。半径 7 → 3 px、不透明度 0.6 → 0 に線形に減らした暈だけ（芯なし） | 1 フレームの描画は玉 1 個あたり 12 回の `DrawEllipse`。2 個でも 24 回 |
| macOS（CA） | 玉 1 個 = 12 層（尾 10 = `CAGradientLayer` radial、暈 1 = 同、芯 1 = `CALayer` + `cornerRadius`）に同じ `CAKeyframeAnimation`（`keyPath position`、`path` = Core `AmbientLayout.OrbCenterline`（`OrbInset` = 面取りありなら枠の太さ × 1.5 = `OrbLayer.PathFor` と同じ規則）の八角形をなぞった `CGPath`、`calculationMode paced` = 周長に対して一定速度、`repeatCount ∞`、`duration` = 時間）。尾は同じアニメーションの `timeOffset` を遅れ（48 × i / 10 px ぶんの時間 = `AmbientLayout.OrbTimeOffsetMs`）だけずらして追従、2 個目は `timeOffset` = 半周。値（半径・不透明度・色の混ぜ方）は Core `AmbientLook` から = Avalonia の `OrbSprite` と同じ。 |
| 層 | `AmbientRoot` の `PulseLayer` → `ScanBeamLayer` → `OrbLayer` の順で描く。玉は `OrbSprite` の位置と尾の相対座標を更新する | `AmbientRoot` は `GlitchLayer` と `DragLayer` より下。2 個目は `orbTwin` のときだけ表示 |

**`glowPulse`（発光の明滅）**

| パラメータ | 既定値（仮） | 備考 |
|---|---|---|
| 作り方 | 静的な枠の発光を残し、その上に同じ `NeonGlowPlan` を焼いた画像を重ねる。大きさ・拡大率・配色・面取り・余白の変更で焼き直す | Windows は `PulseLayer : Canvas` の子 `PulseStrip` 4 本が上下左右の帯を `PushOpacity(Level)` で描く。帯の境界はデバイス px に丸める。`Level` は 2% 以上の変化または 0 との切替で更新 |
| macOS（CA） | `PulseLayer` が焼いた画像（上の「作り方」。枠 + 余白 40、デバイス px）を **PNG にして Platform に渡し**（`Bitmap.Save` → `EncodedImage`。焼き直しのときだけ）、Platform が ImageIO で `CGImage` にして `CALayer.contents`（`contentsScale` = 拡大率、`Nearest`、枠は画像のピクセル数 ÷ 拡大率 = 等倍）に入れ、`opacity` を `CAKeyframeAnimation`（値 = Core `AmbientLook.PulseKeyframes(peak, 33)` = 下の波形を 33 点で標本化、`linear`、`duration` = 周期、`repeatCount ∞`）で揺らす。4 本の帯への分割・2% 刻みの更新は要らない（合成は OS）。画像は上下対称なので層の `geometryFlipped` で内容が反転しても見た目は同じ |
| 波形 | サイン: `Level(t) = peak × (1 − cos(2π t / T)) / 2`。位相 0（今の明るさ）から始まる | `AmbientMath.PulseLevel(elapsedMs, periodMs, peak)` |
| 強さ | `pulse` = peak **0.45**、`pulseStrong` = peak **0.9** | 「柔らかい」が要望なので既定は弱。下限は今の明るさ（暗くはならない。暗くもしたい場合は `Frame` 側の発光を弱い版に差し替える案になるが、OFF 時の見た目が変わるので採らない） |
| 時間 | **4000 ms で 1 周期**（1000〜10000、250 刻み。既定の種類が `none` なのでスライダーの初期位置として使う） | 呼吸の速さ。`frameOrb` とは独立（同期させない。周期が違うので重なりが単調にならない） |
| 同期 | 対象が増えたら（将来）同じ `AmbientAnimator` の時計を使い**全体で同じ位相**にする | ずらすと落ち着かない |

**`scanBeam`（走査線の帯）**

| パラメータ | 値 | 備考 |
|---|---|---|
| 帯 | 高さ **60 px**（`ScanBeamLayer.BandHeight`、定数）、幅 = 面の幅。塗りは主色の縦 `LinearGradientBrush`: 上端 α 0 → **85% の位置で α 28/255（約 11%）** → 下端 **α 60/255（約 24%）**。下端が明るく、上へ尾を引く | ブラシは `FlAccent` が変わったときだけ作り直す（毎フレームの `Color` 比較は `OrbLayer.UpdateColors` と同じ） |
| 動き | 上から下へ**一定速度**。`y = −60 + phase × (面の高さ + 60)`、`phase = AmbientMath.Phase(elapsedMs, durationMs)`。帯の下端が面の上端に現れてから帯の上端が面の下端を抜けるまでが 1 周期で、途切れず繰り返す（1 周期の終わりで次の帯がすぐ上から来る） | 数式は Core `AmbientMath.SweepOffset(phase, faceHeight, bandHeight)` に置いて単体テスト。動かすのは子の `Rectangle` の `RenderTransform`（`TranslateTransform.Y`）だけ（層全体の `InvalidateVisual` はしない） |
| 時間 | **4000 ms で 1 回**（2000〜12000、500 刻み。既定値そのもの = `EffectCatalog` の `Default` は `beam / 4000 / linear`） | 330 px の盤面なら約 100 px/s |
| 既定 | **`beam / 4000`（既定 ON）** | `Normalize` は既定と同じ `beam / 4000` を捨て、`none` は残す（他の演出と同じ規則）。既定構成で動くので SPEC §11 の「表示中 3%」の計測構成は「常時の演出をすべて「なし」にした構成」に改めた |
| 層 | `ScanBeamLayer : Canvas`（`IsHitTestVisible = false`、**`ClipToBounds = true` + `Clip` = 面の八角形**）。`AmbientRoot` の中で `PulseLayer` の上・`OrbLayer` の下。**行・タブ・ステータス行の上**に乗る | 八角形は `FrameChrome.FaceClip`（中身の `Clip` = `Octagon(0,0,w−2t,h−2t,c)` を `Frame`（= `Overlays`）座標へ枠の太さ t だけ平行移動したもの。`Chamfer = 0` なら `FlBoardCornerRadius` の角丸矩形。`SizeChanged` で作り直し、`FaceClipChanged` を出す）。`Overlays.ClipToBounds = false` のままだが帯は自分の `Clip` で面に収まる |
| 幅・高さの追従 | `ScanBeamLayer.SizeChanged` で `Rectangle.Width` を面の幅に、`Advance` が面の高さを使う（`Bounds`）。盤面の行列数が変わっても作り直しは要らない | |
| macOS（CA） | 面の矩形（Core `AmbientLayout.FaceRect` = 枠の太さぶん内側）の `CALayer` に八角形の `CAShapeLayer` を `mask` として付け（`Octagon.Points(0, 0, W, H, Chamfer)` = `FrameChrome.FaceClip` と同じ形）、その中の `CAGradientLayer`（axial、上 → 下、色は Core `AmbientLook.BeamStops` = 上の表の 3 点を主色に、幅 = 面の幅、高さ 60）の `position.y` を `CABasicAnimation`（`AmbientLayout.BeamCenterY(0)` = −30 → `BeamCenterY(1)` = 面の高さ + 30、線形、`duration` = 時間、`repeatCount ∞`）で動かす。下端が明るいことを確かめる | |
| 色 | 主色だけ（副色は使わない） | 配色の切替に追従 |
| 駆動 | `AmbientAnimator.Tick` が `ScanBeamLayer.Advance(elapsedMs)` を呼ぶ。Avalonia の `Animation`は使わない | 理由: 動く条件・250 ms のフェードイン・停止中のフレーム要求なし・30 ms の下限・ヘッドレステスト（`Tick`）が玉・明滅と共通になる。`Animation` の `IterationCount.Infinite` は開始 / 停止と大きさ変更時の作り直しを別に管理することになる |

## 対象外

- 点滅するカーソルと常時のちらつきは用いない。
- 静的な枠の発光、背景画像、静止した走査線、不透明度の設定変更は演出の ON/OFF の対象外。
- ツールチップは Avalonia 標準。

## 操作に反応する装飾

- ターゲットロック: 行のホバー・選択時に、行の左端に主色の `▸` と左右の短い角ブラケットを出す。`rowHover` の有効状態・時間・イージングを使う。`none`・アニメーション OFF 時は `▸` だけ（動かさない）。入力は遮らない。
- 送信信号: `pasteFlash` の再生中、選んだ行の輪郭を主色の光点が左 → 右へ一度走る。`pasteFlash` と連動し時間は 80〜250 ms に制限。貼り付けそのものは待たせない（盤面を隠し始めるのと並行）。演出 OFF / none なら追加層なし。
- 回路パターン: 盤面四隅の静的な配線と接点。主色、グリッドの表示・濃さと連動（濃さはグリッドの 4 倍、上限 25%）。中央へ配線を広げず、入力を遮らない。タイマーなし。
- 工業製品風の刻印: 盤面の外側の発光余白に小さな CPCB / UNIT 刻印、目盛り、斜線。主色で薄く静的に描く。ステータス行の表示と連動し、狭い盤面（幅 220 px 未満）では省略。架空の通信状態は表示しない。
- グループスイープ: グループ切替時、一覧の領域を主色の細い境界と淡い尾が一度通る。番号の小さいほうへ戻ると右→左。`groupSwitch` の有効状態と時間（80〜250 ms）に連動。連続切替で再始動、通常再描画で停止。
- ホログラムの色ずれ: 投影面の縁の一部分に主色と副色を左右 0.8 px ずらして静的に描く。グリッドの表示に連動（濃さ 0 でも非表示）、不透明度 40%。文字は複製しない。タイマーなし。
