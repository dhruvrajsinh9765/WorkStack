(() => {
    const initializeWorkspaceNavigation = () => {
        const shell = document.querySelector("[data-ws-app-shell]");
        const toggle = shell?.querySelector("[data-ws-nav-toggle]");
        const sidebar = shell?.querySelector("#ws-sidebar");
        const backdrop = shell?.querySelector("[data-ws-nav-close]");

        if (!shell || !toggle || !sidebar || !backdrop) {
            return;
        }

        const mobileQuery = window.matchMedia("(max-width: 767.98px)");
        const setOpen = (isOpen, restoreFocus = false) => {
            const shouldOpen = mobileQuery.matches && isOpen;
            shell.classList.toggle("is-nav-open", shouldOpen);
            toggle.setAttribute("aria-expanded", String(shouldOpen));
            toggle.setAttribute("aria-label", shouldOpen ? "Close navigation" : "Open navigation");
            sidebar.setAttribute("aria-hidden", String(mobileQuery.matches && !shouldOpen));
            document.body.classList.toggle("ws-menu-open", shouldOpen);

            if (shouldOpen) {
                sidebar.querySelector("a")?.focus();
            } else if (restoreFocus && mobileQuery.matches) {
                toggle.focus();
            }
        };

        toggle.addEventListener("click", () => {
            setOpen(toggle.getAttribute("aria-expanded") !== "true");
        });
        backdrop.addEventListener("click", () => setOpen(false, true));
        sidebar.addEventListener("click", event => {
            if (event.target.closest("a")) {
                setOpen(false);
            }
        });
        document.addEventListener("keydown", event => {
            if (event.key === "Escape" && shell.classList.contains("is-nav-open")) {
                setOpen(false, true);
            }
        });
        mobileQuery.addEventListener("change", () => setOpen(false));
        setOpen(false);
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initializeWorkspaceNavigation, { once: true });
    } else {
        initializeWorkspaceNavigation();
    }
})();
