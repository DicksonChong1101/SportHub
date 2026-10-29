// ======================================================
 // HOME MODULE - 13-SECOND HERO BANNER ROTATION
 // ======================================================
(() => {
    const hero = document.querySelector("[data-home-hero]");

    if (!hero) {
        return;
    }

    const slides = [...hero.querySelectorAll(".home-hero-slide")];
    const indicators = [...hero.querySelectorAll("[data-home-hero-index]")];

    if (slides.length < 2) {
        return;
    }

    const configuredInterval = Number(hero.dataset.homeHeroInterval);
    const intervalMilliseconds =
        Number.isFinite(configuredInterval) && configuredInterval >= 3000
            ? configuredInterval
            : 13000;

    let currentIndex = 0;
    let timerId;

    const showSlide = (nextIndex) => {
        currentIndex = (nextIndex + slides.length) % slides.length;

        slides.forEach((slide, index) => {
            const isActive = index === currentIndex;
            slide.classList.toggle("is-active", isActive);
            slide.setAttribute("aria-hidden", isActive ? "false" : "true");
        });

        indicators.forEach((indicator, index) => {
            const isActive = index === currentIndex;
            indicator.classList.toggle("is-active", isActive);
            indicator.setAttribute("aria-current", isActive ? "true" : "false");
        });
    };

    const stopRotation = () => {
        if (timerId) {
            window.clearTimeout(timerId);
            timerId = undefined;
        }
    };

    // Schedules one transition at a time. After the last slide, showSlide()
    // uses modulo arithmetic to return to index 0 and continue the loop.
    const scheduleNextSlide = () => {
        stopRotation();

        if (!document.hidden) {
            timerId = window.setTimeout(() => {
                showSlide(currentIndex + 1);
                scheduleNextSlide();
            }, intervalMilliseconds);
        }
    };

    indicators.forEach((indicator) => {
        indicator.addEventListener("click", () => {
            showSlide(Number(indicator.dataset.homeHeroIndex));
            scheduleNextSlide();
        });
    });

    document.addEventListener("visibilitychange", () => {
        if (document.hidden) {
            stopRotation();
        } else {
            scheduleNextSlide();
        }
    });

    showSlide(0);
    scheduleNextSlide();
})();
