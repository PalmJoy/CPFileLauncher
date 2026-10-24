# Common

両アプリが同じ実ファイルを使用する共通ソースです。`SharedItems.props` が製品と層を選び、既存assemblyにリンクしてコンパイルします。製品固有の設定・リソース・盤面処理は製品ディレクトリに置きます。

- `Core/`: 入力判定・幾何・保存・色・演出の計算。
- `Desktop/`: Avaloniaの枠・背景・HUD・演出・補助処理。
- `Platform*/`: OS境界ごとの共通補助。
- `Web/`: 製品別Releaseの選択。
- `build/`: 固定署名の選択、サイト同期、構成検証。
- `Tests/`: 両製品で実行する共通の検証。

C#の共有対象は `shared-sources.json`、コンパイル条件は `SharedItems.props`、全体の仕様は [SPEC.md](SPEC.md) を参照してください。変更後はCPToolsルートのビルドで両製品を検証します。
