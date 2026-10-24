# CPClipboard 見た目の仕様（サイバーパンク）

見た目（色・形・発光・フォント・背景画像・フレームレスの窓・HUD）の仕様。SPEC.md の §3.6〜§3.9 はこのファイルを指す。
CPFileLauncher v1.1.0 の見た目をそのまま引き継いでおり、節の番号（§3.6〜§3.9）も合わせてある。本文中の「§3.6〜§3.9」はこのファイル、それ以外の「§n」は SPEC.md を指す。

- 盤面の中身が「アイコンのグリッド」から「1 行表示の文章の一覧」に変わったため、スロット系のトークンは行（`FlRow*`）に置き換え、ドロップ先・欠落アイテム・D&D のゴースト・アイテムの色帯は持たない。
- 実装の定数・式（`Octagon` / `NeonProfile` / `GlitchLook` / `AmbientLook` / `AmbientLayout`）は CPFileLauncher から持ち込んだものを正とし、この表と一致させる。

### 3.6 見た目「サイバーパンク」（専用化）

**色トークン**

| トークン | 用途 | 値（配色 = シアン） |
|---|---|---|
| `FlWindowBackground` | 設定・権限ガイド・通知・ダイアログの窓背景 | 主色の色相の黒（導出。旧手調整値 `#0B0F1A`） |
| `FlBoardBackground` | 盤面の面（`Frame`） | 同上、少し明るい（旧 `#0D1220`） |
| `FlBoardBorder` | 盤面の枠線 | 主色 80% |
| `FlSurface` | 盤面内の面（タブのホバー、全文のプレビュー、設定のカード） | 主色の色相の暗い面（旧 `#121A2B`） |
| `FlText` | 本文 | 主色の色相の冷たい白（旧 `#E6F7FF`） |
| `FlTextMuted` | 注記・未選択タブ・HUD の文字 | 主色の色相の青灰（旧 `#7FA6B8`） |
| `FlRowText` | 行の文字（定型文の 1 行表示） | 主色の色相の淡い色（旧 `#BFEFFF`） |
| `FlAccent` | 主色。枠・ホバー・選択タブ・情報トースト | `#00E5FF` |
| `FlAccent2` | 副色。角ブラケットの対角・グリッチの影の片方・`orbTwin` の 2 個目 | `#FF2BD6` |
| `FlAccentSoft` | 主色の薄い面（12%） | `#1F00E5FF` |
| `FlAccent2Soft` | 副色の薄い面（14%） | `#24FF2BD6` |
| `FlWarning` / `FlOnWarning` | 警告（読み取り専用の帯、「コピーだけ」になったトースト）とその上の文字 | `#FFC53D` / 窓背景 |
| `FlError` | エラー（エラートースト、権限未許可） | `#FF3B5C` |
| `FlSuccess` | 許可済み等 | `#2EE6A6` |
| `FlRowHoverBackground` / `FlRowHoverBorder` | 行のホバー | `#1F00E5FF` / `#00E5FF` |
| `FlEmptyGroupText` | 空のグループのタブの文字・空のグループの案内文 | 注記色 50% |
| `FlTabText` / `FlTabSelectedText` | グループタブの文字 | 注記色 / `#00E5FF` |
| `FlTabIndicator` | 選択タブの下線（2 px） | `#00E5FF` |
| `FlHeaderSeparator` | ヘッダーとグリッドの区切り線（1 px）。ステータス行（§3.9）の上の線も同じ | `#3300E5FF` |
| `FlToastBackground` / `FlToastBorder` / `FlToastText` | トースト | 窓背景 96% / `#9900E5FF` / 本文 |
| `FlToastErrorBackground` | エラートーストの背景 | `#F5160B14`（濃いワイン色寄りの黒） |
| `FlRowSelectedBorder` | キーボード（↑↓）で選んだ行の枠（SPEC §6.1） | `#00E5FF`（主色） |
| `FlHudText` / `FlHudGrid` / `FlHudScanline` | HUD（§3.9）: ステータス行・タグの文字 / 背景グリッドの線 / 静止した走査線（H14） | **主色 90%（`E6`）**/ **主色そのもの（アルファなし）** / 同じく主色そのもの。線の濃さはトークンではなく設定 `appearance.hud.gridOpacity`（既定 3%）/ `appearance.hud.scanlineOpacity`（既定 25%）を `GridLayer.Opacity` / `ScanlineLayer.Opacity` に掛けて決める |
| `FlTabOutline` | 設定画面のタブ（`TabItem`）の枠線 1 px（§3.9 H7。選択中は `FlAccent`） | 注記色 50%（`#807FA6B8` 相当） |

**形・線・発光**（非色トークン）

| トークン | 値 |
|---|---|
| `FlBoardCornerRadius` / `FlRowCornerRadius` | 2 / 2（ほぼ直角。`FlBoardCornerRadius` は面取り（下の `FlChamfer`）が 0 のときだけ枠に使う予備の値。行・× ボタンの角丸は `FlRowCornerRadius`） |
| `FlChamfer`（コードの定数 `FrameChrome.DefaultChamfer`） | **10**（§3.9 H8）。盤面と `ChromeWindow` の枠の四隅を 10 px の斜めカット（八角形）にする。面と枠線は八角形の `Path` で描き（Avalonia の `Border` は面取りを描けない）、中身（背景画像・覆い・グリッド・行）も同じ八角形で切り抜く。**発光も八角形に沿って描く**。トークン辞書には持たず（配色・設定で変えない）、`FrameChrome.Chamfer` に 0 を入れると従来の角丸矩形に戻る（テスト用） |
| `FlBoardBorderThickness` | **1.5**（ネオン管の枠に合わせて太く。盤面の大きさの計算もこの値を使う） |
| `FlCornerBrackets` | true: 盤面の外周 4 隅に角ブラケット（太さ 1.5 px、枠線から 2 px 外側）。**面取り（`FlChamfer` > 0）のときは斜辺に沿って折れた線**（縦の腕 6 px → 斜辺 10 px（面取りと同じ長さ、45°）→ 横の腕 6 px。八角形の斜辺の外側をなぞる形）。面取りが 0 のときは L 字（腕 10 px）。左上・右下 = `FlAccent`、右上・左下 = `FlAccent2`（2 色使い） |
| （発光の作り方） | **ネオン管方式** `AppTheme.Neon(色, near, mid, far, inner)`: 外側の光を **3 段**重ねる（`0 0 near 0` 不透明度 90% / `0 0 mid 0` 55% / `0 0 far 0` 30%）+ 任意で**内側の光**（`inset 0 0 18 0` 不透明度 inner。省略時なし）。単段のぼかしより管の芯が明るく裾が長く見える。**枠の発光だけはリング描画**: 盤面・窓の枠は八角形（`FlChamfer`）なので `BoxShadow` では描けない。同じ 3 段 + 内側の断面を Core `NeonProfile` で数値化し（σ = 0.288675 × blur + 0.5 = **Avalonia 11.3 の `BoxShadow` と同じ換算**のガウス。層ごとに `α × ½ erfc(d / (σ√2))`、層の合成は `1 − Π(1 − g)`）、App `NeonGlowLayer`（描き方は `NeonGlowPlan`）が「辺からの距離ごとにオフセットした八角形」で区切った **0.5 px 幅の帯**を、帯の中央の値で 1 回ずつ塗って再現する（帯は重ねない: 入れ子に重ねると外側の薄い部分が 8 bit の刻みで消える。 実装時に判明）（`Effect` も `BoxShadow` も使わない。普通の塗りなので `RenderTargetBitmap` にそのまま焼け、明滅 `PulseLayer` と描画コードを共有する = 同じ形）。トークンの型は `BoxShadows` のままで `Blur` / `Color` / `IsInset` だけ読む（`Spread` / オフセットは無視）。発光が消えきる距離は `NeonProfile.Extent`（合成アルファ × 明滅の最大倍率 1.9 が 0.5/255 を下回る距離。`far` 32 で約 27 px）で決まり、余白はそれより 2 px 以上大きいことをテストで保証する。角では本物のぼかしと違い辺と同じ明るさで回り込む（ネオン管が角でも均一に光る見え方。面取り 135° なので差は小さい）。**作り置き**: 盤面では `NeonGlowLayer` が `NeonGlowPlan.Draw` を**外側・内側それぞれ 1 枚の `RenderTargetBitmap` に焼いて `DrawImage` 1 回で貼る**（`FrameChrome.BakeGlow = true`。設定画面・ダイアログは焼かない）。理由: 明滅・玉・帯の常時の演出が毎フレーム枠全体を汚すので、静的なはずの約 80 帯の `DrawGeometry` も毎フレーム描き直されていた（Mac・scale 1・ソフトウェア描画で、静的な発光を消すと玉 2 個・明滅強・帯の構成が 34.4 → 26.5%）。焼く元は同じ `NeonGlowPlan` なので見た目は同じ（画素テストで保証）。余白は `NeonGlowPlan.OutwardExtent + 1` px（外側の層で約 28 px、内側は 1 px）、作り直しは大きさ・面取り・影の値（配色）・`RenderScaling` が変わったときだけ（`Refresh()` の中。`Render` では作らない）、鍵が合わないフレームはベクタで描いて次のフレームまでに焼く。メモリは盤面で scale 1 約 1.3 MB / scale 2 約 5 MB（§11 の内数） |
| `FlBoardGlow` | 主色 near 4 / mid 14 / far 32 / inner 15% |
| `FlRowHoverGlow` | 主色 3 / 10 / 20 / inner 10% |
| `FlTabSelectedGlow` | 主色 3 / 8 / 16（内側なし） |
| `FlPasteGlow` | 主色 4 / 14 / 28 / inner 20%（`pasteFlash` = glow の一瞬の強まり） |
| `FlRowSelectedGlow` | 主色 3 / 10 / 20（= `FlRowHoverGlow` と同じ値。キーボードで選んだ行） |
| `FlToastGlow` / `FlToastErrorGlow` | 主色 3 / 12 / 26 / `#FF3B5C` 3 / 12 / 26（いずれも内側なし） |
| `FlGlowMargin` / `FlGlowMarginThickness` | **40**。発光がウィンドウの縁で切れないよう、盤面ウィンドウの内容の外側に取る透明な余白。規則: **`NeonProfile.Extent(FlBoardGlow の外側 3 層, gain 1.9)` + 2 以上**（`far` 32 で約 27 + 2 = 29）に、Retina の丸めと `far` を少し太らせる余地として 8 px の余裕を足した値。リング描画は `Extent` より外に何も描かないので、窓の端で発光が切れることは構造的に起きない。`Thickness` 形も同じ値。ウィンドウは片側 6 px ずつ大きくなった（ポップアップの位置補正・盤面外クリック判定・`resident.bounds` はウィンドウ矩形のままなので自動） |
| `FlToastMargin` / `FlToastMarginThickness` | **34** |
| `FlChromeShadow` | フレームレスの設定画面・ダイアログ（§3.8）の枠の外側の影 = `FlBoardGlow` と同じネオン管（主色 4 / 14 / 32 / 内側 15%。八角形に沿うリング描画） |
| `FlChromeMargin` / `FlChromeMarginThickness` | **40** |
| `FlTextGlow` / `FlTextGlowSoft` | 文字のネオン発光（CP2077 のメニュー見出し風）。`DropShadowEffect` オフセット 0、強 = ぼかし 10・主色 90%（`E6`）、弱 = ぼかし 6・主色 50%（`80`）。主色に追従。付ける所は下の「各部の見た目」のとおりで、**行の文字には付けない**（`Effect` は要素ごとにオフスクリーン描画になり、行数ぶん重くなる。`BoxShadow` と違い枚数が効く） |
| `FlFontFamily` / `FlMonoFontFamily` | 同梱フォント（下記）。等幅は HUD のコード（タグ・トーストの状態コード・色の hex。§3.9）だけに使う。**ステータス行は `FlFontFamily`（Chakra Petch）SemiBold 11 px**（下の「フォント」） |
| `FlHudHeight` | ステータス行（§3.9）の文字域の高さ **18**。区切り線 1 px と上の余白 2 px を足した盤面の高さへの加算は **21**（`BoardWindow.StatusBarHeight`） |

