# 配布: GitHub Releases + 配布サイト（GitHub Pages）+ タグ push の CI

仕様は SPEC §10.4「リリース」「配布サイト」/ §13.2 T2〜T4 / §6.3「について」/ §13.3 C31・C32（2026-10-11 設計）。ここは作業の分解と、サイトの細かい設計。

## 方針（2026-10-11 ユーザー合意 + 設計で決めたこと）

- 配布物は GitHub Releases のアセット（zip）。固定名 `CPFileLauncher-win-x64.zip` / `CPFileLauncher-win-arm64.zip` / `CPFileLauncher-macos-arm64.zip` + `SHA256SUMS.txt`。サイトのボタンは `https://github.com/PalmJoy/CPFileLauncher/releases/latest/download/<アセット名>` 固定。
- バージョンは csproj の `<Version>` が正、タグは `v<Version>`（`X.Y.Z` のみ）。CI が一致を検査する。
- CI は両 OS でテスト → 既存スクリプトの zip オプション → **下書き（draft）の Release**。ユーザーが実機で確かめて公開する。
- Mac は ad-hoc 署名（`CODESIGN_IDENTITY=-`）、Windows は未署名。初回起動の手順をサイトに載せる。
- サイトは `docs/`（main の `/docs` を Pages で公開）、ビルド工程なしの静的サイト、英語 `docs/index.html` + 日本語 `docs/ja/index.html`。
- **2026-10-12 ユーザー確定**: ライセンス **GPL-3.0-only**（「以降」は未回答のため後から広げられる「のみ」）、著作権表示 **「Copyright © 2026 PalmJoy」**、最初の版 **1.0.0**、Release は下書き → ユーザーが実機確認後に公開、サイトは英語 `/`・日本語 `/ja/`（ブラウザの先頭言語が ja なら自動で `/ja/`）、GitHub API で版・日付・サイズを表示（取れなければ非表示）。
- **第三者のライセンス**（2026-10-12 設計、SPEC §10.4「ライセンス文の同梱」）: 配布物に `THIRD-PARTY-NOTICES.txt` を同梱。libuiohook（LGPL-3.0）は **Windows でも `uiohook.dll` だけを単一 exe から外して exe の隣に置く**（2026-10-12 ユーザー確定。Win / Mac とも利用者が差し替えられる = LGPL §4(d)(1)）。libuiohook のソースは Release アセット `libuiohook-source.zip` で毎回添付。

## ユーザーが行うこと

- [x] **U1 ライセンスの確定** → 2026-10-12: GPL-3.0-only、Copyright © 2026 PalmJoy（SPEC §13.2 T2 に記録済み）。「v3 以降」に広げたくなったら後からでも変えられる（逆はできない）。
- [x] **U0 GitHub の組織 `PalmJoy`**（2026-10-12 済み）: 組織 `PalmJoy` に新しいリポジトリ `PalmJoy/CPFileLauncher` を作り、今の中身を「first commit」1 つで push（旧 `yassy0413/CPFileLauncher` の履歴は持ち込まない。旧リポジトリの扱いはユーザー判断）。手元の `origin` も新しい URL。コミットの作者は `yassy` のまま。
- [ ] **U2 リポジトリを public に**（Settings → General → Danger Zone → Change visibility）。その前に: コミットの作者メールが公開されてよいか（`git log --format='%ae' | sort -u`）、履歴に公開したくないファイル・パスが無いか確認。`releases/latest/download/…` と Pages（無料プラン）は public でないと匿名で使えない。
- [ ] **U3 GitHub Pages を有効に**（Settings → Pages → Build and deployment → Source: Deploy from a branch → Branch: `main` / `/docs` → Save）。`docs/` がコミットされた後に行う。
- [ ] **U4 スクリーンショットを撮る**（下の「スクリーンショット」の撮影リスト）。
- [ ] **U5 最初のリリース（`v1.0.0`）**: メインが `<Version>` を `1.0.0` にしてコミット（C4）・タグ `v1.0.0` を push → 下書きのアセットを両 OS で確認（C31 / C32）→ リリースノートを直して「Publish release」。
- [ ] U6（任意）リポジトリの About（右上の歯車）に Website = サイトの URL、Topics（`launcher` `avalonia` `dotnet` `cyberpunk` など）。

## タスク（メイン）

順序: C3（`THIRD-PARTY-NOTICES.txt`。A のスクリプトがコピーするので先に）→ A0 → A → B（手動実行で試運転）→ C1・C5 → D → E → C4 → U5。D はスクリーンショット待ちにせず進める（ヒーローは HTML の模型なので画像なしで成立する）。

### A. スクリプト（zip 化とライセンス文の同梱）

