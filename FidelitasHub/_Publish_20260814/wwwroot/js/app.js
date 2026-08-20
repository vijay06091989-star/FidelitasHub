/* ==========================================================================
   Fidelitas Hub — app shell behaviour
   Theme toggle, sidebar collapse / off-canvas, submenu accordions,
   active-link highlighting, and the topbar clock.
   ========================================================================== */
(function () {
    "use strict";

    var root = document.documentElement;
    var body = document.body;

    /* ---------- Theme -------------------------------------------------- */
    function applyTheme(theme) {
        root.setAttribute("data-bs-theme", theme);
        root.setAttribute("data-theme", theme);
        try { localStorage.setItem("fh-theme", theme); } catch (e) { }
        var icon = document.querySelector("[data-theme-icon]");
        if (icon) {
            icon.className = (theme === "dark" ? "fa-solid fa-sun" : "fa-solid fa-moon");
        }
    }
    window.fhToggleTheme = function () {
        var current = root.getAttribute("data-bs-theme") === "dark" ? "dark" : "light";
        applyTheme(current === "dark" ? "light" : "dark");
    };

    /* ---------- Sidebar ------------------------------------------------ */
    var DESKTOP = 992;
    window.fhToggleSidebar = function () {
        if (window.innerWidth >= DESKTOP) {
            body.classList.toggle("sidebar-collapsed");
            try { localStorage.setItem("fh-sidebar", body.classList.contains("sidebar-collapsed") ? "1" : "0"); } catch (e) { }
        } else {
            body.classList.toggle("sidebar-open");
        }
    };
    function closeMobileSidebar() { body.classList.remove("sidebar-open"); }

    /* ---------- Submenu accordions ------------------------------------ */
    function initGroups() {
        document.querySelectorAll(".sidebar__group > .sidebar__link").forEach(function (link) {
            link.addEventListener("click", function (e) {
                e.preventDefault();
                var group = link.closest(".sidebar__group");
                var wasOpen = group.classList.contains("open");
                // accordion: close siblings
                document.querySelectorAll(".sidebar__group.open").forEach(function (g) {
                    if (g !== group) g.classList.remove("open");
                });
                group.classList.toggle("open", !wasOpen);
            });
        });
    }

    /* ---------- Active link -------------------------------------------- */
    function markActive() {
        var path = window.location.pathname.replace(/\/+$/, "").toLowerCase() || "/";
        var best = null, bestLen = -1;
        document.querySelectorAll(".sidebar__link[href]").forEach(function (a) {
            var href = a.getAttribute("href");
            if (!href || href === "#") return;
            var hp = href.replace(/\/+$/, "").toLowerCase();
            if (hp && (path === hp || path.indexOf(hp + "/") === 0) && hp.length > bestLen) {
                best = a; bestLen = hp.length;
            }
        });
        if (best) {
            best.classList.add("is-active");
            var group = best.closest(".sidebar__group");
            if (group) group.classList.add("open");
        }
    }

    /* ---------- Clock -------------------------------------------------- */
    function initClock() {
        var el = document.querySelector("[data-clock]");
        if (!el) return;
        function tick() {
            el.textContent = new Date().toLocaleString("en-IN", {
                weekday: "short", day: "2-digit", month: "short",
                hour: "2-digit", minute: "2-digit", second: "2-digit",
                hour12: true, timeZone: "Asia/Kolkata"
            }) + " IST";
        }
        tick();
        setInterval(tick, 1000);
    }

    /* ---------- Boot --------------------------------------------------- */
    document.addEventListener("DOMContentLoaded", function () {
        try {
            if (localStorage.getItem("fh-sidebar") === "1" && window.innerWidth >= DESKTOP) {
                body.classList.add("sidebar-collapsed");
            }
        } catch (e) { }

        var backdrop = document.querySelector(".sidebar__backdrop");
        if (backdrop) backdrop.addEventListener("click", closeMobileSidebar);

        initGroups();
        markActive();
        initClock();
    });
})();