- 発光は Avalonia の `BoxShadow`（`Border` / `ContentPresenter`）で描く。ネオン管方式は `BoxShadows` に 3〜4 枚を並べるだけ（枚数が増えても同じ要素の 1 回の描画で済む）。ぼかしは `BoxShadow` の blur で、`DropShadowEffect`（`Effect`）は使わない（コンテンツ全体のラスタライズが要り、ホバーのたびに重い）。**例外は盤面・窓の枠の発光**（八角形。上の「枠の発光だけはリング描画」）: `BoxShadow` も `Effect` も使わず、同じ断面をオフセット八角形の塗りで描く。`Effect` を使わない理由はもう 1 つあり、`RenderTargetBitmap.Render`（`ImmediateRenderer`）は `Visual.Effect` を適用しないので、明滅 `PulseLayer` に焼けない（Avalonia 11.3 のソースで確認）。
- **透過ウィンドウ上の発光の余白**: 盤面は背景透過（§3.1）なので、`Frame` の外側に `FlGlowMargin` 分の透明な余白を持つ `Root` を置き、ウィンドウの大きさは「盤面 + 余白 × 2」にする。余白を含めたウィンドウ矩形を「盤面」とみなし（§3.3 の盤面外クリック判定、ポップアップの表示位置補正、`resident.bounds` はすべてウィンドウ矩形のまま。余白のクリックは何もしない）、`popup.x/y` と `resident.bounds` もウィンドウ左上のまま。余白は常に 40。
- 不透明度設定（`Window.Opacity`）は発光・グリッチ層にも等しく掛かる（同じウィンドウの中なので自動）。

**フォント**

- 角張った欧文書体 **Chakra Petch**（Cadson Demak、SIL Open Font License 1.1、Google Fonts で配布）の Regular と SemiBold を `src/CPClipboard.Desktop/Resources/Fonts/` に同梱し（`avares://CPClipboard/Resources/Fonts#Chakra Petch`。**URI の authority はアセンブリ名**なので、製品名の変更でアセンブリ名を `CPFileLauncher` にしたら URI も変える、§10.4。TTF 各 100 KB 前後）、OFL の本文 `OFL-ChakraPetch.txt` を同じフォルダに置く。OFL は同梱・再配布可、販売不可、改変時は名前を変える（Reserved Font Name）義務があるだけで、商用アプリへの埋め込みに制約はない。代替候補: Rajdhani（OFL、細身で背が高い）。
- **等幅書体 Share Tech Mono**（Carrois Apostrophe、OFL 1.1、Regular 1 ウェイト約 45 KB）を **HUD のコードだけ**に使う（§3.9: ダイアログのタグ、トーストの状態コード、設定の色の hex 表示。トークン `FlMonoFontFamily`）。**行・本文には使わない**（グループタブの番号だけは例外）。**ステータス行（`GRP 2/05` / `ITEMS 24` / 日時）には使わない**。OFL 本文 `OFL-ShareTechMono.txt` を同じフォルダに置き、「CPClipboard について」のライセンス表記に加える。
- **日本語は同梱しない**。日本語の角張った OFL 書体で小さいものは無く（3〜8 MB）、ピクセル書体（DotGothic16）は 11 px のラベルで読めないため。日本語の文字は Avalonia のフォントフォールバックで OS の標準ゴシック（Windows: Yu Gothic UI、macOS: ヒラギノ角ゴ）に落ちる。これは現在 Inter で日本語が表示できているのと同じ経路で、追加の設定は要らない。欧文が角張った書体・和文がシステムゴシックの混植になるが許容する。色・発光は和文にも同じに掛かる。
- フォントの指定はウィンドウ（`BoardWindow` / `SettingsWindow` / トースト / 権限ガイド / 通知 / ダイアログ）の `FontFamily = {DynamicResource FlFontFamily}` で行い、子に継承させる。文字サイズは行 = `appearance.fontSize`（既定 13）、タブ 12、ステータス行 11、本文 Fluent 既定。Win / Mac の扱いは同じ（埋め込みリソースから `FontManager` が読む。`WithInterFont` と同じ仕組み）。

**各部の見た目**

| 部位 | 見た目 |
|---|---|
| 盤面の枠 | `FlBoardBackground` の面、1.5 px `FlBoardBorder`（`FlBoardBorderThickness`）、**四隅を 10 px 面取りした八角形**（`FlChamfer`。面・枠線・中身の切り抜きが同じ形）、外側に `FlBoardGlow`（ネオン管 3 段。**八角形に沿う**リング描画）、枠線のすぐ内側に内側の光（15%。面の上・中身の下）、4 隅に斜辺に沿った角ブラケット。ヘッダー下に 1 px `FlHeaderSeparator`。グリッドの下に**ステータス行**（§3.9。既定 ON、上に 1 px `FlHeaderSeparator`）。演出 `frameOrb` を選ぶと、盤面が見えている間、枠線の上を**光の玉**（主色、8 秒で 1 周、尾付き。八角形に沿い、角では斜辺の上を通る）が周回する。演出 `glowPulse` を選ぶと枠の発光が **4 秒周期で柔らかく明滅**する（今の明るさを下限に上へ膨らむ）。**どちらも既定 ON**。`spec/EFFECTS.md`「常時の演出の詳細」 |
| グループタブ | 0〜9 の 10 個を常に出す（SPEC §3.2）。文字 12 px `FlMonoFontFamily`（番号）+ 名前があれば `FlFontFamily` で番号の右に（最大 6 文字、超えたら `…`）。角丸なし、背景なし。未選択 = `FlTabText` + `FlTextGlowSoft`、空のグループ = `FlEmptyGroupText`（発光なし）、ホバー = 文字 `FlText` + 背景 `FlAccentSoft`、選択 = 文字 `FlTabSelectedText` + `FlTextGlow` + 下線 2 px `FlTabIndicator` + `FlTabSelectedGlow`（演出 `tabHighlight` = `glow`） |
| 行（定型文） | 高さ = `appearance.fontSize` × 2（既定 13 → 26 px。SPEC §3.1）、左右の内側 8 px、左端 12 px は `▸` の場所。文字 `appearance.fontSize`（既定 13 px）`FlRowText`、**1 行で表示**（改行は `⏎`（`FlTextMuted`）、タブは空白 1 つにし、連続する空白はまとめない）。収まらない部分は末尾を `…` で切る（`TextTrimming = CharacterEllipsis`）。枠 1 px 透明、角 2 px。ホバー = 1 px `FlRowHoverBorder` + 面 `FlRowHoverBackground` + `FlRowHoverGlow`（演出 `rowHover` = `glow`）+ 左端に `▸`（`FlAccent`）。押下 = 0.97 倍（`rowPress`）。**文字発光は付けない**（`FlTextGlow` の注記と同じ理由） |
| 選んだ行（キーボード、SPEC §6.1） | 1 px `FlRowSelectedBorder` + `FlRowSelectedGlow` + 左端の `▸`（面は付けない。ホバーと重なったらホバーの面 + 選択の枠） |
| 空のグループ | 一覧の中央に案内文「このグループには定型文がありません」（`FlEmptyGroupText`、12 px）と、その下に「編集で追加…」のリンク風ボタン（`FlAccent`。押すと編集ウィンドウをこのグループで開く） |
| 全文のプレビュー（SPEC §3.4） | 盤面の外（右。入らなければ左）に出す別の小窓。`FlSurface` 96% の面、1 px 主色 60% の枠、角 2 px、外側に `FlToastGlow` と同じ発光。文字は `FlText` 12 px、改行をそのまま見せる。最大 幅 360 × 高さ 240、超えたら末尾を `…`。上に等幅 9 px の `LEN 128` / `LINES 4`（`FlHudText`。翻訳しない） |
| 貼り付けのフィードバック | `pasteFlash`（既定 `glow`）。行の発光が一瞬 `FlPasteGlow` へ強まる。並行して盤面の非表示演出が始まる |
| トースト | `FlToastBackground`、1 px `FlToastBorder`、左に縦帯 3 px（情報 = `FlAccent`、エラー = `FlError`）、**縦帯の右に状態コード**（§3.9 コンソール風: 情報 `>` / エラー `!`、`FlMonoFontFamily` 12 px 太字、縦帯と同じ色）、文字 `FlToastText` + 文字発光 `FlTextGlowSoft`（弱）、外側に `FlToastGlow`（エラーは `FlToastErrorGlow`。ネオン管 3 段。矩形の `BoxShadow` のまま）、角 2 px、`FlToastMargin` の余白（34。盤面の余白 `FlGlowMargin` = 40 とは別） |
| 設定画面 | **フレームレス**（§3.8）: OS のタイトルバー無し、盤面と同じ枠部品で囲む（面 `FlWindowBackground`、1.5 px `FlBoardBorder`、四隅を 10 px 面取り（`FlChamfer`）、外側に `FlChromeShadow`（= ネオン管。八角形に沿う）、角ブラケット、余白 `FlChromeMargin` = 40）。最上段にタイトル行（左に**タグ**（§3.9。`FlAccent` の縦棒 2×10 px + 9 px `FlMonoFontFamily` のコード `CFG` 等、`FlHudText`）、タイトル文字 `FlText` + `FlTextGlow`、右端に × ボタン `FlTextMuted` → ホバーで `FlText` + 面 `FlAccentSoft`）、下に 1 px `FlHeaderSeparator`。文字 `FlText` / 注記 `FlTextMuted`、フォント `FlFontFamily`。Fluent のコントロールは**アクセント色の差し替え**（`SystemAccentColor` 系 7 キーを主色に。ToggleSwitch の ON、Slider、ComboBox の選択、フォーカス枠が追従する）と **`TabItem` のアウトライン化**。それ以外のコントロールのテンプレートは書き換えない、コントロールに発光は付けない（静的な画面なので「中」の範囲外）。例外として文字発光を付ける: **タブ見出し・小見出し = `FlTextGlow`（強）、各行の項目名（左列のラベル）= `FlTextGlowSoft`（弱）**。注記・コントロール内の文字には付けない。読み取り専用の帯は `FlWarning` / `FlOnWarning`。背景画像（§3.7）は敷かない |
| 権限ガイド・通知・ダイアログ（確認・について） | 設定画面と同じフレームレスの枠とタイトル行（§3.8。タグのコードは §3.9 の表）。中身は窓背景・文字・フォントに従う。**下段の押しボタンは `Dialogs.ActionButton`（高さ 32 固定・文字中央・`MinWidth 90`。§3.8「ボタン」）**。表示 / 非表示は盤面と同じグリッチ（§3.8「演出」）。状態表示の色は `FlSuccess` / `FlError` |
| 背景画像（§3.7） | 画像があるときは `Frame` の面（地の色）を描かず、画像（不透明度 `imageOpacity` %）の上に `FlBoardBackground` を覆い（`overlay` %）として重ねる。枠線・発光・角ブラケット・ヘッダー・行・背景グリッド（§3.9）は画像の上に描く。角は面取りの八角形（`FlChamfer` 10）で切る（画像も角で斜めに切れる）。覆いの色は配色（主色の色相の黒）に追従する |
| コンテキストメニュー（SPEC §6.2） | Fluent の `MenuFlyout*` キーを上書き（導出規則の表）: 背景 窓背景 96%、枠 1 px 主色 60%、ホバー 主色 12%、文字 `FlText`、区切り線 主色 20%、角 2 px、フォント `FlFontFamily`。発光は付けない（一時的に出る面なので「中」の範囲外） |
| トレイ / メニューバーアイコン | 配色の対象外（OS の作法に従う。§8。意匠は §3.9「アイコン」） |

