// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// =====================================================
// UI/UX BOSS - GLOBAL INTERACTION ENHANCEMENTS
// Adds a compact sticky-header state and gentle reveal motion.
// =====================================================
document.addEventListener("DOMContentLoaded", () => {
    const header = document.querySelector(".site-header");

    const updateHeader = () => {
        header?.classList.toggle("is-scrolled", window.scrollY > 12);
    };

    updateHeader();
    window.addEventListener("scroll", updateHeader, { passive: true });

    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
        return;
    }

    const revealItems = document.querySelectorAll(
        ".site-main-content > section, .site-main-content > .row, " +
        ".site-main-content > .card, .site-main-content > .table-responsive"
    );

    if (!("IntersectionObserver" in window)) {
        revealItems.forEach(item => item.classList.add("is-visible"));
        return;
    }

    const observer = new IntersectionObserver(entries => {
        entries.forEach(entry => {
            if (!entry.isIntersecting) {
                return;
            }

            entry.target.classList.add("is-visible");
            observer.unobserve(entry.target);
        });
    }, { threshold: 0.08 });

    revealItems.forEach(item => {
        item.classList.add("ui-reveal");
        observer.observe(item);
    });
});