- [x] **A1 `src/build/publish-win.ps1`**（2026-10-12 実装。Windows での実行は未確認 = B2 の試運転で確かめる）
  - publish の後に、リポジトリ直下の `LICENSE` を `bin\<rid>\LICENSE.txt` に、`THIRD-PARTY-NOTICES.txt` を `bin\<rid>\THIRD-PARTY-NOTICES.txt` にコピー（常に。zip のときだけではない）。「想定外のファイル」の警告は**許可リスト方式**にする: 直下で許すのは `CPFileLauncher.exe`・`uiohook.dll`・`LICENSE.txt`・`THIRD-PARTY-NOTICES.txt` の 4 つ（それ以外が直下にあれば従来どおり警告）。**加えて `uiohook.dll` が直下に無ければエラーで止める**（csproj の除外が効いていない = フックが動かない配布物になるため）。`-p:IncludeNativeLibrariesForSelfExtract=true` のオプションは変えない（Skia / HarfBuzz / ANGLE は exe の中のまま）。
  - A0（下）の csproj の変更が前提。
- [x] **A0 `uiohook.dll`（2026-10-12 実装。Mac からの win-x64 publish で `uiohook.dll` が exe の隣に出ることを確認。C32 (6) の実機確認は未） を単一ファイルの束から外す（csproj。A1 より先に）**（SPEC §10.4「LGPL（libuiohook）と Windows の単一 exe」）: **`src/FileLauncher.Desktop/FileLauncher.Desktop.csproj` にターゲットを足す**（ps1 ではなく csproj に置く。既存の publish 項目にメタデータを付けるのは MSBuild のターゲットでしかできず、`dotnet publish` の `-p:` では渡せないため。csproj に「LGPL §4(d)(1) のため。SPEC §10.4」とコメント）。形:
  ```xml
  <Target Name="KeepUioHookOutsideSingleFile" AfterTargets="ComputeResolvedFilesToPublishList" Condition="'$(PublishSingleFile)' == 'true'">
    <ItemGroup>
      <ResolvedFileToPublish Update="@(ResolvedFileToPublish)" Condition="'%(Filename)%(Extension)' == 'uiohook.dll'" ExcludeFromSingleFile="true" />
    </ItemGroup>
  </Target>
  ```
  （`Update` の中の項目メタデータ条件が効かない MSBuild の版なら、`Include` / `Remove` で該当項目を取り出してメタデータ付きで足し直す形に。どちらでも結果は「`bin\<rid>\uiohook.dll` が出て、exe の束に入らない」こと。）Mac（単一ファイルを使わない）と Debug ビルドは変わらない。確かめ方: publish 後に `bin\win-x64\uiohook.dll` があり、起動して `%TEMP%\.net\CPFileLauncher\<hash>\` に `uiohook.dll` が無く、ホットキーが動く（SPEC C32 (6)）。
- [x] **A1b `src/build/publish-win.ps1` の zip**（2026-10-12 実装・未実行）
  - スイッチ `-Zip` を足す。指定時は RID ごとに: `bin\release\` を作る → 既存の `CPFileLauncher-<rid>.zip` を消す → 一時フォルダに `CPFileLauncher\` を作って `bin\<rid>\` の中身をコピー → `Add-Type -AssemblyName System.IO.Compression.FileSystem` → `[IO.Compression.ZipFile]::CreateFromDirectory(<一時>\CPFileLauncher, <zip>, [IO.Compression.CompressionLevel]::Optimal, $true)` → 一時フォルダを消す → zip のパスとサイズを表示。`Compress-Archive` は使わない（5.1 でパス区切りが `\` になる版がある）。
  - 5.1 で動く書き方・BOM 付き UTF-8 のまま（ファイル先頭のコメントどおり）。使い方のコメント行に `-Zip` を足す。
- [x] **A2 `src/build/publish-mac.sh`**（2026-10-12 実装。`--zip` の実行は未 = 起動中のアプリを止めてから）
  - 組み立ての中（`cp -R …lproj` の後、**署名の前**）で `cp "$ROOT/LICENSE" "$APP/Contents/Resources/LICENSE.txt"` と `cp "$ROOT/THIRD-PARTY-NOTICES.txt" "$APP/Contents/Resources/"`。
  - 引数 `--zip` を受ける（それ以外の引数はエラーで使い方を表示）。指定時は署名・検証の後に: `mkdir -p "$ROOT/bin/release"` → `rm -f` → `ditto -c -k --keepParent "$APP" "$ROOT/bin/release/CPFileLauncher-macos-arm64.zip"` → **検証**: `$TMP/verify` に `ditto -x -k` で展開し `codesign --verify --deep --strict "$TMP/verify/CPFileLauncher.app"`（失敗したら終了コード 1）→ zip のパスとサイズを表示。
  - zip 名の `macos-arm64` は `RID` から導く（`osx-` → `macos-`。将来 T7 で RID を引数にしたとき `macos-x64` になるように）。
  - `CODESIGN_IDENTITY=-` で ad-hoc になること（今の作りで `-` は空でないのでそのまま `codesign --sign -` に渡る。確認済み: 2026-10-11 設計時にコードを読んで確認）。CI はこれで呼ぶ。
- [x] **A3 `.claude/skills/publish/SKILL.md`** に `-Zip` / `--zip` と出力先 `bin/release/` を 1 行ずつ。zip は頼まれたときだけ付ける。
- [x] A4（2026-10-12: `.bat` は先頭の引数が `-` なら素通し）ルートの `build-release.sh` / `build-release.bat`（ユーザーの依頼で作成・コミット済み、108ee49）は**残す**。`publish-*.ps1/.sh` を呼ぶ薄いラッパーのまま、zip を渡せるようにするかを決めて合わせる。推奨: 引数をそのまま素通しにする（`.sh` は既に `"$@"` を渡すので `./build-release.sh --zip` が効く。`.bat` は今 `%*` を `-Rid` の後に付けるので `build-release.bat -Zip` が `-Rid -Zip` になって壊れる → 先頭の引数が `-` で始まるなら `-Rid` を付けずに `%*` をそのまま渡す形に直し、使い方の rem 行に `build-release.bat -Zip` / `build-release.bat win-x64 -Zip` を足す（後者は `-Rid win-x64 -Zip`））。CI はラッパーを使わず `src/build/` を直接呼ぶ。

### B. ワークフロー `.github/workflows/release.yml`

- [x] **B1**（2026-10-12 作成）次の内容で作る（`dotnet publish` を直接書かない。zip 化もスクリプト）:
  - `on`: `push: tags: ['v[0-9]+.[0-9]+.[0-9]+']` と `workflow_dispatch`。
  - 最上位 `permissions: contents: read`、`concurrency: { group: release-${{ github.ref }}, cancel-in-progress: false }`、`env: DOTNET_NOLOGO: true, DOTNET_CLI_TELEMETRY_OPTOUT: true`。各ジョブ `timeout-minutes: 30`。
  - **`check-version`**（`ubuntu-latest`）: checkout → csproj の `<Version>` を `publish-mac.sh` と同じ sed で取る → タグ起動なら `GITHUB_REF_NAME` が `^v[0-9]+\.[0-9]+\.[0-9]+$` かつ `v$VERSION` と一致しなければ `::error::` を出して失敗。手動起動なら版を表示するだけ。`outputs.version` に出す。
  - **`build-windows`**（`windows-latest`、`needs: check-version`）: `actions/checkout` → `actions/setup-dotnet`（`dotnet-version: 10.0.x`）→ `dotnet test src/FileLauncher.sln -c Release` → `powershell -NoProfile -ExecutionPolicy Bypass -File src\build\publish-win.ps1 -Zip`（`shell: pwsh` から Windows PowerShell 5.1 を呼ぶ = 手元と同じ処理系）→ `actions/upload-artifact`（name `zip-windows`、path `bin/release/*.zip`、`retention-days: 7`、`if-no-files-found: error`）。
  - **`build-macos`**（`macos-15`、`needs: check-version`）: checkout → setup-dotnet → `dotnet test …` → `zsh src/build/publish-mac.sh --zip`（`env: CODESIGN_IDENTITY: '-'`）→ upload-artifact（`zip-macos`）。
  - **`release`**（`ubuntu-latest`、`needs: [build-windows, build-macos]`、`if: startsWith(github.ref, 'refs/tags/')`、`permissions: contents: write`）: `actions/download-artifact`（`merge-multiple: true`、`path: dist`）→ `curl -fsSL -o dist/libuiohook-source.zip "https://github.com/TolikPylypchuk/libuiohook/archive/${LIBUIOHOOK_COMMIT}.zip"`（ワークフロー最上位の `env: LIBUIOHOOK_COMMIT: a41658fb2bef7503a3bcb305ab8bf849755fe906` = SharpHook 8.0.0 のサブモジュール。取得に失敗したら Release を作らず失敗）→ `cd dist && sha256sum *.zip > SHA256SUMS.txt` → `GH_TOKEN: ${{ github.token }}` で、`gh release view "$TAG"` が成功すれば `gh release upload "$TAG" dist/* --clobber`、無ければ `gh release create "$TAG" dist/* --draft --verify-tag --title "CPFileLauncher $TAG" --generate-notes --notes "<初回起動の手順はサイトへのリンク（英日 1 行ずつ）>"`（`--notes` は自動生成の前に付く）。`--repo ${{ github.repository }}` を付ける（checkout しないため）。
  - アクションは GitHub 公式のみ・メジャー版で固定（`@v4` 等、着手時点の最新メジャー）。
- [ ] **B2 試運転**: push 後に Actions から手動実行（`workflow_dispatch`）→ 両ジョブが緑・Artifact に zip 3 つ。CI だけで落ちるテストがあれば原因を直す（**テストを消したり弱めたりしない**。実機の権限・画面に依存するテストなら `[Trait]` で分けて CI では `--filter` で除き、その理由を SPEC §10.6 に書く）。テストが本物のデータフォルダに触れないこと（CLAUDE.md の約束）も CI で効く。
- [ ] B3 落とした Artifact で C31 (1) の署名検証を手元でも一度（Archive Utility で展開 → `codesign --verify --deep --strict`）。

### C. アプリ側・リポジトリのファイル（ライセンス表示・版）

- [x] **C1「について」**（2026-10-12 実装。探索ヘルパーは Core `Storage/BundledFile`、UI テストあり）（SPEC §6.3）にフォントの表記の前で「Copyright © 2026 PalmJoy」「GNU General Public License v3（GPL-3.0-only）で配布。無保証。」とソースの URL（クリックで既定のブラウザ。既存のシェル起動の経路を使う）を出し、その下に「サードパーティのライセンス」のリンク（`THIRD-PARTY-NOTICES.txt` を既定のアプリで開く。探す順: `AppContext.BaseDirectory` → `BaseDirectory/../Resources/`。無ければ `https://github.com/PalmJoy/CPFileLauncher/blob/main/THIRD-PARTY-NOTICES.txt` を開く）。
  - `AppInfo` に定数: `Copyright = "Copyright © 2026 PalmJoy"`（`i18n:ignore`）、`LicenseId = "GPL-3.0-only"`、`SourceUrl`、`ThirdPartyNoticesFileName`、`ThirdPartyNoticesUrl`。ファイルの探索は OS の分岐を書かない（探す順序だけ。小さな静的ヘルパー、Core に置けるならテストする）。
  - 文言は `Strings.resx` / `Strings.ja.resx` の両方（キーは `spec/TERMS.md` §4 の命名に従い `About_` 接頭辞。TERMS.md にも訳語を足す。例: `About_License` / `About_Source` / `About_ThirdParty`）。
  - UI テスト: 「について」に GPL-3.0-only の文言・著作権表示・ソースの URL・「サードパーティのライセンス」が出る。探索ヘルパーのテスト（1 つ目にあれば 1 つ目、無ければ `../Resources/`、どちらにも無ければ null → URL）。
- [x] **C3 `THIRD-PARTY-NOTICES.txt`（2026-10-12。publish 出力の dll / dylib と突き合わせ済み。追加で見つかったもの: `msquic.dll`（.NET ランタイムの一部 = .NET の notices に含まれる））（リポジトリ直下、新規・手で保守）** を SPEC §10.4「ライセンス文の同梱」の構成で書く。
  - 先に**実際の配布物の中身で部品を確かめる**: `publish-mac.sh` の `.app` の `Contents/MacOS/` と、Windows の publish の一時出力（単一ファイル化の前 = `-p:PublishSingleFile=false` で一度 publish して見る。この確認用の publish は手元だけで、スクリプトには入れない）の dll / dylib を列挙し、SPEC の表と突き合わせる（表に無いネイティブ部品が出たら表と notices の両方に足す）。
  - 全文の出どころ: MIT の許諾文は各パッケージのリポジトリの `LICENSE`（著作権者の行を正確に）、Skia 系は SkiaSharp リポジトリの `THIRD-PARTY-NOTICES.txt`（2.88.9 のタグのもの）、HarfBuzz は `COPYING`、ANGLE は NuGet パッケージ内の `LICENSE`（`~/.nuget/packages/avalonia.angle.windows.natives/2.1.25547.20250602/LICENSE`）、.NET は SDK の `ThirdPartyNotices.txt` と `LICENSE.txt`（dotnet のルート）、LGPL-3.0 は gnu.org の原文（libuiohook フォークの `COPYING.LESSER.md` と同じ版）。
  - libuiohook の節: 「本ソフトウェアは LGPL-3.0 のライブラリ libuiohook（https://github.com/TolikPylypchuk/libuiohook、コミット a41658fb2bef7503a3bcb305ab8bf849755fe906）を使用」、ソースは各 Release の `libuiohook-source.zip` と上の URL、差し替え方（Windows: `CPFileLauncher.exe` の隣の `uiohook.dll` を改変版（同じアーキテクチャ・同じ関数を出すもの）に置き換えるだけ。作り直しは要らない / macOS: `CPFileLauncher.app/Contents/MacOS/libuiohook.dylib` を置き換えて `codesign --force --sign - CPFileLauncher.app`）。
  - **`ThirdPartyNoticesTests`**（Core テスト、`FileLauncher.Tests`）: リポジトリ直下の `THIRD-PARTY-NOTICES.txt` を読み、`src/FileLauncher.Desktop` / `Platform*` / `Core` の csproj の `PackageReference`（`Condition` に Debug を含むもの・`PrivateAssets="all"` を除く）の「名前 + 版」がすべて本文に出てくることを確かめる。加えて libuiohook のコミットがワークフロー（`.github/workflows/release.yml` の `LIBUIOHOOK_COMMIT`）と同じであること。リポジトリのルートはテストのアセンブリから上へ `FileLauncher.sln` / `.git` を探して決める（本物のデータフォルダには触れない）。
- [ ] **C4 版を 1.0.0 に**: `src/FileLauncher.Desktop/FileLauncher.Desktop.csproj` の `<Version>` を `0.1.0` → `1.0.0`。U5 のタグ push の直前のコミットで（B2 の試運転はその前の 0.1.0 のままでよい）。
- [x] **C5 既存の `README.md` に足す**（今はフォルダ構成のメモだけ）: 先頭に製品名・一文の説明・配布サイトへのリンク（`https://palmjoy.github.io/CPFileLauncher/`）・最新版のダウンロード（`releases/latest`）・ライセンス（GPL-3.0-only、`LICENSE` / `THIRD-PARTY-NOTICES.txt` へのリンク）。既存のフォルダ構成のメモは下に「開発者向け」として残す（`docs/` の行を足す）。最小限にとどめ、使い方の詳細はサイトに任せる。

### D. 配布サイト `docs/`

#### D の設計（ここで確定。迷ったらここに戻る）

**ファイル構成**

```
docs/
  .nojekyll
  index.html            英語（既定）
  ja/index.html         日本語（内容は同じ。相対パスは ../assets/）
  assets/
    site.css
    site.js
    icon.svg            src/FileLauncher.Desktop/Assets/icon.svg のコピー（favicon 兼用）
    apple-touch-icon.png  180 px（make-icons.sh の render で作る or sips。任意）
    og.png              1200×630（S1 のスクリーンショット + 製品名。U4 の後）
    fonts/ChakraPetch-Regular.ttf, ChakraPetch-SemiBold.ttf, ShareTechMono-Regular.ttf,
          OFL-ChakraPetch.txt, OFL-ShareTechMono.txt   ← src/FileLauncher.Desktop/Resources/Fonts/ のコピー
    screens/*.png       U4 のスクリーンショット
```

- 外部依存なし（CDN・Google Fonts・解析タグを使わない。API 呼び出しは GitHub API の 1 回だけ）。重さの目安: スクリーンショットを除いて 400 KB 以下、各スクリーンショット 400 KB 以下・`loading="lazy"`・`width` / `height` を書く。
- サイト内のリンクは相対パス（`/CPFileLauncher/` 配下で動く）。

**参考サイトから取り入れるもの**（beautifului.dev / rareui.com を見て具体化。2 サイトとも製品自身の UI 部品を「見本」として並べるのが軸）

| 参考 | 取り入れる | このサイトでの形 |
|---|---|---|
| beautifului | 番号付きの節（01〜21 の連番で編集物のような流れ） | 節の見出しを HUD のタグ風に `01 // DOWNLOAD`（Share Tech Mono、主色、角ブラケット付き） |
| beautifului | 機能を「実物の小さな UI の標本（specimen）」で見せる。アイコン画像の羅列にしない | 機能カードの上半分に HTML/CSS の小さな模型（キーキャップ、2 つのモードの小盤面、タブ、ドロップ先の副色の枠、色見本…） |
| beautifului | ドット / ピクセルの格子のキャンバス、状態チップ、上部の目次 | 背景の格子（アプリの背景グリッドと同じ主色 3%）、チップ `ARM64` `APPLE SILICON` `RECOMMENDED` `GPL-3.0`、ヘッダーの節リンク |
| rareui | 中央の大見出し + コマンド行風の 1 行 + 主ボタン 1 つのヒーロー | 見出し `CPFileLauncher` + 副題 + コンソール行 `> fetch cpfilelauncher --target macos-arm64`（OS 推定で変わる。アプリのトーストの `>` と同じ語法）+ おすすめのダウンロードボタン 1 つ + 「他の OS」 |
| rareui | 上部バー（ロゴ左・リンク・GitHub）、カードの格子、簡素なフッター | そのまま。GitHub のスター数は出さない（API を増やさない） |
| 採らない | スポンサー枠・推薦文・ニュースレター・ライト / ダークの切替 | サイバーパンク専用（SPEC §1.3）なので暗い配色 1 つ |

**見た目（製品 SPEC §3.6〜§3.9 と整合）**

- 色: CSS 変数 `--c1`（主色、既定 `#00E5FF`）/ `--c2`（副色 `#FF2BD6`）、背景 `#0B0F1A`、面 `#121A2B`、本文 `#E6F7FF`、注記 `#7FA6B8`、警告 `#FFC53D`（SPEC §3.6 の表の旧手調整値 = シアンの見た目）。本文の文字色はコントラスト AA 以上。
- 書体: 見出し・ボタン・数字は Chakra Petch（`@font-face`、`font-display: swap`）、タグ・コンソール行・ファイル名は Share Tech Mono。**日本語の本文は Chakra Petch に仮名が無いので** `font-family: "Chakra Petch", system-ui, "Hiragino Sans", "Yu Gothic UI", "Meiryo", sans-serif`。
- 形: カード・ボタン・盤面の模型は**八角形の面取り**（`clip-path: polygon(...)`、面取り `--chamfer: 10px`、大きい物は 14px）。**発光は `box-shadow` ではなく外側の要素の `filter: drop-shadow(0 0 6px var(--c1))`**（`clip-path` で `box-shadow` は切れる。アプリの H8 と同じ理由）。角ブラケットは主色と副色の対角 2 色（疑似要素）。
- 走査線: 盤面の模型とヒーローの面だけに `repeating-linear-gradient`（3 px 間隔、本文の上には掛けない）。走査線の帯（4 秒で上から下）は模型の中だけ。
- グリッチ: 読み込み時に見出しへ 1 回（250 ms、アプリの既定と同じ長さ）、ダウンロードボタンのホバーで短く（`clip-path` の切片 + 主色 / 副色の `text-shadow` のずれ）。
- `prefers-reduced-motion: reduce` で帯・グリッチ・時計以外の動きを止める。フォーカスリングは主色の 2 px（消さない）。

**ページの構成**（英日とも同じ並び。`<section id>` は英語）

0. **ヘッダー**: アイコン + `CPFileLauncher` / 節リンク Download・Features・Setup・License / 言語切替 `EN | JA`（押すと `localStorage['cpfl.lang']` に保存）/ GitHub リンク。
1. **ヒーロー**（`#top`）: タグ `CYBERPUNK FILELAUNCHER` → 見出し → 一文（例: 「ホットキーやマウスで呼び出す、ネオンの盤面のファイル / アプリランチャー。Windows と macOS。」）→ コンソール行 → おすすめのボタン（JS が無ければ「ダウンロード」で `#download` へ）+「他の OS」→ チップ `vX.Y.Z · 日付`（API）`GPL-3.0` `FREE`。右（狭い画面では下）に**盤面の模型**: 4×6 のスロット（線画の汎用アイコン + ラベル。他社のアプリアイコンは使わない）、タブ `01 02 03`、ステータス行 `PAGE 01/03  ITEMS 18  yyyy/MM/dd HH:mm`（JS で現在時刻、分単位）、空きスロットの角マーカー、枠の発光・走査線・帯。模型の下に**配色の見本 5 つ**（シアン / レッド / ブルー / グリーン / パープル、SPEC §3.6 のプリセットの 2 色）: 押すとページ全体の `--c1` / `--c2` が変わる（`aria-pressed`。保存しない）。
2. **01 // DOWNLOAD**（`#download`）: カード 3 枚（Windows x64 / Windows ARM64 / macOS Apple シリコン）。各カード: OS 名、チップ（アーキテクチャ）、ファイル名（等幅）、サイズ（API。無ければ非表示）、条件の 1 行（Windows 10 1809+ / 11、macOS 15+・Apple シリコン専用・Intel Mac 非対応）、ボタン（固定 URL）。おすすめのカードは発光が強く `RECOMMENDED` チップ。カードの下に「zip を展開して使う（インストーラなし）→ 初回の起動は #setup」「すべてのリリース・変更履歴」（`/releases`）「SHA256SUMS.txt」（固定 URL）。
3. **02 // FEATURES**（`#features`）: 標本付きカード（文言は SPEC と食い違わないこと。既定値は SETTINGS.md を見て書く）:
   1. 呼び出し: ホットキー `Ctrl+Alt+Space` / `⌃⌥Space`、`Ctrl(⌃)+中クリック`（キーキャップの模型）
   2. 2 つの表示モード: ポップアップ / 常駐（小盤面 2 つ）
   3. D&D で登録・複数ページ（タブの模型）
   4. Drop-to-Open: ファイルをアイテムに落とすとそのアプリで開く（副色のドロップ枠）
   5. フォルダは既存の Explorer / Finder のタブで開く
   6. 配色は 2 色だけ・HUD の演出（グリッチ・走査線・光の玉）（色見本）
   7. データは人が読める JSON・自動バックアップ・エクスポート / インポート
   8. 日本語 / 英語の UI
4. **03 // SCREENSHOTS**（`#screenshots`）: 画像があるものだけ `<figure>`（キャプション付き、クリックで原寸 = 普通のリンク）。U4 が済むまで節ごと出さない。
5. **04 // REQUIREMENTS**: 表（OS / CPU / 必要な権限（macOS: アクセシビリティ、Finder のタブを使うなら自動化）/ ディスク（zip を展開して Windows 約 130〜150 MB・macOS 約 120 MB、SPEC §10.4 の値）/ 常駐メモリの目安は書かない）。
6. **05 // SETUP**（`#setup`）: Windows / macOS のタブ（`<input type="radio">` + `<label>` の CSS だけで切り替え、JS は推定した OS を最初に選ぶだけ）。手順は番号付き。SPEC §10.4「リリース」の文言に合わせる。
   - Windows: ① zip を展開（任意のフォルダ。`Program Files` でなくてよい。**フォルダごと置き、`uiohook.dll` を exe と離さない** = 離すとホットキー・マウスのトリガーが動かない）② `CPFileLauncher.exe` を開く → SmartScreen「詳細情報」→「実行」（zip のプロパティで「許可する」にしてから展開すると出ない）③ 通知領域にアイコン、`Ctrl+Alt+Space` で盤面 ④ 自動起動は設定画面で。注意: スマート アプリ コントロールがオンの PC では動かない（既知の制限）、Defender の誤検知の可能性。
   - macOS: ① zip をダブルクリックで展開（Safari は自動で展開）② `CPFileLauncher.app` を「アプリケーション」へ移す（先に移す理由も 1 行）③ 開く →「“CPFileLauncher”は開いていません」→「完了」④ システム設定 → プライバシーとセキュリティ → 下の方の「このまま開く」→ パスワード → もう一度開く（または ターミナルで `xattr -dr com.apple.quarantine /Applications/CPFileLauncher.app`、コピー用ボタン）⑤ メニューバーのアイコン → 「権限の設定…」→ アクセシビリティを許可（`⌃⌥Space` が効くまで）⑥ フォルダを Finder のタブで開くとき初回に「Finder を制御」の確認 → 許可。
   - **更新するとき**: Windows は終了して exe を置き換える。macOS は終了 → 置き換え → ③④ をもう一度 → アクセシビリティの一覧の古い CPFileLauncher を「−」で消して ⑤ をやり直す（ad-hoc 署名のため更新のたびに外れる、と理由も）。データは消えない（データフォルダのパス）。
   - **アンインストール**: アプリを消す + データフォルダ（`%APPDATA%\CPFileLauncher\` / `~/Library/Application Support/CPFileLauncher/`）+ Mac はアクセシビリティ・自動化の一覧から消す、自動起動をオフにしてから。
   - **困ったとき**: 「壊れているため開けません」→ `xattr` の手順 / トリガーが効かない（Mac）→ 一覧から消して付け直す / Intel Mac で「この Mac では使用できません」→ 非対応。
7. **06 // LICENSE**（`#license`）: 「CPFileLauncher — Copyright © 2026 PalmJoy — GPL-3.0-only」、無保証、ソースは GitHub、`LICENSE` へのリンク。**サードパーティ**: 主な部品を 1 行ずつ（Avalonia・SharpHook・SkiaSharp / HarfBuzzSharp・.NET = MIT、Skia・ANGLE = BSD-3-Clause、libuiohook = LGPL-3.0（ソースは各 Release の `libuiohook-source.zip`）、Chakra Petch・Share Tech Mono・Inter = OFL 1.1）と、全文として **`THIRD-PARTY-NOTICES.txt` へのリンク**（`https://github.com/PalmJoy/CPFileLauncher/blob/main/THIRD-PARTY-NOTICES.txt`）。**このサイト自体も Chakra Petch / Share Tech Mono を使うので `assets/fonts/OFL-*.txt` へのリンクも置く**。「Windows は Microsoft、macOS・Finder は Apple の商標」の 1 行。
8. **フッター**: `© 2026 PalmJoy` · GitHub · Issues · License · Third-party notices。

**`site.js` の仕様**（依存なし、`defer`、目安 200 行以下。定数（リポジトリ名・アセット名）は先頭に 1 か所）

- 言語の自動選択: 英語ページの `<head>` に**インラインの小さなスクリプト**（読み込み前のちらつき防止）で、`localStorage['cpfl.lang']` が無く `navigator.languages[0]` が `ja` で始まるときだけ `location.replace('ja/' + location.hash)`。日本語ページは自動で移らない。言語リンクを押すと保存。
- OS の推定 → `win-x64` / `win-arm64` / `macos-arm64` / null（SPEC §10.4「おすすめの表示」の規則）。結果でヒーローのボタン（`href` と文言）・コンソール行・カードの `is-recommended`・セットアップの初期タブを決める。
- 最新版: `fetch('https://api.github.com/repos/PalmJoy/CPFileLauncher/releases/latest')`、`sessionStorage['cpfl.latest']` に `{ at, tag, published, sizes }` を 1 時間。`[data-version]` `[data-date]`（ページの言語で `toLocaleDateString`）`[data-size="<アセット名>"]`（MB 小数 1 桁）を埋める。失敗・404（リリース前）なら要素を `hidden` のまま。
- 配色の見本: `document.documentElement.style.setProperty('--c1' …)`。
- 模型の時計: 分の境目に合わせて更新（`setTimeout` → 60 s ごと）。
- `<head>` には `lang`、`<link rel="alternate" hreflang="en|ja|x-default">`、`description`、OGP（`og:title` `og:description` `og:image` = `assets/og.png` の絶対 URL、`twitter:card = summary_large_image`）、`theme-color` = 背景色、favicon = `assets/icon.svg`。

#### D のタスク

- [x] **D1** `docs/` の骨組み: `.nojekyll`、フォント・アイコン・OFL のコピー、`site.css`（変数・書体・格子・八角形・発光・チップ・ボタン・カード・タブ・reduced-motion）。
- [x] **D2** `index.html`（英語）を上の構成で。JS なしで全節が読めること・ボタンが固定 URL で効くことを先に作り、その上に `site.js`。
- [x] **D3** `site.js`（上の仕様）。
- [x] **D4** `ja/index.html`（D2 の訳。アプリの表示と同じ語 = `spec/TERMS.md`、macOS の画面の語は「プライバシーとセキュリティ」「このまま開く」「完了」、Windows は「詳細情報」「実行」「許可する」）。
- [x] **D5**（S1 との見比べは未）盤面の模型と機能カードの標本（HTML/CSS）。模型の見た目をアプリのスクリーンショット（S1）と見比べて大きく外れないこと。
- [ ] **D6** U4 の後: `assets/screens/` に置き（長辺 1600 px 以下、`sips -Z 1600`、必要なら `pngquant`）、03 の節と `og.png` を作る。alt 文を英日で。
- [ ] **D7**（2026-10-12: Chrome の 1440 px / 390 px で表示だけ確認。Safari / Firefox・JS 無効・reduced-motion は未）手元確認: `python3 -m http.server -d docs 8000`（`docs/` を公開元と同じ相対構成で見る。`/CPFileLauncher/` 配下の再現は `python3 -m http.server` をリポジトリ親で動かすか、Pages 公開後に見る）。Chrome / Safari / Firefox、幅 375 px と 1440 px、JS 無効、reduced-motion、英日の切替と自動選択、配色の見本、API 失敗時（オフライン）に崩れない。

### E. 文書

- [x] **E1 CLAUDE.md**（メインが更新）: 「フォルダ運用ルール」に `docs/` — 配布サイト（GitHub Pages の公開元、`main` の `/docs`）。仕様ではない（仕様は `spec/`）。HTML/CSS/JS を直接置く、ビルド工程なし。中身の設計は SPEC §10.4「配布サイト」。`bin/release/` — 配布用 zip（`-Zip` / `--zip`）も 1 行。「リリースの手順」（`<Version>` を上げてコミット → `git tag vX.Y.Z` → `git push origin main vX.Y.Z` → 下書きを確認して公開）を「実装時の約束」か新しい小節に。
- [ ] E2 `issue/M6_DISTRIBUTION_WINDOWS.md` の CI のタスクはこの issue に移した（更新済み、2026-10-11）。
- [ ] E3 リリース後に SPEC §13.3 C31 / C32 の結果を記録し、§10.4 のサイズ（zip のサイズ）を追記。

## スクリーンショット（U4。撮るのはユーザー、配置と加工はメイン）

- 撮る前に、設定 → データの**エクスポートで今の盤面を退避**し、見せてよいアイテム（OS 標準のアプリ・よくあるフォルダ。個人名・社内のパスが映らない）で 15〜20 件のデモ盤面を作る。撮り終えたらインポートで戻す。
- Mac は ⌘⇧4 → Space → 窓をクリック（⌥を押しながらクリックで影なし）。盤面の透過の余白と発光が PNG の透明として残るので、サイトの暗い背景にそのまま載る。Windows は Win+Shift+S のウィンドウ切り取り。
- 撮影リスト:

  | # | 内容 | 用途 | 必須 |
  |---|---|---|---|
  | S1 | 盤面（ポップアップ、シアン、既定の演出、Mac Retina） | 03 の 1 枚目、`og.png` | 必須 |
  | S2 | S1 と同じ盤面をレッド・グリーン（2 枚） | 03（配色） | 任意 |
  | S3 | 設定画面（表示タブ） | 03 | 任意 |
  | S4 | Windows 11 の盤面（通知領域の近く） | 03（Windows でも動く） | 任意 |
  | S5 | 盤面の出入り（グリッチ）の 3〜5 秒の画面収録（mp4、H.264、無音、2 MB 以下） | ヒーロー横の差し替え候補 | 任意（無ければ HTML の模型のまま） |

- ファイル名 `s1-board-cyan.png` のように英小文字。メインに渡すだけでよい。

## 確認手順（リリースの通し）

1. B2 の手動実行が緑、Artifact の zip で C31 (1)・C32 (1) を手元で。
2. `<Version>` を上げてコミット → タグ push → 4 ジョブが緑、下書きの Release に zip 3 つ + `SHA256SUMS.txt`。タグと `<Version>` をわざと違えたタグ（試験用、すぐ消す）で `check-version` が落ちることも 1 度見る。
3. 下書きのアセットを両 OS で落として C31 (2)〜(4)・C32 (2)〜(4)。サイトの手順の文言と実際の画面の文言を突き合わせて直す。
4. 公開（Publish release）→ サイトの 3 つのボタンが落とせる・版と日付・サイズが出る・`SHA256SUMS.txt` のリンクが効く。
5. 次の版（`<Version>` だけ上げる）で更新の手順（Mac の許可の付け直し）が書いたとおり。

## 完了条件

- タグ push で両 OS のテスト → zip → 下書きの Release が自動で作られる（手動実行で試運転もできる）。ワークフローに `dotnet publish` が無い。
- 最初のリリースが公開され、アセット 4 つが固定名で `releases/latest/download/…` から落とせる。
- `https://palmjoy.github.io/CPFileLauncher/`（英）と `/ja/` が公開され、ボタン・版の表示・おすすめ・初回起動の手順・ライセンス表記（GPL と OFL）がある。少なくとも S1 が載っている。
- 「について」に GPL-3.0-only・Copyright © 2026 PalmJoy・ソースの URL・「サードパーティのライセンス」がある。配布物に `LICENSE.txt` と `THIRD-PARTY-NOTICES.txt` があり、Windows の配布物では `uiohook.dll` が exe の隣にあって exe の束に入っていない（C32 (6)）。Release に `libuiohook-source.zip` がある。`ThirdPartyNoticesTests` が緑。
- README にサイトへのリンクがある。C31 / C32 の結果が SPEC に記録され、CLAUDE.md に `docs/` とリリースの手順がある。→ この issue を削除。

## 要ユーザー判断（残り）

1. （任意）「GPL-3.0-only」を将来「v3 以降」に広げるか。今は未回答のため「のみ」で確定扱い。広げるのはいつでもできる（「について」・サイト・notices の表記を変えるだけ）。
