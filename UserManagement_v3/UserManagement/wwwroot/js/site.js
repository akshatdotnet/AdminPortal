/* ══════════════════════════════════════════════════════════════════════════════
   site.js  –  Sidebar, Alerts, Delete confirm, Theme switcher
   ══════════════════════════════════════════════════════════════════════════════ */

document.addEventListener('DOMContentLoaded', function () {

    // ── 1. THEME SWITCHER ─────────────────────────────────────────────────────
    const THEME_KEY = 'uh-theme';
    const THEMES    = ['dark', 'light', 'sepia'];

    function applyTheme(theme) {
        if (!THEMES.includes(theme)) theme = 'dark';
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem(THEME_KEY, theme);

        // Sync active state on all theme buttons
        document.querySelectorAll('.theme-btn').forEach(function (btn) {
            btn.classList.toggle('active', btn.dataset.theme === theme);
        });
    }

    // On load: restore saved theme (default = dark)
    applyTheme(localStorage.getItem(THEME_KEY) || 'dark');

    // Wire up each theme button
    document.querySelectorAll('.theme-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            applyTheme(this.dataset.theme);
        });
    });

    // ── 2. SIDEBAR MOBILE TOGGLE ──────────────────────────────────────────────
    var sidebar  = document.getElementById('sidebar');
    var overlay  = document.getElementById('overlay');
    var openBtn  = document.getElementById('sidebarOpen');
    var closeBtn = document.getElementById('sidebarClose');

    function openSidebar()  { sidebar?.classList.add('open');    overlay?.classList.add('show'); }
    function closeSidebar() { sidebar?.classList.remove('open'); overlay?.classList.remove('show'); }

    openBtn?.addEventListener('click', openSidebar);
    closeBtn?.addEventListener('click', closeSidebar);
    overlay?.addEventListener('click', closeSidebar);

    // ── 3. AUTO-DISMISS ALERTS ────────────────────────────────────────────────
    setTimeout(function () {
        document.querySelectorAll('.alert').forEach(function (el) {
            bootstrap.Alert.getOrCreateInstance(el)?.close();
        });
    }, 4000);

    // ── 4. DELETE CONFIRMATION ────────────────────────────────────────────────
    document.querySelectorAll('.confirm-delete').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            var name = this.dataset.name || 'this record';
            if (!confirm('Delete "' + name + '"?\n\nThis action cannot be undone.')) {
                e.preventDefault();
            }
        });
    });

});
