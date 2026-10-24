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
    const assets = Object.keys(names).map(name => (release.assets || []).find(asset => asset.name === name)).filter(Boolean);
    if (!assets.length) return;
    const urls = {}, cards = {};
    function element(tag, className, text) {
      const node = document.createElement(tag);
      node.className = className;
      if (text) node.textContent = text;
      return node;
    }
    const requirements = ja
      ? {'win-x64':'Windows 10（1809以降）/ 11。Intel・AMDのPC。','win-arm64':'ARMのPC（Snapdragonなど）のWindows 11。','macos-arm64':'macOS 15以降。Apple silicon（M1以降）専用。Intel Macには対応していません。'}
      : {'win-x64':'Windows 10 (1809+) / 11 on Intel and AMD PCs.','win-arm64':'Windows 11 on ARM PCs, including Snapdragon.','macos-arm64':'macOS 15+. Apple silicon (M1 or later) only. Intel Macs are not supported.'};
    for (const asset of assets) {
      const url = new URL(asset.browser_download_url);
      if (url.origin !== 'https://github.com' || !url.pathname.startsWith('/PalmJoy/CPFileLauncher/releases/download/')) continue;
      urls[asset.name] = url.href;
      const rid = asset.name.slice('CPClipboard-'.length, -4);
      const item = element('div', 'dl-item'); item.setAttribute('data-asset', rid);
      const card = element('div', 'dl-card cut');
      const chips = element('div', 'chips');
      chips.appendChild(element('span', 'chip', rid === 'macos-arm64' ? 'Apple silicon' : rid === 'win-arm64' ? 'ARM64' : 'x64'));
      chips.appendChild(element('span', 'chip c2 rec', ja ? 'おすすめ' : 'Recommended'));
      card.appendChild(chips);
      card.appendChild(element('h3', '', names[asset.name].split(' · ')[0] + (rid === 'win-arm64' ? ' (ARM)' : '')));
      card.appendChild(element('div', 'file', asset.name + (asset.size ? ` · ${(asset.size / 1048576).toFixed(1)} MB` : '')));
      card.appendChild(element('p', 'req', requirements[rid]));
      const a = element('a', 'btn'); a.href = url.href;
      a.appendChild(element('span', 'cut', ja ? '↓ ダウンロード' : '↓ Download'));
      card.appendChild(a); item.appendChild(card); downloads.appendChild(item); cards[rid] = item;
    }
    if (downloads.children.length) status.textContent = (ja ? '公開版: ' : 'Latest release: ') + release.tag_name;
    document.querySelectorAll('[data-version]').forEach(node => { node.textContent = release.tag_name.replace('cpclipboard-', ''); });
    const releaseLink = document.querySelector('[data-release-link]');
    if (releaseLink) releaseLink.href = 'https://github.com/PalmJoy/CPFileLauncher/releases/tag/' + release.tag_name;
    const platform = await detectPlatform();
    if (cards[platform]) cards[platform].className += ' is-recommended';
    const target = document.querySelector('[data-console-target]');
    if (target && platform) target.textContent = platform;
    const radio = document.getElementById(platform === 'macos-arm64' ? 'os-mac' : 'os-win');
    if (radio && platform) radio.checked = true;
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
