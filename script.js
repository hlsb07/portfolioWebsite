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

  const cacheKey = `gh-repos:${user}:${max}`;
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
    const stars = r.stars ? `<span class="repo-stars"><span class="star">★</span> ${r.stars}</span>` : '';
    const home = r.homepage ? `<a href="${escapeAttr(r.homepage)}" target="_blank" rel="noopener noreferrer"><i class="fa-solid fa-arrow-up-right-from-square"></i> Live Demo</a>` : '';
    return `
      <article class="github-featured-card">
        <div class="repo-head"><a href="${escapeAttr(r.html_url)}" target="_blank" rel="noopener noreferrer">${escapeHTML(r.name)}</a></div>
        <div class="repo-desc">${desc}</div>
        <div class="repo-stats">${lang}${stars}${home}<span>Aktualisiert ${upd}</span></div>
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

// Scroll Animations mit Intersection Observer
(function () {
  const hiddenElements = document.querySelectorAll('.hidden');

  const observer = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
      if (entry.isIntersecting) {
        entry.target.classList.add('show');
      } else {
        // Remove 'show' class to allow animations to repeat when scrolling back
        entry.target.classList.remove('show');
      }
    });
  }, {
    threshold: 0.1, // Trigger when 10% of element is visible
    rootMargin: '0px 0px -50px 0px' // Start animation slightly before element enters viewport
  });

  hiddenElements.forEach((el) => observer.observe(el));
})();

// Hero animation timeline via Web Animations API
(function () {
  const heroIcons = document.querySelector('.hero-icons');
  if (!heroIcons) return;

  const factory = heroIcons.querySelector('.hero-icon-factory');
  const gear = heroIcons.querySelector('.hero-icon-gear');
  const code = heroIcons.querySelector('.hero-icon-code');
  const progress = heroIcons.querySelector('.hero-path-progress');
  const dots = heroIcons.querySelectorAll('.hero-path-dot');

  const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');

  const setStaticState = () => {
    [factory, gear, code].forEach((icon) => {
      if (!icon) return;
      icon.style.opacity = '1';
      icon.style.removeProperty('animation');
      icon.style.removeProperty('transform');
    });
    if (progress) {
      progress.style.transform = 'scaleX(1)';
    }
    dots.forEach((dot) => {
      dot.style.opacity = '1';
      dot.style.transform = 'scale(1)';
    });
  };

  if (prefersReducedMotion.matches) {
    setStaticState();
    return;
  }

  let running = false;

  const playHeroAnimation = () => {
    if (running) return;
    running = true;

    const promises = [];
    const store = (animation) => {
      if (!animation) return;
      promises.push(animation.finished.catch(() => {}));
    };

    if (factory) {
      store(
        factory.animate(
          [
            { transform: 'translateX(-220px) rotate(2deg)', opacity: 0 },
            { transform: 'translateX(-80px) rotate(2deg)', opacity: 1, offset: 0.5 },
            { transform: 'translateX(-15px) rotate(10deg)', offset: 0.75 },
            { transform: 'translateX(0px) rotate(2deg)' }
          ],
          { duration: 1600, easing: 'cubic-bezier(0.34, 1.56, 0.64, 1)', fill: 'forwards' }
        )
      );
    }

    if (gear) {
      store(
        gear.animate(
          [
            { transform: 'translateX(-50px) rotate(-6deg)', opacity: 0 },
            { transform: 'translateX(-20px) rotate(160deg)', opacity: 1, offset: 0.3 },
            { transform: 'translateX(-5px) rotate(320deg)', offset: 0.55 },
            { transform: 'translateX(8px) rotate(520deg)', offset: 0.8 },
            { transform: 'translateX(0px) rotate(620deg)' }
          ],
          {
            duration: 1500,
            delay: 350,
            easing: 'cubic-bezier(0.68, -0.55, 0.265, 1.55)',
            fill: 'forwards'
          }
        )
      );
    }

    if (code) {
      store(
        code.animate(
          [
            { transform: 'translateX(-120px) rotate(-3deg)', opacity: 0 },
            { transform: 'translateX(-70px) rotate(45deg)', opacity: 1, offset: 0.35 },
            { transform: 'translateX(-10px) rotate(-12deg)', offset: 0.65 },
            { transform: 'translateX(10px) rotate(6deg)', offset: 0.85 },
            { transform: 'translateX(0px) rotate(-3deg)' }
          ],
          {
            duration: 1400,
            delay: 900,
            easing: 'cubic-bezier(0.34, 1.56, 0.64, 1)',
            fill: 'forwards'
          }
        )
      );
    }

    if (progress) {
      store(
        progress.animate(
          [
            { transform: 'scaleX(0)' },
            { transform: 'scaleX(1)' }
          ],
          { duration: 1400, delay: 700, easing: 'cubic-bezier(0.22, 1, 0.36, 1)', fill: 'forwards' }
        )
      );
    }

    dots.forEach((dot, index) => {
      store(
        dot.animate(
          [
            { opacity: 0, transform: 'scale(0.5)' },
            { opacity: 1, transform: 'scale(1.2)', offset: 0.7 },
            { opacity: 1, transform: 'scale(1)' }
          ],
          {
            duration: 500,
            delay: 900 + index * 200,
            easing: 'cubic-bezier(0.34, 1.56, 0.64, 1)',
            fill: 'forwards'
          }
        )
      );
    });

    if (promises.length === 0) {
      running = false;
      return;
    }

    Promise.all(promises).finally(() => {
      running = false;
    });
  };

  const heroObserver = new IntersectionObserver(
    (entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          playHeroAnimation();
        }
      });
    },
    { threshold: 0.5 }
  );

  heroObserver.observe(heroIcons);

  prefersReducedMotion.addEventListener?.('change', (event) => {
    if (event.matches) {
      heroObserver.disconnect();
      setStaticState();
    }
  });
})();
