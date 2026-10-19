# CPFileLauncher

ホットキーやマウスで呼び出す、ネオンの盤面のファイル / アプリランチャー（Windows / macOS）。
A cyberpunk file & app launcher for Windows and macOS, summoned with a hotkey or a mouse click.

- 配布サイト / Website: https://palmjoy.github.io/CPFileLauncher/
- ダウンロード / Download: [最新のリリース / Latest release](https://github.com/PalmJoy/CPFileLauncher/releases/latest)（Windows x64 / ARM64、macOS Apple シリコン）
- ライセンス / License: GPL-3.0-only（[LICENSE](LICENSE)）。Copyright © 2026 PalmJoy。第三者の部品 / Third-party components: [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt)

## 開発者向け

### 配布サイトの検索登録

サイトには日英それぞれの正規 URL（canonical）と言語別 URL（hreflang）を設定し、`docs/sitemap.xml` に両ページを記載している。ページ追加・URL 変更時はこれらも更新する。

公開後、Google Search Console で次の操作を行う。

1. URL プレフィックス `https://palmjoy.github.io/CPFileLauncher/` をプロパティとして追加する。
2. 所有権確認を行う。HTML ファイル方式なら Google が発行したファイルを `docs/` に配置して公開する。メタタグ方式なら発行された `google-site-verification` タグを `docs/index.html` の `<head>` に追加して公開する。確認用ファイル・タグは確認後も残す。
3. サイトマップ `https://palmjoy.github.io/CPFileLauncher/sitemap.xml` を送信する。
4. 英語トップと日本語トップをそれぞれ URL 検査し、インデックス登録をリクエストする。

所有権確認には Google が発行する値が必要なため、仮の確認タグは入れない。GitHub Pages のプロジェクト配下の `robots.txt` はホスト直下の設定として機能しないため、このサイトでは Search Console からサイトマップを送信する。検索掲載・順位・反映時期は保証されない。

参考: [Google のサイトマップ作成・送信ガイド](https://developers.google.com/search/docs/crawling-indexing/sitemaps/build-sitemap?hl=ja)

フォルダ構成
- spec
  - アプリ仕様のドキュメント。常に最新のものだけの状態にし、古いものは残さない。
- issue
  - 進行中の課題ドキュメント。終了したタスクは消していく。
- src
  - ソースコード等の実装環境。
- docs
  - 配布サイト（GitHub Pages の公開元）。仕様ではない。
- bin
  - ビルドされたarch毎の実行ファイル（`bin/release/` は配布用の zip）

リリースビルド: Windows は `build-release.bat`、Mac は `./build-release.sh`（`-Zip` / `--zip` で zip も作る）。
