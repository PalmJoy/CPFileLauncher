# サイバーパンク装飾のキャプチャ

2026-10-19。テスト用データを実際の BoardWindow に表示し、Avalonia Headless + Skia で描画した PNG。OS のデスクトップのスクリーンショットではない。アイコンは OS 取得を行わず、通常の頭文字プレースホルダーを使う。

- 01-target-lock-hover.png / idle.png: ホバー照準あり・なし。
- 02-launch-signal.png: 起動成功通知を呼んだ後、輪郭の光点が回る途中（38%）。
- 03-circuit-pattern.png: 背景四隅の回路。
- 04-frame-markings.png: 枠外の刻印。
- 05-page-sweep.png: 実際のページ切替後、スイープ途中（45%）。
- 06-registration-scan.png: 登録フィードバックを呼んだ後、スキャン途中（55%）。
- 07-hologram-edges.png: 縁の一部に主色・副色の微小なずれ。

キャプチャ生成: `CPFL_CAPTURE_DIR="$PWD/work/captures/cyberpunk" dotnet test src/FileLauncher.Desktop.Tests/FileLauncher.Desktop.Tests.csproj --no-restore --filter FullyQualifiedName~CyberpunkCaptureTests`

演出途中の静止画は進捗を指定して同じ描画処理から生成。クリックによる外部アプリの起動は行わない。各ファイルはテスト再実行時に現在の装飾を反映して更新される。