**レイアウト例**（ASCII。盤面の構成は SPEC §3.1）

```
 ┏                                                   ┓   ← 角ブラケット（左上・右下 主色 / 右上・左下 副色）
   ┌─────────────────────────────────────────────┐     ← 1.5 px 主色の枠 + 外側 3 段の発光 + 内側の光（八角形。余白 40 px）
   │  0   1  [2]  3   4   5   6   7   8   9      │     ← グループタブ: 選択 = 主色文字 + 下線 2 px + 発光、空のグループ = 注記色 50%
   │ ─────────────────────────────────────────── │     ← 区切り線 1 px（主色 20%）
   │   いつもお世話になっております。株式会社…  │
   │ ┏▸お疲れさまです。〇〇です。⏎よろしくお…┓ │     ← ┏▸┓ = ホバー / 選択中の行（主色 1 px + 12% 面 + 発光、左端に ▸）
   │   以上、よろしくお願いいたします。          │
   │   〒100-0001 東京都千代田区千代田1-1 …    │
   │ ─────────────────────────────────────────── │
   │  GRP 2 · 挨拶      ITEMS 4        15:05     │     ← ステータス行（§3.9 H1）
   └─────────────────────────────────────────────┘
 ┗                                                   ┛
```

**配色（主色・副色）**

配色は**主色（`FlAccent`）と副色（`FlAccent2`）の 2 色**だけをユーザーが選び、**残りの色トークンはすべて 2 色から機械的に導く**。 のプリセット 5 種（シアン / レッド / ブルー / グリーン / パープル）は **2 色の組**として残し、選ぶと 2 色が入る（プリセットは「入力の近道」であって保存の単位ではない）。**形・線・発光のぼかし量・余白・フォント・警告 / エラー / 成功の色は配色で変えない**。既定はシアン（主色 `#00E5FF` / 副色 `#FF2BD6`）。

| 項目 | 内容 |
|---|---|
| 設定 | `appearance.colors.primary` / `appearance.colors.secondary`（`#RRGGBB` の文字列。大文字小文字を問わない。8 桁ならアルファを捨てる。不正な文字列はその色だけシアンプリセットの値に戻してログ 1 行。`spec/SETTINGS.md` 表示タブ） |
| プリセット（Core `ColorPresets`。ID は旧 `AccentPreset` を流用、JSON には書かない） | `cyan` シアン `#00E5FF` / `#FF2BD6`（既定）、`red` レッド `#FF2E4D` / `#37EBF3`（CP2077 インベントリ風。副色は同作のハイライト色に近い水色）、`blue` ブルー `#4D8BFF` / `#C77DFF`、`green` グリーン `#39FF88` / `#FF4FD8`、`purple` パープル `#B967FF` / `#00E5FF`。2 色が完全一致するプリセットがあれば設定画面の「配色」はそれを表示し、無ければ「カスタム」 |
| 設定 UI（表示タブ、不透明度の上） | 「配色」`ComboBox`（5 プリセット + カスタム。各項目に主色・副色の色見本 12×12 を 2 つ + 名前。プリセットを選ぶと 2 色を書き換える。「カスタム」は表示専用で選べない）→「主色」「副色」の 2 行 = Avalonia 公式の **`ColorPicker`**（`Avalonia.Controls.ColorPicker` パッケージ、`ColorView` をフライアウトで開く。アルファ無効、タブ = スペクトル / パレット / 成分。パレットはプリセット 10 色 + ネオン 12 色相（S 100% / L 55%）の独自 `IColorPalette`）+ 右に hex（`FlMonoFontFamily`、読み取り表示）。副色の行の右端に「⇄ 入れ替え」ボタン（主色と副色を交換。マゼンタ基調 + シアンの副色がワンクリックで作れる）。`ColorPicker` がフレームレスの透過窓の上で乱れるなら退避案: 色見本の格子（12 色相 × 2 明度 + プリセット）+ hex の `TextBox` の自前コントロール |
| 即時反映 | `ColorPicker` の `ColorChanged` のたびに `SettingsHub.Update(…, Colors)`。盤面・トースト・設定画面・Fluent のアクセントが辞書の差し替え（§10.5）で描き直される。スペクトルをドラッグしている間は 1 フレームに 1 回に間引く。保存は従来どおり 500 ms のまとめ書き |
| 読みやすさ（Core `Normalize` の範囲） | **暗い色の自動補正はしない**（`MinContrast` は削除済み）。`ColorSettings.Normalize` がすることは **(a) `TryParse` できない色をシアンのプリセットの値に戻す（ログ 1 行）、(b) 表記を大文字の `#RRGGBB` にそろえる** の 2 つだけで、ユーザーが選んだ色の値は変えない。暗い主色・副色（例 `#0000CC` や黒に近い色）を選ぶと枠・発光・タブが面に埋もれて見えにくくなるが、**それはユーザーの選択に委ねる**（プリセットを選べば必ず戻せる）。文字 3 色は導出で明度 0.95 / 0.61 / 0.88 に固定されるので、主色が暗くても本文・注記・ラベルは読める（面との比は常に 6 以上）。コントラスト不足の**警告の注記**（値は変えずに知らせるだけ）は §12 |
| 無彩色 | 主色の HSL 彩度が 0.15 未満なら背景・文字の色相を使わず無彩色（S = 0）で導出する（白 / 灰の端末風。色相が不安定なため） |

導出規則

| 導出先トークン | 規則 |
|---|---|
| `FlWindowBackground` | HSL(H, 0.38, 0.065)（例: シアンで `#0B1516` 程度。旧手調整値は紺 `#0B0F1A`） |
| `FlBoardBackground` | HSL(H, 0.40, 0.083) |
| `FlSurface` | 盤面内の面（タブのホバー、全文のプレビュー、設定のカード） | 主色の色相の暗い面（旧 `#121A2B`） |
| `FlText` | HSL(H, 1.0, 0.95) |
| `FlTextMuted` | HSL(H, 0.28, 0.61) |
| `FlHudText` | 主色 90%（`E6`） |
| `FlRowText` | HSL(H, 1.0, 0.88) |
| `FlBoardBorder` | 主色 80%（`CC`） |
| `FlAccentSoft` / `FlRowHoverBackground` / `FlTabHoverBackground` / `FlTabCheckedHoverBackground` | 主色 12%（`1F`） |
| `FlAccent2Soft` | 副色 14%（`24`） |
| `FlRowHoverBorder` / `FlTabSelectedText` / `FlTabIndicator` / `FlRowSelectedBorder` | 主色 |
| `FlHeaderSeparator` | 主色 20%（`33`） |
| `FlEmptyGroupText` | 注記色 50%（`80`） |
| `FlTabText` | 注記色 |
| `FlToastBackground` / `FlToastBorder` / `FlToastText` | 窓背景 96%（`F5`）/ 主色 60%（`99`）/ 本文 |
| `FlOnWarning` | 窓背景 |
| `FlHudGrid`（§3.9 H6）/ `FlHudScanline`（§3.9 H14） | 主色（アルファなし。濃さは設定 `appearance.hud.gridOpacity` / `scanlineOpacity` を `GridLayer.Opacity` / `ScanlineLayer.Opacity` に掛ける。 に「主色 7%」から変更、走査線は） |
| `FlTabOutline`（§3.9 H7） | 注記色 50%（`80`） |
| `FlBoardGlow` / `FlRowHoverGlow` / `FlTabSelectedGlow` / `FlToastGlow` | `Neon(主色, …)`: 4 / 14 / 32 + 内側 15% / 3 / 10 / 20 + 内側 10% / 3 / 8 / 16 / 3 / 12 / 26（3 段の不透明度は共通で 90 / 55 / 30%） |
| `FlPasteGlow` | `Neon(主色, 4, 14, 28, 内側 20%)` |
| `FlRowSelectedGlow` | `Neon(主色, 3, 10, 20)`（内側なし） |
| Fluent のメニューのキー `MenuFlyoutPresenterBackground` / `MenuFlyoutPresenterBorderBrush` / `MenuFlyoutItemBackgroundPointerOver` / `MenuFlyoutItemForeground`（`PointerOver` も）/ `MenuFlyoutSeparatorBackground` | 窓背景 96%（`F5`）/ 主色 60%（`99`）/ 主色 12%（`1F`）/ 本文 / 主色 20%（`33`）。コンテキストメニュー（§6.3）のため、`SystemAccentColor` と同じくサイバーパンク辞書で Fluent のキーを直接上書きする（新しい `Fl*` は増やさない） |
| `FlTextGlow` / `FlTextGlowSoft` | `DropShadowEffect`（オフセット 0）ぼかし 10・主色 90% / ぼかし 6・主色 50% |
| `SystemAccentColor` と `Dark1/2/3`・`Light1/2/3` | 主色 / 主色を黒と 20・40・60% 混ぜる / 白と 30・50・70% 混ぜる |
| 変えないもの | `FlWarning` `#FFC53D` / `FlError` `#FF3B5C` / `FlSuccess` `#2EE6A6` / `FlToastErrorBackground` `#F5160B14` / `FlToastErrorGlow`、形・線・余白・フォントの全トークン |

