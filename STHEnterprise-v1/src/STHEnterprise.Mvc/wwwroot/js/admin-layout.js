$(function () {

    const SIDEBAR_KEY = "sidebar-collapsed";
    const $sidebar = $("#sidebar");

    /* ===============================
       RESTORE SIDEBAR STATE (DESKTOP)
    =============================== */

    function restoreSidebarState() {
        if (window.innerWidth > 768) {
            const isCollapsed = localStorage.getItem(SIDEBAR_KEY) === "true";
            $sidebar.toggleClass("collapsed", isCollapsed);
        }
    }

    restoreSidebarState();

    /* ===============================
       SIDEBAR TOGGLE
    =============================== */

    $("#toggleSidebar").on("click", function () {

        if (window.innerWidth <= 768) {
            // Mobile → slide in/out
            $sidebar.toggleClass("show");
            return;
        }

        // Desktop → collapse
        $sidebar.toggleClass("collapsed");

        // Persist state
        localStorage.setItem(
            SIDEBAR_KEY,
            $sidebar.hasClass("collapsed")
        );
    });

    /* ===============================
       RESET STATE ON RESIZE
    =============================== */

    $(window).on("resize", function () {

        if (window.innerWidth <= 768) {
            // Mobile cleanup
            $sidebar.removeClass("collapsed");
            localStorage.removeItem(SIDEBAR_KEY);
        } else {
            restoreSidebarState();
        }
    });

    /* ===============================
       THEME TOGGLE (UNCHANGED)
    =============================== */

    $("#themeToggle").on("click", function () {
        const html = $("html");
        const current = html.attr("data-theme");
        const next = current === "dark" ? "light" : "dark";

        html.attr("data-theme", next);
        localStorage.setItem("theme", next);

        $(this).find("i").toggleClass("bi-moon-stars bi-sun");
    });

});



//$(document).ready(function () {

//    const $html = $("html");
//    const $sidebar = $("#sidebar");
//    const $toggleSidebar = $("#toggleSidebar");
//    const $themeToggle = $("#themeToggle");
//    const MOBILE_WIDTH = 768;

//    /* ======================================
//       SIDEBAR TOGGLE
//    ====================================== */
//    $toggleSidebar.on("click", function () {

//        if (window.innerWidth <= MOBILE_WIDTH) {
//            // Mobile → slide in/out
//            $sidebar.toggleClass("show");
//        } else {
//            // Desktop → mini sidebar
//            $sidebar.toggleClass("collapsed");
//        }

//        $(this).attr("aria-expanded",
//            !$sidebar.hasClass("collapsed") && !$sidebar.hasClass("show")
//        );
//    });

//    /* ======================================
//       AUTO FIX SIDEBAR ON RESIZE
//    ====================================== */
//    $(window).on("resize", function () {

//        if (window.innerWidth > MOBILE_WIDTH) {
//            // Desktop cleanup
//            $sidebar.removeClass("show");
//        } else {
//            // Mobile cleanup
//            $sidebar.removeClass("collapsed");
//        }
//    });

//    /* ======================================
//       THEME TOGGLE
//    ====================================== */
//    function applyTheme(theme) {
//        $html.attr("data-theme", theme);
//        localStorage.setItem("theme", theme);

//        // Update icon
//        const $icon = $themeToggle.find("i");
//        $icon.removeClass("bi-moon-stars bi-sun")
//            .addClass(theme === "dark" ? "bi-sun" : "bi-moon-stars");
//    }

//    // Toggle theme on click
//    $themeToggle.on("click", function () {
//        const currentTheme = $html.attr("data-theme") || "light";
//        const nextTheme = currentTheme === "dark" ? "light" : "dark";
//        applyTheme(nextTheme);
//    });

//    /* ======================================
//       INITIAL LOAD
//    ====================================== */
//    (function init() {
//        const savedTheme = localStorage.getItem("theme") || "light";
//        applyTheme(savedTheme);
//    })();

//});
