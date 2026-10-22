/* =========================================================
   dashboard-core.js
   Shared by the four role dashboards.

   How it works:
   1) Each sidebar button has  data-page="someKey".
   2) When it is clicked, we look up  pages[someKey]  (a function that
      returns an HTML string) and put the result inside
      document.getElementById("mainContent").innerHTML.
   3) The sidebar + navbar never change — only <main id="mainContent">.

   Each role file (student-dashboard.js, ...) only has to call:
       initDashboard({ user: { name: "..." }, pages: { dashboard: fn, ... } });
   ========================================================= */
(function() {
    "use strict";

    // ---------- Small helpers ----------

    // Always escape data before putting it in innerHTML (protects you from XSS
    // once the data comes from Firebase / user input).
    function esc(value) {
        return String(value ?? "").replace(/[&<>"']/g, ch => ({
            "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
        }[ch]));
    }

    function greeting() {
        const h = new Date().getHours();
        if (h < 12) return "Good morning";
        if (h < 18) return "Good afternoon";
        return "Good evening";
    }

    // e.g. "TUE, OCT 20, 03:30 PM"
    function dateLine() {
        const d = new Date();
        const day = d.toLocaleDateString("en-US", { weekday: "short", month: "short", day: "numeric" });
        const time = d.toLocaleTimeString("en-US", { hour: "2-digit", minute: "2-digit" });
        return `${day}, ${time}`;
    }

    function initials(name) {
        return String(name || "")
            .replace(/^(Dr\.|Eng\.|Prof\.)\s*/i, "")
            .split(/\s+/).filter(Boolean)
            .slice(0, 2).map(w => w[0].toUpperCase()).join("");
    }

    // Status text → colour tone (used by UI.status)
    const STATUS_TONES = {
        "Pending": "warning",
        "Under Review": "primary",
        "Submitted": "primary",
        "Accepted": "success",
        "Confirmed": "success",
        "Approved": "success",
        "Completed": "success",
        "On Track": "success",
        "Ready": "success",
        "Rejected": "danger",
        "Delayed": "danger",
        "Needs Revision": "warning",
        "Awaiting Academic": "warning",
        "Awaiting Company": "warning",
        "New": "primary"
    };

    // ---------- UI building blocks (each returns an HTML string) ----------
    const UI = {
        esc,
        initials,

        /* Greeting header: date line + "Good afternoon, Ahmad." + subtitle */
        header(firstName, subtitle) {
            return `
            <header class="dash-head">
                <div class="dash-date">${dateLine()}</div>
                <h1 class="dash-title">${greeting()}, ${esc(firstName)}.</h1>
                <p class="dash-sub">${esc(subtitle)}</p>
            </header>`;
        },

        /* Row of 4 stat cards */
        stats(cards) {
            return `<section class="stats">${cards.map(UI.statCard).join("")}</section>`;
        },

        statCard({ label, value, note = "", icon, tone = "primary", soft = false }) {
            return `
            <article class="stat ${soft ? "soft" : ""}">
                <div class="stat-label">${esc(label)}</div>
                <div class="stat-value">${value}</div>
                <div class="stat-note">${esc(note)}</div>
                <span class="stat-icon tone-${tone}"><i class="bi ${icon}"></i></span>
            </article>`;
        },

        /* Big dark banner with a button that opens another page */
        hero({ kicker, title, text, button, page }) {
            return `
            <section class="hero">
                <div class="hero-content">
                    <div class="hero-kicker">${esc(kicker)} <i class="bi bi-arrow-up-right"></i></div>
                    <h2>${esc(title)}</h2>
                    <p>${esc(text)}</p>
                    <button type="button" class="hero-btn" data-page="${page}">${esc(button)} <i class="bi bi-arrow-right"></i></button>
                </div>
            </section>`;
        },

        /* White card. area = "list" | "side1" | "side2" (grid-template-areas) */
        panel({ title, area, page, body }) {
            const viewAll = page
                ? `<button type="button" class="link-btn" data-page="${page}">View all</button>`
                : "";
            return `
            <section class="panel area-${area}">
                <div class="panel-head">
                    <h2 class="panel-title">${esc(title)}</h2>
                    ${viewAll}
                </div>
                <div>${body}</div>
            </section>`;
        },

        /* One row inside a panel */
        item({ lead, title, sub = "", extra = "", meta = "", chevron = false }) {
            return `
            <div class="item">
                ${lead}
                <div class="item-body">
                    <div class="item-title">${esc(title)}</div>
                    ${sub ? `<div class="item-sub">${sub}</div>` : ""}
                    ${extra}
                </div>
                ${meta ? `<div class="item-meta">${meta}</div>` : ""}
                ${chevron ? `<i class="bi bi-chevron-right chev"></i>` : ""}
            </div>`;
        },

        logo(name) {
            return `<span class="item-logo">${esc(initials(name))}</span>`;
        },

        iconBox(icon, tone = "primary") {
            return `<span class="item-icon tone-${tone}"><i class="bi ${icon}"></i></span>`;
        },

        pill(text, tone = "primary") {
            return `<span class="pill tone-${tone}">${esc(text)}</span>`;
        },

        status(text) {
            return UI.pill(text, STATUS_TONES[text] || "muted");
        },

        progress(percent, tone = "") {
            const p = Math.max(0, Math.min(100, Math.round(percent)));
            return `<div class="progress-line ${tone}" role="progressbar" aria-valuenow="${p}" aria-valuemin="0" aria-valuemax="100"><span style="width:${p}%"></span></div>`;
        },

        tags(list) {
            return `<div class="tags">${list.map(t => `<span class="tag">${esc(t)}</span>`).join("")}</div>`;
        },

        pad(n) {
            return String(n).padStart(2, "0");
        },

        /* Simple page for sidebar items you haven't built yet */
        placeholder(title, description, icon = "bi-tools") {
            return `
            <div class="page">
                <header class="dash-head">
                    <div class="dash-date">${dateLine()}</div>
                    <h1 class="dash-title">${esc(title)}</h1>
                    <p class="dash-sub">${esc(description)}</p>
                </header>
                <section class="panel empty-state">
                    <span class="stat-icon tone-primary"><i class="bi ${icon}"></i></span>
                    <h3>${esc(title)} — coming soon</h3>
                    <p>Build this page's HTML inside its function in the role's JS file. It will be rendered into &lt;main id="mainContent"&gt;.</p>
                </section>
            </div>`;
        }
    };

    // ---------- Router: renders pages into <main id="mainContent"> ----------
    function initDashboard({ user, pages, defaultPage = "dashboard" }) {
        const main = document.getElementById("mainContent");
        const sidebar = document.getElementById("sidebar");
        const backdrop = document.getElementById("sidebarBackdrop");
        const roleTitle = document.title;

        // Navbar user info
        document.getElementById("userName").textContent = user.shortName || user.name;
        document.getElementById("userInitials").textContent = initials(user.name);

        function render() {
            const key = location.hash.slice(1);
            const page = pages[key] ? key : defaultPage;

            // THE important line: put the selected page's HTML into main
            main.innerHTML = pages[page]();

            // Highlight the active sidebar button
            document.querySelectorAll(".side-link[data-page]").forEach(btn => {
                const isActive = btn.dataset.page === page;
                btn.classList.toggle("active", isActive);
                if (isActive) btn.setAttribute("aria-current", "page");
                else btn.removeAttribute("aria-current");
            });

            const label = document.querySelector(`.side-link[data-page="${page}"] span`);
            document.title = label ? `${label.textContent} | ${roleTitle}` : roleTitle;
            window.scrollTo(0, 0);
        }

        // Any element with data-page (sidebar buttons, "View all", hero button) navigates
        document.addEventListener("click", e => {
            const target = e.target.closest("[data-page]");
            if (!target) return;
            e.preventDefault();                     // stop ASP.NET postback / link jump
            closeSidebar();
            const page = target.dataset.page;
            if (location.hash.slice(1) === page) render();
            else location.hash = page;              // triggers hashchange → render()
        });

        window.addEventListener("hashchange", render);

        // Mobile sidebar drawer
        function openSidebar() { sidebar.classList.add("open"); backdrop.classList.add("show"); }
        function closeSidebar() { sidebar.classList.remove("open"); backdrop.classList.remove("show"); }
        document.getElementById("menuToggle").addEventListener("click", openSidebar);
        backdrop.addEventListener("click", closeSidebar);
        document.addEventListener("keydown", e => { if (e.key === "Escape") closeSidebar(); });

        // Pressing Enter in the search box must not submit the Web Forms <form>
        const search = document.querySelector(".search input");
        if (search) {
            search.addEventListener("keydown", e => { if (e.key === "Enter") e.preventDefault(); });
        }

        render(); // first load → Dashboard (or the page in the URL hash)
    }

    window.UI = UI;
    window.initDashboard = initDashboard;
})();