- 角ブラケット（左上・右下 = 主色、右上・左下 = 副色）、グリッチの影 2 枚（`FlAccent` / `FlAccent2`）、トーストの縦帯（情報 = 主色）、光の玉（`orbTwin` の 2 個目 = 副色）は既にトークン参照なので自動で追従する。
- 主色が赤系のとき `FlError` と近くなる。エラートーストは背景（ワイン色）・状態コード `!`（§3.9）・文言で区別できるので許容する（旧「`red` に限り橙にする」案は、任意色になったので採らない）。
- 切替は即時（配色を変えると盤面・トースト・設定画面が描き直される）。ウィンドウの大きさ・余白・演出の既定値は変わらないので `Render` し直す必要はない（§10.5）。
- 色トークンは常に主色から導出する。プリセットと手入力で同じ色を指定した場合は、同じ背景色になる。

### 3.7 盤面の背景画像

- **対象**: 盤面ウィンドウの面（`Frame` の内側、ヘッダーとグリッドの下）だけ。**全グループ共通**。設定画面・ダイアログ・トーストには敷かない（文字とコントロールの読みやすさを優先。§12 に候補として残す）。
- **設定** `appearance.background`（省略可能な追加なので `schemaVersion` は 2 のまま、§9.3）:

| キー | 型 | 既定 | 意味 |
|---|---|---|---|
| `image` | 文字列 / null | null（なし） | 画像ファイル。**データフォルダからの相対パス**（`background/<ファイル名>`。設定画面から選んだときはこの形）または絶対パス（手編集用。`~` / 環境変数を展開） |
| `fit` | enum | `fill` | 表示方法: `fill` 埋める（アスペクト比を保ち、はみ出た分を切る）/ `fit` 収める（余白は覆いの色だけ。地の色は描かないので透ける）/ `stretch` 引き伸ばす / `tile` 並べる（原寸、左上から）/ `center` 中央（原寸。盤面より大きければ中央を切り取る） |
| `overlay` | int (%) | 40 | 覆いの濃さ（0〜90、5 刻み）。画像の上に `FlBoardBackground` をこの不透明度で重ねる。大きいほど画像が薄くなりアイコン・文字が読みやすい。画像があるときは面の地の色を描かないので、**盤面の暗さはこの覆いだけで決まる**。100 は「画像なし」と同じなので範囲に含めない |
| `imageOpacity` | int (%) | 100 | 画像自体の不透明度（10〜100、5 刻み。 B′）。画像の層 `Backdrop` の `Opacity`。下げると**画像の部分からデスクトップが透ける**（覆いの色は残る）。ウィンドウ全体の `appearance.opacity` とは独立で、両方が掛かる。省略可能な追加なので `schemaVersion` は 2 のまま |

- **描画の層**（下から）: `Frame` の面（`FlBoardBackground`。**画像があるときは描かない = `Transparent`**。画像が無いときだけ従来どおり描く）→ 画像 `Backdrop`→ 覆い `Overlay`（`Border`、`Background = FlBoardBackground`（DynamicResource）、`Opacity = overlay / 100`。画像が無いとき・`overlay = 0` のときは非表示）→ ヘッダー・区切り線・グリッド（今の `DockPanel`。`Frame.Padding` 6 を `DockPanel.Margin` 6 に移し、画像は余白の下まで敷く）。枠線・外側の発光・角ブラケット・`GlitchLayer` / `DragLayer` は今までどおり画像の上 / 外側。
  - **地の色を描かない理由**: 「ウィンドウ全体の不透明度」で薄くしても、明るい画像は透けにくく見える。かといって地の色を残したまま画像だけ薄くすると、透けて見えるのは地の色であってデスクトップではなく、「覆い」と同じ効果にしかならない。画像があるときは地の色を抜き、画像の不透明度を下げた分だけデスクトップが透けるようにする。
  - **帰結**: `imageOpacity = 100` なら「埋める」「引き伸ばす」「並べる」は画像が面を覆うので見た目は従来どおり。「収める」「中央」で画像が届かない余白と、透明 PNG の透明部分は**覆いの色だけ**になり、地の色があったときより透ける（余白を不透明にしたければ覆いを上げるか「埋める」にする）。
- **他の設定・演出との関係**: 不透明度設定（`Window.Opacity`）は画像・覆い・中身のすべての層に等しく掛かる（同じウィンドウの中なので自動。`imageOpacity` は画像の層だけに掛かる別の軸）。グリッチ（`spec/EFFECTS.md`）は `Frame` の静止画を撮るので画像ごとブレる（追加の処理なし）。配色を変えても画像はそのままで、覆いの色だけが `FlBoardBackground`（主色の色相の黒）に追従する（コードで色を計算し直す必要はない）。背景グリッド（§3.9 H6）は覆いの上・スロットの下に描く。背景画像の切替に演出は付けない（静的。EFFECTS.md「対象外」）。
- **表示方法の意味**: 面（`Backdrop` の `Bounds`、論理 px）を W×H、画像の原寸（`Bitmap.Size`、論理 px。長辺 2048 px に縮めた後の大きさ）を w×h として、`fill` = 倍率 `max(W/w, H/h)` でアスペクト比を保って面を埋め、はみ出た分を中央で切る（元画像の中央の矩形 → 面全体）/ `fit` = 倍率 `min(W/w, H/h)` で全体を収め、面の中央に置く（余白は覆いの色だけ）/ `stretch` = 元画像全体 → 面全体（比率は保たない）/ `tile` = 原寸（1 画像 px = 1 論理 px）を左上 (0, 0) から縦横に `ceil(W/w) × ceil(H/h)` 枚並べ、面からはみ出た分は切る / `center` = 原寸を面の中央に置く。面より大きい辺は中央で切る。**元画像の補間は `HighQuality`（作り置きのとき 1 回だけ）、作り置きを貼るときは等倍なので補間なし（`BitmapInterpolationMode.None`。継ぎ目や 0.5 px のずれが見えたら `LowQuality` に落とす）**。
- **作り置き（`Themes/BackdropLayer`）**: 背景画像は表示サイズと拡大率に合わせて一度リサンプルし、描画時は等倍で貼る。
  - **作り置きの大きさ** = 面の大きさ（`Backdrop.Bounds.Size`）× `RenderScaling` を切り上げたピクセル数、DPI = 96 × `RenderScaling`。= 画面のデバイス px と 1:1 で、貼るときに拡大縮小が起きない。
  - **作り方**: `RenderTargetBitmap` を作り、その `DrawingContext` に上の「表示方法の意味」どおり元画像を `DrawImage(source, 元の矩形, 先の矩形)` で描く（`tile` は枚数ぶん繰り返す）。描くのは UI スレッドで同期的に 1 回（Retina の 420×330 の面で 5〜20 ms 程度の見込み。ソフトウェア描画）。**覆い・画像の不透明度は焼き込まない**（覆いは別の層 `Overlay` のまま、画像の不透明度はスライダーに追従して毎回変わりうるので描画時の `PushOpacity`）。
  - **作り直す契機**（`BackdropLayer.Refresh()`。「大きさ・`RenderScaling`・表示方法・元画像の参照」の 4 つを前回の値と比べ、同じなら何もしない = `PulseLayer.Refresh` と同じ流儀）: (1) 層の `SizeChanged`（面の大きさが変わる操作すべて = 盤面の幅・行数・文字の大きさ・ステータス行の ON/OFF・グループ切替で行数の違うページへ移る。`Render` → `RestoreSize` の後のレイアウトで起きる）、(2) `RenderScaling` の変化、(3) 画像の差し替え・クリア・表示方法の変更。**作り直さないもの**: 画像の不透明度・覆い（描画時の値）、配色（画像は配色に依らない）、面取り（切り抜きは親の `Clip`）、ページ切替で行列数が同じとき、Hide / Show。大きさが 1 px 未満（準備運動中に macOS が窓を 0×0 に縮めたとき）や画像なしでは作らず、持っている作り置きを捨てる。
  - **表示遅延（§11）への影響**: 作り置きは「画像を選んだとき」「大きさが変わったとき」に起きるので、ポップアップの Hide / Show（大きさは変わらない）では何もしない。起動後の最初のレイアウトでの作り置きは `PopupController.Warmup`（準備運動。起動時に画面外で一度表示する）の中で済むので、ユーザーの最初の表示にも載らない。Debug ログに「背景の作り置き: W×H px, n ms」を 1 行出し、「表示まで n ms」が導入前と変わらないことを見る。
  - **メモリ**: 作り置き 1 枚 = 面のデバイス px × 4 バイト（Retina の 420×330 の面で約 2.2 MB、800×600 の大きな盤面で約 7.7 MB。等倍モニタならその 1/4）。元画像（≤ 16 MB、§3.7「大きな画像」）は作り直しのために持ち続ける（捨てて作り直しのたびにファイルから読み直す案は、読み直しが非同期になり面の大きさが変わった瞬間に画像が一瞬消えるので採らない）。§11 の常駐メモリの目標は背景画像なしの値なので、画像ありではこの合計ぶんだけ上に出てよい。作り直しのたびに古い `RenderTargetBitmap` を `Dispose` する（常に 0 または 1 枚）。
  - **失敗時**: `RenderTargetBitmap` が作れない・描けない（例外）ときは作り置きをあきらめ、同じ数式（`BackgroundLayout`）で元画像を毎フレーム `DrawImage` する（= 従来の `ImageBrush` と同じ負荷。見た目は同じ）。Debug ログに 1 行。次の `Refresh` でまた試す。
  - **他との関係**: グリッチの静止画は `Frame` を撮るので作り置きの画像ごと写る（従来どおり）。設定画面のプレビュー（120×68 の `Image`）は元画像のまま（作り置きは使わない）。`tile` / `center` の「原寸」の意味は従来どおり（1 画像 px = 1 論理 px。Retina では 2 倍に拡大されて並ぶ）。
