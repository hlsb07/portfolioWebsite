// Kleine Helfer für Navigation, Scroll‑Status und Theme

(function () {
  const doc = document;

  // Jahr im Footer
  const y = doc.getElementById('year');
  if (y) y.textContent = String(new Date().getFullYear());

  // Mobile Navigation
  const toggle = doc.querySelector('.nav-toggle');
  const nav = doc.getElementById('nav-list');
  if (toggle && nav) {
    toggle.addEventListener('click', () => {
      const open = nav.classList.toggle('open');
      toggle.setAttribute('aria-expanded', String(open));
      toggle.setAttribute('aria-label', open ? 'Menü schließen' : 'Menü öffnen');
    });
    nav.addEventListener('click', (e) => {
      const t = e.target;
      if (t instanceof HTMLElement && t.tagName === 'A') {
        nav.classList.remove('open');
        toggle.setAttribute('aria-expanded', 'false');
      }
    });
  }

  // Active‑Link beim Scrollen (IntersectionObserver)
  const links = Array.from(doc.querySelectorAll('.nav-list a'));
  const sections = links
    .map((a) => doc.querySelector(a.getAttribute('href')))
    .filter((el) => el instanceof HTMLElement);

  const activate = (id) => {
    links.forEach((a) => a.classList.toggle('active', a.getAttribute('href') === `#${id}`));
  };

  const io = new IntersectionObserver(
    (entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) activate(entry.target.id);
      });
    },
    { rootMargin: '-40% 0px -55% 0px', threshold: 0.01 }
  );
  sections.forEach((el) => io.observe(el));

  // Theme Toggle (Light/Dark) mit LocalStorage
  const btnTheme = doc.querySelector('.theme-toggle');
  const key = 'pref-theme';
  const root = doc.documentElement;
  const setTheme = (mode) => {
    root.setAttribute('data-theme', mode);
    localStorage.setItem(key, mode);
    const icon = btnTheme?.querySelector('.theme-icon');
    if (icon) icon.textContent = mode === 'dark' ? '☾' : '☀︎';
  };

  const saved = localStorage.getItem(key);
  const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
  setTheme(saved || (prefersDark ? 'dark' : 'light'));

  btnTheme?.addEventListener('click', () => {
    setTheme(root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark');
  });
})();

// GitHub Repository Übersicht – stilvolle Karten
(function () {
  const container = document.getElementById('gh-repos');
  if (!container) return;

  const user = container.getAttribute('data-github-user');
  const max = parseInt(container.getAttribute('data-max') || '6', 10);
  if (!user || user === 'data-github-user') {
    container.innerHTML = '<p class="muted">Hinweis: Setze dein GitHub‑Nutzername in data-github-user, um Repos zu laden.</p>';
    return;
  }

  const cacheKey = `gh-repos:${user}`;
  const cache = localStorage.getItem(cacheKey);
  if (cache) {
    try {
      const parsed = JSON.parse(cache);
      if (Date.now() - parsed.t < 1000 * 60 * 60) { // 1h Cache
        render(parsed.d);
        return;
      }
    } catch {}
  }

  fetch(`https://api.github.com/users/${encodeURIComponent(user)}/repos?per_page=100&sort=updated`, {
    headers: {
      'Accept': 'application/vnd.github+json',
      'X-GitHub-Api-Version': '2022-11-28'
    }
  })
    .then((r) => {
      if (!r.ok) throw new Error(`GitHub API Fehler: ${r.status}`);
      return r.json();
    })
    .then((repos) => {
      const list = repos
        .filter((r) => !r.fork)
        .sort((a, b) => (b.stargazers_count || 0) - (a.stargazers_count || 0) || new Date(b.pushed_at) - new Date(a.pushed_at))
        .slice(0, max)
        .map((r) => ({
          name: r.name,
          html_url: r.html_url,
          description: r.description,
          language: r.language,
          stars: r.stargazers_count,
          homepage: r.homepage,
          updated: r.pushed_at
        }));
      localStorage.setItem(cacheKey, JSON.stringify({ t: Date.now(), d: list }));
      render(list);
    })
    .catch(() => {
      container.innerHTML = '<p class="muted">Konnte GitHub‑Daten nicht laden. Bitte später erneut versuchen.</p>';
    });

  function render(list) {
    if (!Array.isArray(list) || list.length === 0) {
      container.innerHTML = '<p class="muted">Keine Repositories gefunden.</p>';
      return;
    }
    container.innerHTML = list
      .map((r) => card(r))
      .join('');
  }

  function card(r) {
    const updated = new Date(r.updated);
    const upd = updated.toLocaleDateString(undefined, { year: 'numeric', month: 'short' });
    const desc = r.description ? escapeHTML(r.description) : 'Ohne Beschreibung.';
    const lang = r.language ? `<span class="repo-lang"><span class="repo-dot"></span>${escapeHTML(r.language)}</span>` : '';
    const stars = r.stars ? `<span class="repo-stars"><span class="star">★</span>${r.stars}</span>` : '';
    const home = r.homepage ? `<a href="${escapeAttr(r.homepage)}" target="_blank" rel="noopener noreferrer">Live</a>` : '';
    return `
      <article class="card repo-card">
        <div class="repo-head"><a href="${escapeAttr(r.html_url)}" target="_blank" rel="noopener noreferrer">${escapeHTML(r.name)}</a></div>
        <div class="repo-desc">${desc}</div>
        <div class="repo-stats">${lang}${stars ? `<span>·</span>${stars}` : ''}<span>·</span><span>Aktualisiert ${upd}</span>${home ? `<span>·</span>${home}` : ''}</div>
      </article>`;
  }

  function escapeHTML(s) {
    return String(s).replace(/[&<>"']/g, (c) => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;','\'':'&#39;'}[c]));
  }
  function escapeAttr(s) { return escapeHTML(s).replace(/\n/g, ''); }
})();

// Entferne Kontaktbereich und Links auf #kontakt, um tote Anker zu vermeiden
(function () {
  document.querySelectorAll('a[href="#kontakt"]').forEach((a) => a.remove());
  const sec = document.getElementById('kontakt');
  if (sec) sec.remove();
})();

