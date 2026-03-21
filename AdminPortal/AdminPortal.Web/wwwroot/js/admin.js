document.addEventListener("DOMContentLoaded", function () {
    // Sidebar toggle (mobile)
    const toggle = document.getElementById("sidebarToggle");
    const sidebar = document.getElementById("sidebar");
    if (toggle && sidebar) {
        toggle.addEventListener("click", () => sidebar.classList.toggle("open"));
        document.addEventListener("click", (e) => {
            if (sidebar.classList.contains("open") && !sidebar.contains(e.target) && !toggle.contains(e.target))
                sidebar.classList.remove("open");
        });
    }

    // Auto-dismiss flash banners after 4s
    document.querySelectorAll(".flash-banner").forEach(b => {
        const close = b.querySelector(".flash-close");
        if (close) close.addEventListener("click", () => b.remove());
        setTimeout(() => b?.remove(), 4000);
    });

    // Uppercase discount code inputs
    document.querySelectorAll("input[name='Code']").forEach(inp => {
        inp.addEventListener("input", () => inp.value = inp.value.toUpperCase());
    });

    // Select all checkbox
    document.querySelectorAll("thead input[type='checkbox']").forEach(th => {
        th.addEventListener("change", () => {
            const table = th.closest("table");
            table?.querySelectorAll("tbody input[type='checkbox']")
                  .forEach(cb => cb.checked = th.checked);
        });
    });
});