- **画像ファイルの扱い**: 設定画面の「選択…」で選んだファイルは**データフォルダの `background/` へコピー**し（同名があれば上書き。コピー元が既に `background/` の中ならコピーしない）、設定には相対パス `background/<ファイル名>` を保存する。「クリア」で `image = null` にし、`background/` にあるコピーを削除する。別の画像に差し替えたときも古いコピーを削除する（`background/` には常に 0 または 1 ファイル）。手編集で絶対パスを書いた場合はコピー・削除・エクスポートの対象外（`iconOverride` と同じ扱い）。理由: ポータブルモード（§9.1）でフォルダごと持ち運べる、エクスポート zip（SETTINGS.md データタブ）に入れられる、元ファイルを動かしても消えない。1 枚だけなので容量の心配が小さい。
- **対応形式**: png / jpg / jpeg / bmp / gif（最初のフレーム）/ webp（Avalonia（Skia）が標準で読めるもの）。svg / heic / ico / icns は対象外（ファイル選択のフィルタにも出さない）。
- **大きな画像・メモリ・表示遅延**: 読み込み時に**長辺が 2048 px に収まるよう縮小してデコード**する（`Bitmap.DecodeToWidth` / `DecodeToHeight`。BGRA でメモリ上限 約 16 MB、通常 10 MB 以下。§11 の常駐メモリの目標は背景画像なしの値なので、画像ありではこのぶんだけ上に出てよい。表示後 150 MB + 約 16 MB が上限の目安）。`tile` / `center` の「原寸」もこの上限の後の大きさ（2048 px を超える画像は縮んで並ぶ。許容）。デコードは**バックグラウンドスレッド**で、起動時と設定変更時に 1 回だけ行う（結果は 1 枚だけメモリに保持。差し替え時に古い `Bitmap` を `Dispose`）。できるまでは面の色だけで表示し、できたら `ImageBrush` を張る（切替は瞬時。演出なし）。ポップアップの表示時には何もしない（Hide / Show 再利用で画像は張りっぱなし）ので、表示遅延（§11）には影響しない。`Render`（グリッドの作り直し）も不要。Retina / 高 DPI でも**デコードの上限**には拡大率を掛けない（2048 px のデコードで足りる）。**表示する大きさへの縮小は上の「作り置き」で 1 回だけ行う**（作り置きのぶん + 数 MB）。
- **見つからない / 読めない**: 面の色だけで表示し、ログに 1 行。設定値は**消さない**（外付けドライブなど一時的な欠落のため）。設定画面の「背景画像」行のプレビューを空にし、`FlError` の注記「画像が見つかりません: <パス>」または「画像を読み込めません: <パス>」を出す。トーストは出さない（起動のたびに出るとうるさい）。
- **設定 UI**（表示タブ、不透明度の下。SETTINGS.md）: 「背景画像」行 = プレビュー（120×68、`Uniform`、無ければ `FlHeaderSeparator` の枠（実線。Avalonia の `Border` に破線は無い）に「なし」）+ ファイル名 + 「選択…」（OS のファイル選択、上記の形式フィルタ）+ 「クリア」。続けて「表示方法」`ComboBox`（埋める / 収める / 引き伸ばす / 並べる / 中央）と「画像の覆い」スライダー（0〜90 %、5 刻み、注記「盤面の色を画像の上に重ねます。大きいほど画像が薄くなり、アイコンと文字が読みやすくなります。」）、その下に「画像の不透明度」スライダー（10〜100 %、5 刻み、注記「下げると画像の部分からデスクトップが透けます。」）。画像が無いときは表示方法・覆い・画像の不透明度を無効表示。変更は即時反映（`SettingsChange.Background`）。
- **ドロップで設定**:
  - **ドロップ先は設定画面 表示タブの「背景画像」行の サムネイル（プレビュー 120×68）だけ**（: 「設定画面の背景画像の項目だけで良い。盤面・ページ設定ダイアログは不要」→「行全体ではなくサムネイル部分だけ」）。ファイル名・「選択…」「クリア」・行のラベルはドロップ先ではない（落としても OS の既定どおり何も起きない。強調もしない）。Explorer / Finder から画像ファイルをサムネイルへ落とすと、「選択…」でそのファイルを選んだときと**同じ処理**。行の下に常時の注記「サムネイルに画像ファイルをドロップしても設定できます。」（`Settings_Appearance_Background_Note`）を出して、120×68 の小さな的でも気付けるようにする。
  - 盤面へのドロップは受け付けない（盤面は D&D の受け口を持たない。SPEC §6）。
  - **受け付けるもの**: `DataFormat.File` のローカルファイル（`TryGetLocalPath()` が取れるもの）で、拡張子が対応形式（上の「対応形式」= Core `BackgroundImageStore.Extensions`。大文字小文字を区別しない: `.PNG` / `.Jpg` も可）のもの。**複数ファイルなら最初の対応ファイル 1 つ**を使い、残りは黙って無視する（背景画像は 1 枚なので）。判定は Core `BackgroundImageStore.IsSupported(path)` / `FirstSupported(paths)`（拡張子の文字列判定だけ。I/O なし）。
  - **受け付けないもの**: フォルダ（Finder の `.app` もフォルダとして届く）、非対応形式（svg / heic / ico 等）、テキスト / URL だけのドロップ、ローカルパスが取れないもの（仮想化されたストレージ）。対応ファイルが 1 つも無ければ何も変えず、トースト `Toast_BackgroundDropUnsupported`「背景画像にできる画像ファイルがありません（png / jpg / jpeg / bmp / gif / webp）。」を 1 回出す。例外（コピー失敗）は「選択…」と同じ `Settings_Appearance_BackgroundCopyFailed`。
  - **ドラッグ中の表示**: サムネイルの上にある間、その枠線を `FlAccent2`・面を `FlAccent2Soft`（発光は付けない）にし、画像が無いときは「なし」の代わりに「ここにドロップ」（`Settings_Appearance_Background_DropHint`）を出す。離れたら（`DragLeave`）／落としたら元に戻す。`DragOver` の時点でファイル一覧が読める場合（Windows / macOS とも通常は読める）は、対応ファイルが無ければ `DragDropEffects.None`（強調しない。OS のカーソルが「禁止」になる）。読めない場合（`TryGetFiles()` が null）は `Copy` で受け付けてドロップ時に判定する（上のトースト）。盤面は `DragOver` で `DataFormat.File` の有無しか見ていない（§6.1）ので、設定画面のほうが一段厳しい判定になるが問題ない。
  - **元に戻す**: 無い。設定画面の他の項目と同じで、「元に戻す」（§6.7）は盤面の操作だけ。「選択…」と同じく前の画像のコピーは `Import` が消す（`background/` は常に 0 または 1 ファイル）。
  - **OS 差**: 無い。受け口は盤面と同じ Avalonia `DragDrop`（§6.2「実装方式」）で、Platform のインターフェースは増えない。フレームレスの `ChromeWindow`（§3.8）でも盤面（同じくフレームレス）と同じく D&D は届く。設定画面がアクティブでなくてもドロップは届く（アクティブ化はしない。§3.5 の規則はそのまま）。
  - 設定項目・JSON（`schemaVersion`）は変わらない（入力経路が増えるだけ）。
