(() => {
  'use strict';

  const qs = (id) => document.getElementById(id);

  // Helpers to parse time strings like "05:40", "05:40:00", or "5:40 AM"
  function parsePrayerTimeToDate(t) {
    if (!t) return null;
    t = t.trim();
    const now = new Date();
    // Try HH:mm[:ss]
    let m = t.match(/^(\d{1,2}):(\d{2})(?::(\d{2}))?$/);
    if (m) {
      const h = parseInt(m[1], 10), min = parseInt(m[2], 10), s = parseInt(m[3] || '0', 10);
      return new Date(now.getFullYear(), now.getMonth(), now.getDate(), h, min, s, 0);
    }
    // Try h:mm AM/PM
    m = t.match(/^(\d{1,2}):(\d{2})\s*(AM|PM)$/i);
    if (m) {
      let h = parseInt(m[1], 10);
      const min = parseInt(m[2], 10);
      const ap = m[3].toUpperCase();
      if (ap === 'PM' && h < 12) h += 12;
      if (ap === 'AM' && h === 12) h = 0;
      return new Date(now.getFullYear(), now.getMonth(), now.getDate(), h, min, 0, 0);
    }
    return null;
  }

  function findNextPrayer() {
    const grid = qs('prayerGrid');
    if (!grid) return null;

    const cards = Array.from(grid.querySelectorAll('.prayer-card'));
    const now = new Date();
    const items = cards.map(c => ({
      el: c,
      name: c.getAttribute('data-name'),
      timeStr: c.getAttribute('data-time'),
      date: parsePrayerTimeToDate(c.getAttribute('data-time'))
    })).filter(x => !!x.date);

    items.sort((a, b) => a.date - b.date);

    const next = items.find(x => x.date > now);
    if (!next) return null;

    cards.forEach(c => c.classList.remove('next-prayer'));
    next.el.classList.add('next-prayer');

    return next;
  }

  function formatCountdown(ms) {
    if (ms < 0) ms = 0;
    const s = Math.floor(ms / 1000);
    const hh = Math.floor(s / 3600).toString().padStart(2, '0');
    const mm = Math.floor((s % 3600) / 60).toString().padStart(2, '0');
    const ss = (s % 60).toString().padStart(2, '0');
    return `${hh}:${mm}:${ss}`;
  }

  function startCountdown() {
    const badge = qs('nextBadge');
    const label = qs('nextLabel');
    const countdown = qs('countdown');
    if (!badge || !label || !countdown) return;

    function tick() {
      const next = findNextPrayer();
      if (!next) {
        badge.classList.add('d-none');
        return;
      }
      badge.classList.remove('d-none');
      label.textContent = `Seterusnya: ${next.name}`;
      const now = new Date();
      const diff = next.date - now;
      countdown.textContent = formatCountdown(diff);
    }

    tick();
    setInterval(tick, 1000);
  }

  // Expose for inline handlers in markup
  window.changeZone = function () {
    const select = qs('zoneSelect');
    if (!select) return;
    const selectedZone = select.value;
    if (!selectedZone) return;
    select.disabled = true;
    select.classList.add('disabled');
    window.location.href = `/?zone=${selectedZone}`;
  };

  window.goToday = function () {
    const url = new URL(window.location.href);
    const zoneSel = qs('zoneSelect');
    const zone = zoneSel?.value || '';
    url.searchParams.set('zone', zone);
    window.location.href = url.toString();
  };

  document.addEventListener('DOMContentLoaded', () => {
    // Start countdown if the grid exists
    if (qs('prayerGrid')) {
      startCountdown();
    }
  });
})();