// Shared palette controls and the board clock; all elements are optional.
(function () {
  document.querySelectorAll('.swatch').forEach(button => {
    button.addEventListener('click', () => {
      document.documentElement.style.setProperty('--c1', button.getAttribute('data-c1'));
      document.documentElement.style.setProperty('--c2', button.getAttribute('data-c2'));
      document.querySelectorAll('.swatch').forEach(other => other.setAttribute('aria-pressed', String(other === button)));
    });
  });
  const clock = document.querySelector('[data-clock]');
  if (clock) {
    const pad = value => String(value).padStart(2, '0');
    function tick() {
      const now = new Date();
      clock.textContent = `${now.getFullYear()}/${pad(now.getMonth()+1)}/${pad(now.getDate())} ${pad(now.getHours())}:${pad(now.getMinutes())}`;
      setTimeout(tick, 60000 - (now.getSeconds()*1000 + now.getMilliseconds()) + 20);
    }
    tick();
  }
})();
