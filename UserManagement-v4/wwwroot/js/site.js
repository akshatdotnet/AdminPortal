/* ══════════════════════════════════════════════════════════════════════════════
   site.js  –  UserHub client-side behaviour
   Responsibilities:
     1. Theme switching  (dark / light / sepia, persisted to localStorage)
     2. Sidebar mobile   (open / close via overlay)
     3. Alert auto-dismiss (4 s)
     4. Delete confirmation dialog
   ══════════════════════════════════════════════════════════════════════════════ */

document.addEventListener('DOMContentLoaded', function () {

    /* ── 1. THEME SWITCHER ───────────────────────────────────────────────────
       Reads / writes 'uh-theme' in localStorage.
       Stamps [data-theme] on <html> (matched by CSS custom properties).
       The inline <script> in <head> also calls this at load time to
       prevent a colour flash before DOMContentLoaded fires.
    ─────────────────────────────────────────────────────────────────────── */
    var THEME_KEY    = 'uh-theme';
    var VALID_THEMES = ['dark', 'light', 'sepia'];

    function applyTheme(theme) {
        if (VALID_THEMES.indexOf(theme) === -1) theme = 'dark';
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem(THEME_KEY, theme);

        document.querySelectorAll('.theme-btn').forEach(function (btn) {
            btn.classList.toggle('active', btn.dataset.theme === theme);
            btn.setAttribute('aria-pressed', btn.dataset.theme === theme ? 'true' : 'false');
        });
    }

    /* Restore saved theme (or default to dark) */
    applyTheme(localStorage.getItem(THEME_KEY) || 'dark');

    /* Wire each theme button */
    document.querySelectorAll('.theme-btn').forEach(function (btn) {
        btn.addEventListener('click', function () { applyTheme(this.dataset.theme); });
    });

    /* ── 2. SIDEBAR – MOBILE OPEN / CLOSE ────────────────────────────────────
       The sidebar is fixed and off-screen on mobile (transform: translateX(-100%)).
       Toggle .open on the sidebar and .show on the backdrop overlay.
    ─────────────────────────────────────────────────────────────────────── */
    var sidebar  = document.getElementById('sidebar');
    var overlay  = document.getElementById('overlay');
    var openBtn  = document.getElementById('sidebarOpen');
    var closeBtn = document.getElementById('sidebarClose');

    function openSidebar() {
        sidebar?.classList.add('open');
        overlay?.classList.add('show');
        openBtn?.setAttribute('aria-expanded', 'true');
    }

    function closeSidebar() {
        sidebar?.classList.remove('open');
        overlay?.classList.remove('show');
        openBtn?.setAttribute('aria-expanded', 'false');
    }

    openBtn?.addEventListener('click', openSidebar);
    closeBtn?.addEventListener('click', closeSidebar);
    overlay?.addEventListener('click', closeSidebar);

    /* Close sidebar on Escape key */
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') closeSidebar();
    });

    /* ── 3. ALERT AUTO-DISMISS ───────────────────────────────────────────────
       Fade out success / error flash messages after 4 seconds.
    ─────────────────────────────────────────────────────────────────────── */
    setTimeout(function () {
        document.querySelectorAll('.alert').forEach(function (el) {
            var inst = window.bootstrap?.Alert.getOrCreateInstance(el);
            inst?.close();
        });
    }, 4000);

    /* ── 4. DELETE CONFIRMATION ──────────────────────────────────────────────
       Forms with class .confirm-delete show a browser confirm dialog
       before submitting. Set data-name on the form for a personalised message.
    ─────────────────────────────────────────────────────────────────────── */
    document.querySelectorAll('.confirm-delete').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            var name = this.dataset.name ? '"' + this.dataset.name + '"' : 'this record';
            if (!confirm('Delete ' + name + '?\n\nThis action cannot be undone.')) {
                e.preventDefault();
            }
        });
    });

});