- **エクスポート / インポート**（SETTINGS.md データタブ）: zip に `background/<ファイル名>` を含める（`image` が相対パスのときだけ）。インポートでは `background/` に書き戻してから再起動する。
- **確定した値**（推奨どおり）: 覆い = 既定 **40**・範囲 **0〜90**（5 刻み）、画像の不透明度 = 既定 **100**・下限 **10**（5 刻み）、表示方法の既定 `fill`、デコード上限 長辺 2048 px、プレビュー 120×68。Windows 側（ファイル選択ダイアログ・`%APPDATA%\CPClipboard\background\`・メモリ・表示遅延）は  で確認する。
- **実装の注意**（今後も効くもの。実装は `BoardWindow.ApplyBackground` / `FrameChrome` / `BoardBackgroundLoader`）:
  - `tile` は `ImageBrush.DestinationRect` を**絶対単位（`RelativeUnit.Absolute`）で画像の原寸**にする。既定（相対 0,0,1,1）のままだとブラシ全体に 1 枚だけ描かれる。原寸は `PixelSize / (Dpi.X / 96)`（`DecodeToWidth` の結果は 96 dpi なので通常は割らない）。`center` は `Stretch.None` + `Center` で `DestinationRect` は既定のまま。
  - `Overlay` の `Background` は `DynamicResource FlBoardBackground` で結び、`Opacity` だけ変える。色をコードで計算して `SolidColorBrush` に固めない（テーマ・基調色の即時切替が効かなくなる）。`overlay = 0` のときは層ごと非表示。
  - 地の色の ON / OFF は `FrameChrome.SetSurfaceVisible(bool)`。OFF = `Brushes.Transparent`、ON に戻すときは `backgroundKey` の `DynamicResource` に**結び直す**（`Themed.Res`）。固定ブラシを代入すると、画像を外した後に配色を切り替えても面の色が追従しなくなる。面取り（§3.9 H8）のときに切り替わるのは八角形 `Path` の `Fill`（`Frame.Background` は常に透明）。
  - デコード手順: まず `new Bitmap(fs)` で原寸を読み、長辺 ≤ 2048 ならそのまま使う。超えていれば `Dispose` して `fs.Position = 0` から横長は `DecodeToWidth(2048)`、縦長は `DecodeToHeight(2048)`（大きい画像だけ 2 回デコードになる。許容）。`DecodeToWidth` は小さい画像を拡大してしまうので先に原寸を見る。
  - 読み込みはバックグラウンドで非同期なので、完了は `SettingsHub.BackgroundLoaded` で設定画面に知らせ、プレビューと注記を読み直す（`Changed(Background)` は読み込み前に出るので、それだけではプレビューが古いまま）。
  - UI テストはファイル選択ダイアログを使わず `SettingsWindow.ApplyBackgroundFileAsync(path)` から入る。ドロップ（上の「ドロップで設定」）も OS の D&D セッションは使わず `SettingsWindow.ApplyDroppedPathsAsync(paths)` から入る（`Drop` ハンドラはパスの一覧を集めてこれを呼ぶだけにする）。
- **残課題（メモ）**: 「設定をリセット」（詳細タブ）は `settings.json` を既定に戻すが `background/` のコピーは消さない（`image = null` になって参照されないだけ。害なし。次に画像を選ぶと `Import` が `background/` を空にするので自然に消える）。消したければリセット処理で `BackgroundImageStore.Remove` を 1 回呼ぶだけ。

### 3.8 フレームレスのウィンドウ（設定画面・ダイアログ）

- **対象**: 設定画面、権限ガイド（SPEC §4.3）、通知（`Notice`）、確認ダイアログ、「CPClipboard について」（SPEC §6.2）。= 自アプリの **盤面とトースト以外の全ウィンドウ**（統一感のため混在させない）。盤面は今までどおり（ヘッダー = グループタブ）、トーストも今までどおり（既にフレームレス）。
- **見た目**: OS のタイトルバー・枠・影を消し（`SystemDecorations = None`、背景透過）、盤面と同じ枠部品 `FrameChrome`（§10.5）で囲む。面 `FlWindowBackground`（盤面は `FlBoardBackground`）、枠線 `FlBoardBorder` × `FlBoardBorderThickness`、四隅を 10 px 面取り（`FlChamfer`、§3.9 H8。 採用）、外側の影 `FlChromeShadow`（= `FlBoardGlow` と同じネオン管。八角形に沿うリング描画、§3.6）、角ブラケット（`FlCornerBrackets`）、余白 `FlChromeMargin`。**ウィンドウの大きさ = 本体 + 余白 × 2**（設定画面なら本体 560 × 600 のまま、ウィンドウは幅 640、高さ 600 + タイトル行 34 + 区切り 1 + 80 = 715）。背景画像（§3.7）は敷かない。不透明度設定（`appearance.opacity`）は盤面だけに掛け、これらのウィンドウには掛けない。
- **タイトル行**（`Frame` の中の最上段、高さ 34）: 左に**タグ**（§3.9 H4。`TagCode` があるとき）とタイトル文字、右に × ボタン（`Path` で描く 10×10 の斜め十字、線 `FlTextMuted`、ホバーで `FlText` + 面 `FlAccentSoft`、角 `FlRowCornerRadius`、ツールチップ「閉じる (Esc)」）。下に 1 px `FlHeaderSeparator`。**タイトル行の左ボタンドラッグでウィンドウを移動**（`BeginMoveDrag`。ダブルクリックは何もしない）。タイトル行以外のドラッグでは動かない。
- **閉じる**: × / Esc / Ctrl+W（Mac は ⌘W）。確認ダイアログでは × と Esc = **キャンセル**（`IsCancel` ボタンと同じ結果。Esc は既に `IsCancel` で効いている）。通知・について・権限ガイドは閉じるだけ。**設定画面の下部の「閉じる」ボタンは廃止**（× / Esc / Ctrl+W で閉じる。即時反映・自動保存・閉じるときの `Flush` は変わらない）。Esc は ComboBox のドロップダウンなどが開いていればそちらが先に消費する（Avalonia の既定どおり）。
- **最小化・最大化・サイズ変更**: いずれも無し（固定寸法のまま。信号ボタン・最小化ボタンは置かない）。Windows の設定画面はタスクバーに出す（§7 のまま。`ShowInTaskbar = true` はフレームレスでも効く。Alt+Tab の表示名は `Window.Title`、アイコンは `AppIcon.Window`（`Assets/app-256.png`、§3.9「アイコンの仕様」）。Aero Snap・システムメニューは使えなくなる（固定寸法なので困らない）。
- **macOS**: 信号ボタンは出ない（フレームレスの帰結）。Dock には出ない（§3.5 のまま）。キー入力の受け取り・前面化・閉じたときの返却は §3.5 の規則のまま。
- **確定した値**（推奨どおり）: 余白 34、影 `FlChromeShadow`、タイトル行 34 px、設定画面の下部「閉じる」廃止、ダイアログ群もすべてフレームレス。
- **ウィンドウ位置**: `CenterScreen` / `CenterOwner` は余白込みの矩形で中央寄せ（余白は左右対称なので見た目も中央）。設定画面の位置は保存しない（今までどおり）。
- **配色の即時切替**: 枠・影・ブラケットは `DynamicResource` で追従する。余白は常に 40なので大きさの計算し直しは要らない（`ActualThemeVariantChanged` での再計算はテーマ廃止で不要になったが、残っていても害はない）。
- **透過が取れない環境**: 余白の部分と角が黒く残るだけで動作は変わらない。盤面と同じ扱い。
- **ボタン**（Mac で確認 OK）: 確認・通知・について・権限ガイドの押しボタンは**すべて `Dialogs.ActionButton(text, isDefault, isCancel)`** で作る。高さ **32 固定**、`MinWidth 90`、`Padding 16,0`、文字は縦横とも中央（`HorizontalContentAlignment` / `VerticalContentAlignment = Center`）。理由: 日本語の文字は欧文フォント（Inter）の代替書体で描かれて行の高さが変わり、「OK」と「キャンセル」で高さがずれていた。高さを固定すれば言語・書体に関係なくそろう。新しいダイアログを足すときも `Button` を直接 `new` せずこれを使う（設定画面の中のボタン（「今すぐ再起動」等）は対象外。ダイアログの下段のボタン列だけ）。
- **演出**: `ChromeWindow` の全ウィンドウ（設定画面・すべてのダイアログ）は**盤面と同じ演出の設定 `boardShow` / `boardHide` に従う**（`spec/EFFECTS.md`。新しい演出 ID・設定項目は作らない。SETTINGS.md に変更なし）。**種類が `glitch` のときだけ掛かる**（他の種類（fade / zoom / slide / none）では演出なしで OS のウィンドウ表示に任せる。常駐モードでの扱いと同じ規則）。
  - 表示: 最初のフレームから枠を透明（`Chrome.Opacity = 0`）にしておき、`Opened` の後（`DispatcherPriority.Loaded`）に盤面と同じ `GlitchPlayer.Start`で現す。静止画が撮れない環境では演出なしでそのまま出す（盤面の fade フォールバックとは違い、窓は短い演出なので代替フェードは入れない）。
  - 非表示: `Closing` を一度キャンセルしてグリッチ（後半のフェードアウト、`boardHide` の時間 = 既定 250 ms）で消し、**終わってから同じダイアログ結果で `Close(result)` を呼び直す**（`ShowDialog<T>` の戻り値は変わらない。× / Esc = キャンセルの規則もそのまま）。演出中はウィンドウの当たり判定を切る（`IsHitTestVisible = false`。二度押し防止）。`CloseReason` が `ApplicationShutdown` / `OSShutdown` のとき（アプリ終了・OS 終了）は演出せず即閉じる。
  - 設定「演出の調整…」で `boardShow` / `boardHide` を変えると、盤面と同時に次に開く窓から追従する。
  - 表示遅延（§11）の目標は盤面だけのもので、窓の演出は対象外。トーストは従来どおり `toastShow` / `toastHide`。
- **実装の注意**（今後も効くもの）: `Path.bracket` / `Button.chrome-close` のスタイルは `App.axaml` ではなく `AppTheme.Install` が `Styles` に足す（`TestApp` でも効くように）。`ChromeWindow` の Esc は `KeyDown`（バブル）で受けるので、中のコントロールが処理済み（`IsCancel` ボタン、開いている ComboBox など）なら閉じない。`BodyWidth` の再計算は `ActualThemeVariantChanged` で行い、`SizeToContent.Height` の窓は `Height` を触らない。`Body` の下端が枠に接しないよう `Margin 0,0,0,8` 程度を残す。`SystemDecorations = None` の窓での `ShowDialog(owner)` は Windows では親が `WS_POPUP` でも動く（盤面で実績）。Windows の `ShowInTaskbar = true` + フレームレスでは Avalonia が `WS_EX_APPWINDOW` を付ける（最小化・復帰は。できなければ「許容」としてこの節に注記を足す）。

### 3.9 HUD 風の要素

盤面には次の HUD 要素を用いる。操作に反応する装飾の動作と停止条件は `EFFECTS.md` を参照。

| ID | 要素 | 仕様 |
|---|---|---|
| H1 | ステータス行 | GRP / ITEMS / 日時。11 px UI フォント太字、主色 90%、高さ 18 px。見えている間だけ時刻を更新 |
| H2 | 空のグループ | タブは注記色 50%、一覧は案内文（§3.6「各部の見た目」） |
| H3 | 等幅フォント | HUD のコード部分に Share Tech Mono |
| H4 | タイトルのタグ | 主色の縦棒と翻訳しない CFG / AUTH / SYS / CONFIRM / INFO |
| H5 | トースト | 状態コード `>` / `!` と等幅 12 px 太字 |
| H6 | 背景グリッド | 24 px 間隔、主色、既定の濃さ 3%。行の下に描く |
| H7 | 設定画面のタブ | 1 px の枠、選択中は主色の枠と FlAccentSoft |
| H8 | 面取り | 盤面とダイアログの枠の四隅を 10 px カット |
| H13 | アプリアイコン | 面取りした板・横 3 本の行（1 本目だけ主色で `▸` 付き）・角ブラケット。トレイは面取り八角形と行の記号、macOS は単色テンプレート画像 |
| H14 | 走査線 | 静止線は既定 25%・3 px 間隔。流れる帯は高さ 60 px、既定 4 秒で一周 |

**ステータス行の仕様（H1）**

- 位置: グリッドの下、`Frame` の内側の最下段。上に 1 px `FlHeaderSeparator`。文字域の高さ 18、その上に余白 2 px。左右の `Padding` はグリッドと同じ 6。ウィンドウの高さは `appearance.hud.statusBar` が ON のとき **21 px** 大きい。
- 区画（`Grid` 3 列 `Auto,*,Auto`。左から）: **左** `GRP 2`（選択中のグループの番号。名前があれば ` · 名前` を続け、長ければ `…`）/ **中央** `ITEMS 4`（選択中のグループの定型文の数）/ **右端** 日時。日時の区画は `appearance.hud.date`（既定 ON）と `appearance.hud.clock`（既定 `minutes`）の組み合わせで決まる: 日付 + 時刻 `2026/10/04 15:05`（`seconds` なら `2026/10/04 15:05:34`）/ 時刻だけ `15:05` / 日付だけ `2026/10/04`（`clock = off` + `date` ON）/ 両方 OFF なら区画を出さない。**日付は `yyyy/MM/dd`、時刻は 24 時間表記で固定**（HUD の慣例。OS の日付書式・12 時間表記には従わない。`InvariantCulture`）。日時が長くなったので右端は `Auto` 幅にし、中央の `ITEMS` が残りを取る。
- 文字: **11 px `FlFontFamily`（Chakra Petch）SemiBold、`FlHudText`（主色 90%）、文字間 0.5 px**（`LetterSpacing`。`BoardWindow.axaml` の `TextBlock.hud`）。5 案（等幅のまま / 背景の板 / ラベルと値の 2 色 / 文字の発光 / UI フォント）の比較画像から**ユーザーが「UI フォント」を選択**。発光は付けない（静的でも `DropShadowEffect` は描き直しのたびに重い。比較した「文字の発光」案も採らなかった）。`GRP` / `ITEMS` の語は翻訳しない装飾（Core の `StatusLine` が返す固定文字列。リソースは置かない。`spec/TERMS.md` §5）。
- 更新: グループ切替・描き直し（`Render`）のたびに文字列を組み直す（Core `Hud/StatusLine.Format(groupKey, groupName, itemCount, now, clock, date)`、単体テスト）。日時は盤面が見えている間だけ `DispatcherTimer` 1 つで更新する（`StatusLine.NextTick(now, clock, date)` の間隔 + 20 ms の余裕で次の変わり目に合わせ、発火のたびに付け直す）: `minutes` は**次の分の変わり目**、`seconds` は次の秒、**日付だけ**（`clock = off` + `date` ON）は**次の日付の変わり目**。`clock = off` + `date` OFF ならタイマーを動かさない。表示演出中は止めない（グリッチの静止画には写る）。隠れている間・常駐の収納中はタイマーを止める（`UpdateAmbient` と同じ呼び出し点・同じ条件）。
- 当たり判定: 行ではないので左ボタンのドラッグで**ウィンドウが動く**（SPEC §3.1）。右クリックは盤面背景のメニュー。
- ステータス文字列の更新は盤面が見えている間だけ行い、§11 の表示遅延と CPU の要件に従う。

**タグのコード（H4）**: 翻訳しない装飾。**リソース（`Strings`）には置かず、各ウィンドウのコードに直書きする**。`ChromeWindow.TagCode`（`string?`。null なら縦棒ごと出さない。タイトル文字の左余白はタグあり 10 / なし 12）。見た目は `FlAccent` の縦棒 2×10 px + 9 px `FlMonoFontFamily` `FlHudText`、文字間 1 px（タグの書体・大きさは  のステータス行の変更では変えていない。`FlHudText` が主色 70% → 90% になったぶんだけ少し濃くなる）。既定の割り当て: 設定 `CFG`、権限の設定 `AUTH`、通知（データの読み込み）`SYS`、確認ダイアログ `CONFIRM`、CPClipboard について `INFO`。

**トーストの状態コード（H5）**: 縦帯の右に `TextBlock` 1 つ。情報 `>` / エラー `!`（翻訳しない。コードに直書き）、12 px 太字 `FlMonoFontFamily`、色は縦帯と同じ（情報 `FlAccent` / エラー `FlError`）。文言・表示時間・位置は従来どおり（§3.6「各部の見た目」のトースト行）。

**背景グリッドの仕様（H6）**

- 位置: `Frame` の中の `Grid` で、`Backdrop`（背景画像）と `Overlay`（覆い）の上・`DockPanel`（タブ・行・ステータス行）の下。面全体に広がり（`ClipToBounds`）、`IsHitTestVisible = false`（当たり判定は従来どおり下の面に落ちる = ドラッグでウィンドウが動く、右クリックは背景のメニュー）。
- 線: 24 px 間隔（`GridLayer.Spacing`）の縦横線、1 px、ピクセル境界に合わせて +0.5 px。`Pen` は `FlHudGrid`（= 主色そのもの）から作り、ブラシが差し替わったときだけ作り直す。線の原点は `Frame` の内側の左上（行の位置とは独立。合わせない）。
- 濃さ: **`appearance.hud.gridOpacity`（int %、0〜10、1 刻み、既定 3）**を `GridLayer.Opacity = gridOpacity / 100` で掛ける。トークンにアルファを持たせない理由は、濃さをユーザーが選べるようにするため（配色を変えても濃さは保たれる）。`appearance.hud.grid` が OFF、または `gridOpacity = 0` なら層ごと非表示（`IsVisible = false`。0 のときは描かない）。範囲外の値は読み込み時に 0〜10 へ丸める。
- 設定 UI（表示タブ）: 「背景グリッド」ToggleSwitch + 注記「行の下に主色の細い線を敷きます。」の直下に「グリッドの濃さ」`ValueSlider`（0〜10、1 刻み、`%` 表示）。背景グリッドが OFF の間はスライダーを無効表示（状態による無効化。ステータス行 OFF で「時刻」「日付」が無効になるのと同じ扱い）。
- 反映: どちらも `SettingsChange.Hud`。`Render` の中で `IsVisible` / `Opacity` を付け直す（層の表示切替だけで、ウィンドウの大きさは変わらない）。配色の変更は `DynamicResource` で自動追従。
- CPU / 表示遅延（§11）: 静的（描き直しが起きない）。描き直し 1 回あたり線 30 本程度で無視できる。背景画像（§3.7）の上にも乗る（覆いの上なので、覆いの濃さに関わらず同じ濃さで見える）。

**走査線の仕様（H14）**

走査線は 2 つの独立した要素で、**JSON 上の置き場所は違う**。**静止した走査線は HUD（見た目。`appearance.hud.*`。背景グリッドと同じ静的な層）**、**走査線の帯は常時の演出（`appearance.effects.scanBeam`。層・動く条件は `frameOrb` / `glowPulse` と同じだが、既定だけが ON）**。片方だけでも、両方でも使える。**既定構成（新規の settings.json）では両方が出る。** **設定画面では両方とも「演出」タブに置く**（静止した走査線は演出ではないが、ユーザーには「走査線」という 1 つの見た目の 2 面なので同じタブで並べる。JSON のキーは動かさない = 互換性に影響なし）。

- **静止した走査線**（`Themes/ScanlineLayer.cs`、`GridLayer` と同じ作り）
  - 位置: `Frame` の中の `Grid` で、**`GridLayer`（背景グリッド）の直後・`DockPanel`（タブ・行・ステータス行）の下**。= 面・背景画像・覆い・グリッドの上、アイコン・文字の下。面全体に広がり（`ClipToBounds`）、八角形の切り抜きは `FrameChrome` の中身の `Clip`（§3.9 H8）で自動的に掛かる。`IsHitTestVisible = false`（当たり判定は下の面に落ちる。ドラッグでウィンドウが動く、右クリックは背景のメニュー）。
  - 線: **`appearance.hud.scanlinePitch`（int px、2 / 3 / 4、既定 3）おき**に 1 px の横線、ピクセル境界に合わせて `y + 0.5`。`ScanlineLayer.Pitch` は `StyledProperty<double>`（`AffectsRender`）で、`RenderPage` が設定値を入れる。原点は `Frame` の内側の左上（グリッドと同じ。スロットの格子とは合わせない）。`Pen` は新トークン **`FlHudScanline`**（= 主色そのもの、アルファなし。§3.6）から作り、ブラシが差し替わったときだけ作り直す。縦線は無い（横線だけが走査線）。間隔の選択肢が 3 つだけなのは、2 px 未満は線にならず、5 px 以上は「走査線」より「横罫」に見えるため。2 / 3 / 4 以外の値は読み込み時に 3 へ戻す（`ButtonSize` と同じ扱い）。
  - 濃さ: **`appearance.hud.scanlineOpacity`（int %、0〜50、5 刻み、既定 25）**を `ScanlineLayer.Opacity = scanlineOpacity / 100` で掛ける。グリッドと同じく、トークンにアルファを持たせないのは濃さをユーザーが選べるようにするため（配色を変えても濃さは保たれる）。`appearance.hud.scanlines`（bool、既定 **true**）が OFF、または `scanlineOpacity = 0` なら層ごと非表示（`IsVisible = false`）。範囲外の値は読み込み時に 0〜50 へ丸め、5 の倍数でなければ最も近い 5 の倍数に（`AppSettings.Normalize`）。
  - 値の根拠（Mac、Retina）: 4% / 2 px（最初の試し）と 12% / 2 px は「ほとんど見えない / 見えない」。**25% / 3 px・行の下**をユーザーが選んだ（録画で確認）。2 px おきだと Retina で線が密すぎて面の明度が一様に上がるだけに見え、3 px おきなら線として見える。
  - 間隔を選べるようにした理由: 2 px は Retina では密すぎて面の明度が一様に上がるだけに見えたが、濃さを上げれば CRT らしい細かさになる / 4 px は粗く「柄」として目立ちにくい。濃さと間隔の組み合わせで見え方が大きく変わるので、ユーザーに選ばせる。既定は実物で選んだ 3。
  - 高 DPI: 線は論理 px で描く（グリッドと同じ。Mac の Retina（2 倍）では 2 デバイス px の線が 2 × 間隔 デバイス px おき）。Windows の 125% / 150%（1.25 / 1.5 倍）では 1 論理 px の線がアンチエイリアスで 2 デバイス px にまたがり、間隔も 3.75 / 4.5 デバイス px（間隔 3 のとき）と不揃いになる。Core・設定は変えない。
  - 他の層との関係: 背景画像（§3.7）の上にも乗る（覆いの上なので、覆いの濃さに関わらず同じ濃さで見える）。不透明度設定（`Window.Opacity`）は等しく掛かる。グリッチの静止画は `Frame` を撮るので走査線ごとブレる（追加の処理なし）。行の文字は線の上に描かれる。
  - 設定 UI: 「すべて既定に戻す」ボタンの下に小見出し **「静止した走査線」**（`Settings_Effects_ScanlinesHeading`。「常時の演出」と同じ作り。直上の行名「走査線の帯」と区別し、「走査線」の行と重ならない語）、その下に「走査線」ToggleSwitch + 注記「行の下に主色の横線を敷きます。動かないので CPU を使わず、「アニメーション」がオフでも消えません。」（演出タブに置くので「アニメーション」OFF で無効にならない理由を注記で示す）、その直下に「走査線の濃さ」`ValueSlider`（0〜50、5 刻み、`%` 表示、注記なし）、さらに「走査線の間隔」`ComboBox`（「2 px」「3 px」「4 px」。表示は書式リソース `Common_PixelValue` = `{0} px`（両言語同じ）で組む。注記なし）。走査線が OFF の間はスライダーと `ComboBox` を無効表示（背景グリッド OFF でグリッドの濃さが無効になるのと同じ扱い）。**「アニメーション」OFF では無効にしない**（演出ではなく見た目。`appearance.animation` と無関係に表示される、SETTINGS.md）。**「すべて既定に戻す」の対象外**（ボタンの上にある演出の並び = `effects` + `vsync` だけ。静止した走査線をボタンの下に置くのは、対象がボタンの上の並びであることを形で示すため）。リソースキーは `Settings_Effects_Scanlines` / `_Note` / `Settings_Effects_ScanlineOpacity` / `Settings_Effects_ScanlinePitch`（旧 `Settings_Appearance_Hud_Scanline*` を改名。TERMS.md §4「タブを移したらキーも改名」）。表示タブの HUD の並びは「背景グリッド」「グリッドの濃さ」で終わる。
  - 反映: 3 つとも `SettingsChange.Hud`。`RenderPage` の中で `IsVisible` / `Opacity` / `Pitch` を付け直す（層の表示切替と描き直しだけで、ウィンドウの大きさは変わらない）。配色の変更は `DynamicResource` で自動追従。
  - CPU / 表示遅延（§11）: 静的（描き直しが起きない）。描き直し 1 回あたり線 80〜170 本程度（面の高さ ÷ 間隔）で、グリッドの 30 本より多いが無視できる。表示遅延には影響しない（層は構築時に作り、`Show` では何もしない）。
- **走査線の帯**（`Effects/ScanBeamLayer.cs`。演出 ID **`scanBeam`**、種類 none / **`beam`**、時間 = **1 回流れ切る長さ**（2000〜12000 ms、500 刻み、選んだときの初期値 **4000**）。詳細は `spec/EFFECTS.md`「常時の演出の詳細」）
  - 見た目: 高さ **60 px**、幅 = 面の幅の矩形。塗りは主色の縦グラデーション（上端 0% → 85% の位置で約 11%（`#1C`）→ 下端約 24%（`#3C`）の不透明度 = 下端が明るい「尾を引く」帯）。**上から下へ一定速度**で、帯の下端が面の上端に現れてから帯の上端が面の下端を抜けるまでを 1 周期とし、途切れず繰り返す（`y = −60 + phase × (面の高さ + 60)`、`phase = AmbientMath.Phase(elapsedMs, durationMs)`）。
  - 位置と層: **行・タブ・ステータス行の上**。層は `FrameChrome.Overlays` の **`AmbientRoot` の中**（順: `PulseLayer` → **`ScanBeamLayer`** → `OrbLayer`。玉は帯の上）。`Overlays` は `Frame` の外なので**グリッチの静止画に写らず**、常時の演出の規則（表示演出の後に 250 ms でフェードイン、非表示演出・収納・非表示で即座に消える、止まっている間はフレーム要求なし）がそのまま効く。八角形の切り抜きは `_content.Clip` の外なので、**`ScanBeamLayer.Clip` に面の八角形を自分で持つ**（`FrameChrome.FaceClip`: 中身の `Clip` を `Frame`（= `Overlays`）座標へ移したもの。`Chamfer = 0` なら角丸矩形。大きさが変わったら作り直す）。
  - 駆動: **Avalonia の `Animation` ではなく `AmbientAnimator`（`RequestAnimationFrame`）に載せる**（`AmbientAnimator.Tick` が `ScanBeamLayer.Advance(elapsedMs)` を呼ぶ）。理由: 動く条件・フェードイン・停止中のフレーム要求なし・16 ms の下限・ヘッドレステスト（`Tick`）が既存の 2 つと共通になる。`Animation` だと `IterationCount.Infinite` の開始 / 停止と大きさ変更時の作り直しを別に管理することになり、止めても `Transitions` のように残りやすい。動かすのは子の `Rectangle` の `RenderTransform`（`TranslateTransform.Y`）だけ（層全体の `InvalidateVisual` はしない。`OrbSprite` と同じ方針）。
  - 色: 主色（`FlAccent`）を毎フレーム引き、変わったときだけグラデーションブラシを作り直す（`OrbLayer.UpdateColors` と同じ）。副色は使わない。
  - 動く条件: 常時の演出の共通の規則どおり。
  - 既定は **`beam / 4000`（ON）**。`frameOrb` / `glowPulse` と同じく常時の演出に含む。
  - 他の層との関係: 背景画像・不透明度設定は光の玉と同じ（画像・覆い・中身の上に描かれ、`Window.Opacity` が等しく掛かる）。グリッチ中は止まっているので静止画にも写らない。ドラッグのゴースト（`DragLayer`）とグリッチの静止画層は帯より上。
