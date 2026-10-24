import {selectRelease} from './release-selection.js';
// 公開済み正式版の、実在する配布アセットだけを表示する。
(async function () {
  'use strict';
  const ja = document.documentElement.lang === 'ja';
  const status = document.querySelector('[data-release-status]');
  const downloads = document.querySelector('[data-downloads]');
  try {
    const response = await fetch('https://api.github.com/repos/PalmJoy/CPFileLauncher/releases?per_page=100', {cache:'no-store',headers:{Accept:'application/vnd.github+json'}});
    if (response.status === 404) return;
    if (!response.ok) throw new Error('release lookup');
    const release = selectRelease(await response.json(), 'cpclipboard-v');
    if (!release) return;
    const names = {'CPClipboard-win-x64.zip':'Windows · x64','CPClipboard-win-arm64.zip':'Windows · ARM64','CPClipboard-macos-arm64.zip':'macOS · Apple silicon'};
    const assets = (release.assets || []).filter(a => names[a.name]);
    if (!assets.length) return;
    for (const asset of assets) {
      const url = new URL(asset.browser_download_url);
      if (url.origin !== 'https://github.com' || !url.pathname.startsWith('/PalmJoy/CPFileLauncher/releases/download/')) continue;
      const a = document.createElement('a'); a.href = url.href;
      a.textContent = names[asset.name] + ' ↓ ZIP'; downloads.appendChild(a);
    }
    if (downloads.children.length) status.textContent = (ja ? '公開版: ' : 'Latest release: ') + release.tag_name;
  } catch (_) {
    status.textContent = ja ? '公開版の情報を取得できません。GitHubのリリース一覧をご確認ください。' : 'Release information is unavailable. Please check the GitHub releases page.';
  }
})();
