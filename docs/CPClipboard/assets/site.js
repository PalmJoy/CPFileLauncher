import {selectRelease} from './release-selection.js';
// 公開済み正式版の、実在する配布アセットだけを表示する。
(async function () {
  'use strict';
  const ja = document.documentElement.lang === 'ja';
  const status = document.querySelector('[data-release-status]');
  const downloads = document.querySelector('[data-downloads]');
  const hero = document.querySelector('[data-hero-download]');
  const heroLabel = document.querySelector('[data-hero-label]');
  const other = document.querySelector('[data-hero-other]');
  async function detectPlatform() {
    const data = navigator.userAgentData;
    const platform = data?.platform || navigator.platform || '';
    const ua = navigator.userAgent || '';
    if (/iPhone|iPad|iPod|Android/i.test(ua)) return null;
    if (/Win/i.test(platform) || /Windows/i.test(ua)) {
      try {
        const values = await data?.getHighEntropyValues?.(['architecture']);
        return values?.architecture === 'arm' ? 'win-arm64' : 'win-x64';
      } catch (_) { return 'win-x64'; }
    }
    // Browsers do not reliably distinguish Intel Macs from Apple silicon.
    if (/Mac/i.test(platform) || /Macintosh/i.test(ua)) return 'macos-arm64';
    return null;
  }
  try {
    const response = await fetch('https://api.github.com/repos/PalmJoy/CPFileLauncher/releases?per_page=100', {cache:'no-store',headers:{Accept:'application/vnd.github+json'}});
    if (response.status === 404) return;
    if (!response.ok) throw new Error('release lookup');
    const release = selectRelease(await response.json(), 'cpclipboard-v');
    if (!release) return;
    const names = {'CPClipboard-win-x64.zip':'Windows · x64','CPClipboard-win-arm64.zip':'Windows · ARM64','CPClipboard-macos-arm64.zip':'macOS · Apple silicon'};
    const assets = (release.assets || []).filter(a => names[a.name]);
    if (!assets.length) return;
    const urls = {};
    for (const asset of assets) {
      const url = new URL(asset.browser_download_url);
      if (url.origin !== 'https://github.com' || !url.pathname.startsWith('/PalmJoy/CPFileLauncher/releases/download/')) continue;
      urls[asset.name] = url.href;
      const a = document.createElement('a'); a.href = url.href;
      a.textContent = names[asset.name] + ' ↓ ZIP'; downloads.appendChild(a);
    }
    if (downloads.children.length) status.textContent = (ja ? '公開版: ' : 'Latest release: ') + release.tag_name;
    const platform = await detectPlatform();
    const file = platform && `CPClipboard-${platform}.zip`;
    if (hero && urls[file]) {
      hero.href = urls[file];
      const labels = ja
        ? {'win-x64':'Windows版をダウンロード', 'win-arm64':'Windows（ARM64）版をダウンロード', 'macos-arm64':'macOS版をダウンロード（Apple silicon専用）'}
        : {'win-x64':'Download for Windows', 'win-arm64':'Download for Windows on ARM', 'macos-arm64':'Download for macOS (Apple silicon only)'};
      if (heroLabel) heroLabel.textContent = labels[platform];
      if (other) other.hidden = false;
    }
  } catch (_) {
    status.textContent = ja ? '公開版の情報を取得できません。GitHubのリリース一覧をご確認ください。' : 'Release information is unavailable. Please check the GitHub releases page.';
  }
})();