- **対象にしないもの**: 設定画面・ダイアログ、トースト（一瞬出るだけ）。

**タブのアウトライン（H7）**

- 全タブ: `BorderThickness 1`、`BorderBrush` = `FlTabOutline`（注記色 50%）、`CornerRadius 0`、`Padding 9,2`、`MinHeight 30`、右に 4 px の間隔（`Margin 0,0,4,0`）、**`FontSize 14`**。
- 選択中（`:selected`）: `BorderBrush` = `FlAccent`、`Background` = `FlAccentSoft`。Fluent の選択下線 `/template/ Border#PART_SelectedPipe` は `IsVisible = false`。
- ホバー・フォーカスは Fluent のまま（アクセント色の差し替えで主色に追従）。
- テスト（`SettingsUiTests`）: 日英とも `TabItem` が 7 つ、全部 `BorderThickness 1`、`Bounds.Y` が同じ（折り返していない）、末尾のタブの右端が窓の幅の内側。



- 量: **10 px**（`FrameChrome.DefaultChamfer`、static。§3.6 トークン表の `FlChamfer`）。盤面と `ChromeWindow`（設定画面・ダイアログ群）の両方で同じ。トーストは対象外（従来どおり角 2 px）。設定項目にはしない（見た目の一部。配色でも変わらない）。
- 描き方: `Frame`（`Border`）の `Background` / `BorderBrush` / `BoxShadow` は**すべて外す**。**発光は `Frame` の中の `NeonGlowLayer` 2 枚**（外側 = `_content` の下に置き、余白へはみ出して描く / 内側 = `_content` の中で面の上・`Body` の下）が八角形に沿って描く（§3.6「発光の作り方」の「枠の発光」。断面は Core `NeonProfile`、形は Core `Octagon`）。面と枠線は `Frame` の中身 `_content`（`Grid`）の最背面に置いた**八角形の `Path`**（`Fill` = 面のトークン（盤面 `FlBoardBackground` / 窓 `FlWindowBackground`）、`Stroke` = `FlBoardBorder`、`StrokeThickness` = `FlBoardBorderThickness`。線がはみ出さないよう線の太さの半分だけ内側に取る）で描く。中身（`Body` = 背景画像・覆い・グリッド・タブ・行・ステータス行）は同じ八角形で `Clip` する（背景画像も角で斜めに切れる）。大きさが変わったら（`SizeChanged`）八角形と `Clip` を作り直す。
- 切った角の中: 八角形の外なので、**外側の光が斜辺に沿って回り込む**。 は発光が矩形だったため角の三角形の中に内側の光が見えていて「許容」としていたが、角で四角い光が枠の外に出る症状と同じ原因だったので一緒に直した。
- 角ブラケット: L 字ではなく、八角形の斜辺の外側（枠線から 2 px）を**斜辺に沿って折れた線**でなぞる（縦の腕 6 px → 斜辺 → 横の腕 6 px。色は従来どおり左上・右下 = 主色、右上・左下 = 副色。§3.6 `FlCornerBrackets`）。
- 背景画像なし / あり: `SetSurfaceVisible(false)`（画像あり、§3.7）のときは八角形の `Fill` を透明にする（`Frame.Background` は常に透明。地の色を抜く規則は §3.7 のまま）。
- グリッチ（`spec/EFFECTS.md`）: 静止画は `Frame` を撮るので八角形の面・枠線ごと写る（追加の処理なし）。常時の演出 `frameOrb` の `OrbPath` は**八角形**。`glowPulse` の `PulseLayer` は静的な発光と同じ `NeonGlowPlan` を焼くので同じ八角形（`spec/EFFECTS.md`「常時の演出の詳細」）。発光層は `Frame` の子なので、グリッチの `Frame.Opacity = 0` で一緒に消え、静止画には角の三角形の分だけ写る（従来の内側の光と同じ扱い）。
- 当たり判定: 切った角の三角形は面の外だが `Frame` の矩形の中なので、クリック・ドラッグは今までどおり盤面として扱う。
- 0 に戻す道: `FrameChrome.Chamfer = 0` で従来の角丸矩形（`Border` の面と枠線、L 字のブラケット、`Clip` なし）。製品では使わず、テスト（`ChromeWindowTests`）で両方の形を確認するために残す。
- テスト用の公開: `FrameChrome.Face`（八角形 `Path`）、`SurfaceBrush`（今の面の色。面取りなら `Face.Fill`）、`OutlineBrush`（今の枠線の色）、`OuterGlow` / `InnerGlow`（発光層 `NeonGlowLayer`）。

