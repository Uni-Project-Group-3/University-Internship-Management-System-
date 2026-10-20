(() => {
    "use strict";

    document.querySelectorAll(".home-navbar").forEach(nav => {
        const toggle = nav.querySelector(".home-navbar__toggle");
        const links = nav.querySelector(".home-navbar__links");
        const mobile = window.matchMedia("(max-width: 700px)");

        function setOpen(open) {
            toggle.setAttribute("aria-expanded", String(open));
            toggle.setAttribute("aria-label", open ? "Close navigation menu" : "Open navigation menu");
            links.classList.toggle("is-open", open);
        }

        nav.classList.add("has-menu-toggle");
        toggle.hidden = false;
        toggle.addEventListener("click", () => setOpen(toggle.getAttribute("aria-expanded") !== "true"));
        nav.addEventListener("keydown", event => {
            if (event.key === "Escape" && toggle.getAttribute("aria-expanded") === "true") {
                setOpen(false);
                toggle.focus();
            }
        });
        nav.addEventListener("focusout", event => {
            if (!nav.contains(event.relatedTarget)) setOpen(false);
        });
        document.addEventListener("click", event => {
            if (!nav.contains(event.target)) setOpen(false);
        });
        mobile.addEventListener("change", () => setOpen(false));
    });
})();
