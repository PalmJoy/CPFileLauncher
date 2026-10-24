import { initializeApp } from 'https://www.gstatic.com/firebasejs/13.0.0/firebase-app.js';
import { getAnalytics, isSupported, logEvent } from 'https://www.gstatic.com/firebasejs/13.0.0/firebase-analytics.js';

const firebaseConfig = {
  apiKey: 'AIzaSyAG1mJUN96IQztYl49KUneIskvXmhRvD5E',
  authDomain: 'cpfilelauncher.firebaseapp.com',
  projectId: 'cpfilelauncher',
  storageBucket: 'cpfilelauncher.firebasestorage.app',
  messagingSenderId: '398881184018',
  appId: '1:398881184018:web:c086cc7fb384e88155fbcb',
  measurementId: 'G-2VWQWZKZC4'
};

try {
  if (await isSupported()) {
    const app = initializeApp(firebaseConfig);
    const analytics = getAnalytics(app);

    document.querySelectorAll('.dl-item[data-asset] a.btn, [data-hero-download]').forEach((link) => {
      link.addEventListener('click', () => {
        const card = link.closest('.dl-item');
        const href = link.href || '';
        const platform = card?.dataset.asset ||
          Object.keys({ 'win-x64': 1, 'win-arm64': 1, 'macos-arm64': 1 })
            .find((name) => href.includes(name));
        if (!platform) return;

        logEvent(analytics, 'download_click', {
          platform,
          language: document.documentElement.lang || 'en',
          placement: card ? 'platform_card' : 'hero'
        });
      });
    });
  }
} catch (error) {
  // Analytics must not affect the site's download links or other behavior.
  console.warn('Firebase Analytics could not be initialized.', error);
}