**アイコンの仕様（H13）**

- 意匠: CPFileLauncher と同じ面取りした濃紺 `#0B1220` の板（枠線シアン `#00E5FF`）と、左上シアン・右下マゼンタ `#FF2BD6` の角ブラケット。板の中は 3×3 のスロットの代わりに**横 3 本の行**（シアン 55% の細線）を置き、1 本目だけ発光（面 25% + 枠）させて左端に `▸`。トレイ用の線画は面取り八角形 + 横 3 本の短線（1 本目の左に `▸`）。案を SVG で作ってユーザーが選ぶ（M6）。
- ファイル（`src/CPClipboard.Desktop/Assets/`）: `icon.svg`（アプリ、1024 基準）/ `tray-template.svg`（メニューバー用、**黒の単色**。面取り八角形 + 横 3 本の短線、線 1.3）/ `tray-color.svg`（Windows のトレイ用、同じ線画をシアンで）/ `tray-warning.svg`（警告、同じ線画を橙 `#FF9F1C` で）。生成物 `AppIcon.icns`・`app.ico`（16 / 24 / 32 / 48 / 64 / 128 / 256 の PNG 入り）・`app-256.png`・`tray-*.png`（32 px）も同じフォルダに置いてコミットする（ビルド時には生成しない）。
- 生成スクリプト `src/build/make-icons.sh`（macOS で実行）: Quick Look（`qlmanage -t`）で SVG を各サイズに描画（1 px ずれることがあるので `sips` で大きさをそろえる）→ `iconutil` で `AppIcon.icns`（16〜512 と @2x）→ 小さな Python で PNG を詰めた `app.ico` → `tray-*.png`。SVG を直したらこれを 1 回走らせて生成物を更新する。
- 組み込み: Windows の exe は csproj の `<ApplicationIcon>Assets\app.ico</ApplicationIcon>`、PNG は `<AvaloniaResource Include="Assets\*.png" />` で `avares://CPClipboard/Assets/…`。App 層 `AppIcon`（静的クラス）: `Create(warning)` = トレイ / メニューバー用 `WindowIcon`（Mac 通常時 `tray-template.png`、Windows 通常時 `tray-color.png`、警告は両 OS とも `tray-warning.png`）、`IsTemplate(warning)` = Mac かつ警告でないときだけ true（`MacOSProperties.SetIsTemplateIcon(trayIcon, …)` に渡す。警告の橙は色を見せたいのでテンプレートにしない）、`Window` = `app-256.png`（`ChromeWindow` が `Icon` に設定。Windows の Alt+Tab・タスクバー用。資産が無いテスト環境では無視）。macOS の `.app` は `Info.plist` の `CFBundleIconFile` = `AppIcon`、`publish-mac.sh` が `Assets/AppIcon.icns` を `Contents/Resources/AppIcon.icns` にコピーする（§10.4）。
- macOS のメニューバー: テンプレート画像なので OS が明暗に合わせて塗る（**暗いメニューバーでは白、明るいメニューバーでは黒）。§8 の「警告はバッジ」は実装では**橙の線画への差し替え**（テンプレート解除）で表す（バッジを重ねるより 16 px で見分けやすい。§8 を合わせた）。

---

## 編集ウィンドウと自動行ショートカット

- 編集ウィンドウのグループ/定型文一覧は盤面と同じ背景・枠・文字・選択のトークンを使う。Fluent既定の灰色ListBox面や選択色を残さず、面はFlBoardBackground、文字はFlRowText、hover/selectionは既存FlRow*とFlAccentへ揃える。ウィンドウ本体のChrome枠と配色追従は維持する。
- 左グループは番号と名前だけを表示し、件数は付けない。空グループも選択でき、選択状態を盤面と同じ主色の枠・面で示す。
- 盤面の先頭26件には小文字a〜zを右端の独立した固定幅欄へ表示する。本文の末尾省略がキー表示を隠さないよう本文と列を分ける。選択marker▸は左端の既存位置を維持。27件目以降は表示しない。キー文字はFlMonoFontFamily、通常FlTextMuted、選択時FlAccent。文字キーの表示は画面上の実体化順ではなく論理行indexで決まる。
- 編集一覧は同じ描画トークンを使うが、盤面の文字キー即実行を持たない。本文TextBoxの複数行・タブ入力、リストのUndo/D&Dと既存focus操作を維持する。
