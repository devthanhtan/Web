// ==========================================================================
// HUIT Learning v2 — Smooth Scroll (Lenis), GSAP Choreography & Motion Engine
// Zero-error offline fallback + strict prefers-reduced-motion compliance
// ==========================================================================
(function () {
    "use strict";

    const reducedMotionQuery = window.matchMedia("(prefers-reduced-motion: reduce)");
    const finePointerQuery = window.matchMedia("(hover: hover) and (pointer: fine)");
    const desktopQuery = window.matchMedia("(min-width: 992px)");

    let lenisInstance = null;
    let lenisRafId = 0;
    let activeOverlayCount = 0;

    /**
     * Progressive View Transitions API wrapper with safe synchronous fallback.
     * Exposed globally as window.huitViewTransition(updateDomFn).
     */
    window.huitViewTransition = function (updateDomFn) {
        if (typeof updateDomFn !== "function") {
            return Promise.resolve();
        }

        if (
            reducedMotionQuery.matches ||
            typeof document.startViewTransition !== "function"
        ) {
            try {
                return Promise.resolve(updateDomFn());
            } catch (err) {
                return Promise.reject(err);
            }
        }

        try {
            const transition = document.startViewTransition(function () {
                return updateDomFn();
            });
            return transition.finished.catch(function () {});
        } catch (_err) {
            return Promise.resolve(updateDomFn());
        }
    };

    /**
     * Smooth View Transitions on Bootstrap 5 tab & pill switches.
     */
    function initTabViewTransitions() {
        document.addEventListener(
            "click",
            function (e) {
                if (
                    reducedMotionQuery.matches ||
                    typeof document.startViewTransition !== "function" ||
                    !window.bootstrap ||
                    !window.bootstrap.Tab
                ) {
                    return;
                }

                const trigger = e.target.closest(
                    '[data-bs-toggle="tab"], [data-bs-toggle="pill"]'
                );
                if (
                    !trigger ||
                    trigger.classList.contains("active") ||
                    trigger.disabled ||
                    trigger.getAttribute("aria-disabled") === "true"
                ) {
                    return;
                }

                e.preventDefault();
                e.stopPropagation();

                window.huitViewTransition(function () {
                    return new Promise(function (resolve) {
                        let settled = false;
                        const finish = function () {
                            if (!settled) {
                                settled = true;
                                resolve();
                            }
                        };

                        trigger.addEventListener("shown.bs.tab", finish, {
                            once: true
                        });
                        setTimeout(finish, 220);

                        const tabInstance =
                            window.bootstrap.Tab.getOrCreateInstance(trigger);
                        tabInstance.show();
                    });
                });
            },
            true
        );
    }

    /**
     * Lenis Smooth Scroll synced cleanly with GSAP ticker + ScrollTrigger.
     * Automatically pauses on Bootstrap modals/offcanvas and disables on reduced motion.
     */
    function initSmoothScroll() {
        if (reducedMotionQuery.matches || typeof window.Lenis !== "function") {
            return;
        }

        try {
            const hasGsap = typeof window.gsap !== "undefined";
            const hasScrollTrigger =
                hasGsap && typeof window.ScrollTrigger !== "undefined";

            if (hasScrollTrigger) {
                window.gsap.registerPlugin(window.ScrollTrigger);
            }

            lenisInstance = new window.Lenis({
                duration: 1.1,
                easing: function (t) {
                    return Math.min(1, 1.001 - Math.pow(2, -10 * t));
                },
                smoothWheel: true,
                touchMultiplier: 1.5
            });

            window.huitLenis = lenisInstance;

            lenisInstance.on("scroll", function () {
                if (hasScrollTrigger) {
                    window.ScrollTrigger.update();
                }
                updateScrollChrome();
            });

            if (hasGsap) {
                window.gsap.ticker.add(function (time) {
                    if (lenisInstance) {
                        lenisInstance.raf(time * 1000);
                    }
                });
                window.gsap.ticker.lagSmoothing(0);
            } else {
                const raf = function (time) {
                    if (!lenisInstance) return;
                    lenisInstance.raf(time);
                    lenisRafId = requestAnimationFrame(raf);
                };
                lenisRafId = requestAnimationFrame(raf);
            }

            // Pause Lenis while Bootstrap modals or offcanvas drawers are open
            const pauseLenis = function () {
                activeOverlayCount += 1;
                if (lenisInstance) {
                    lenisInstance.stop();
                }
            };

            const resumeLenis = function () {
                activeOverlayCount = Math.max(0, activeOverlayCount - 1);
                if (activeOverlayCount === 0 && lenisInstance) {
                    lenisInstance.start();
                }
            };

            document.addEventListener("show.bs.modal", pauseLenis);
            document.addEventListener("hidden.bs.modal", resumeLenis);
            document.addEventListener("show.bs.offcanvas", pauseLenis);
            document.addEventListener("hidden.bs.offcanvas", resumeLenis);

            // Smooth in-page hash navigation
            document.addEventListener("click", function (e) {
                const anchor = e.target.closest('a[href^="#"]');
                if (!anchor || anchor.hasAttribute("data-bs-toggle")) return;
                const href = anchor.getAttribute("href");
                if (!href || href === "#" || href.length < 2) return;

                const target = document.querySelector(href);
                if (target && lenisInstance) {
                    e.preventDefault();
                    lenisInstance.scrollTo(target, { offset: -84 });
                }
            });
        } catch (_err) {
            lenisInstance = null;
        }
    }

    /**
     * Scroll-driven UI chrome:
     * - Floating navbar elevation (.is-scrolled)
     * - Top reading progress bar (.hl-scroll-progress__bar)
     * - Back-to-top button visibility & SVG ring progress (.hl-back-to-top)
     */
    let scrollProgressBar = null;
    let backToTopBtn = null;
    let backToTopRing = null;
    let backToTopCircumference = 132;
    let navbars = [];
    let scrollTickPending = false;

    function updateScrollChrome() {
        const scrollTop = window.scrollY || document.documentElement.scrollTop || 0;
        const maxScroll = Math.max(
            document.documentElement.scrollHeight - window.innerHeight,
            1
        );
        const progress = Math.min(Math.max(scrollTop / maxScroll, 0), 1);

        for (let i = 0; i < navbars.length; i++) {
            if (scrollTop > 14) {
                navbars[i].classList.add("is-scrolled");
            } else {
                navbars[i].classList.remove("is-scrolled");
            }
        }

        if (scrollProgressBar) {
            scrollProgressBar.style.setProperty("--hl-scroll-p", progress.toFixed(4));
        }

        if (backToTopBtn) {
            if (scrollTop > 280) {
                backToTopBtn.classList.add("is-visible");
            } else {
                backToTopBtn.classList.remove("is-visible");
            }

            if (backToTopRing) {
                const offset = backToTopCircumference * (1 - progress);
                backToTopRing.style.strokeDashoffset = offset.toFixed(2);
            }
        }
    }

    function initScrollChrome() {
        navbars = Array.from(document.querySelectorAll(".app-navbar, .hl-floating-nav"));
        scrollProgressBar = document.querySelector(".hl-scroll-progress__bar");
        backToTopBtn = document.querySelector(".hl-back-to-top");
        backToTopRing = document.querySelector(".hl-back-to-top__fill");

        if (backToTopRing) {
            const r = parseFloat(backToTopRing.getAttribute("r") || "21");
            backToTopCircumference = 2 * Math.PI * r;
            backToTopRing.style.strokeDasharray = backToTopCircumference.toFixed(2);
            backToTopRing.style.strokeDashoffset = backToTopCircumference.toFixed(2);
        }

        if (backToTopBtn) {
            backToTopBtn.addEventListener("click", function () {
                if (lenisInstance && !reducedMotionQuery.matches) {
                    lenisInstance.scrollTo(0, { duration: 1.05 });
                } else {
                    window.scrollTo({
                        top: 0,
                        behavior: reducedMotionQuery.matches ? "auto" : "smooth"
                    });
                }
            });
        }

        updateScrollChrome();

        window.addEventListener(
            "scroll",
            function () {
                if (scrollTickPending) return;
                scrollTickPending = true;
                requestAnimationFrame(function () {
                    scrollTickPending = false;
                    updateScrollChrome();
                });
            },
            { passive: true }
        );
    }

    /**
     * Form loading spinner helper for forms marked with [data-loading-form].
     */
    function initLoadingForms() {
        document.querySelectorAll("form[data-loading-form]").forEach(function (form) {
            form.addEventListener("submit", function () {
                if (typeof form.checkValidity === "function" && !form.checkValidity()) {
                    return;
                }
                const btn = form.querySelector("button[type='submit']");
                if (btn && !btn.disabled) {
                    btn.dataset.originalHtml = btn.innerHTML;
                    btn.disabled = true;
                    btn.innerHTML =
                        '<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>Đang xử lý...';
                }
            });
        });
    }

    /**
     * Reveal choreography:
     * - [data-stagger-group] + [data-reveal="up"|"fade"|"scale"|"left"|"right"]
     * - Uses GSAP ScrollTrigger when available, with failure-safe IntersectionObserver fallback.
     */
    function revealElementNow(el) {
        el.classList.add("is-revealed");
        el.style.opacity = "";
        el.style.transform = "";
    }

    function initScrollReveal() {
        const revealEls = Array.from(document.querySelectorAll("[data-reveal]"));
        if (!revealEls.length) return;

        if (reducedMotionQuery.matches) {
            document.documentElement.classList.remove("js-motion-ready");
            revealEls.forEach(revealElementNow);
            return;
        }

        const hasGsapST =
            typeof window.gsap !== "undefined" &&
            typeof window.ScrollTrigger !== "undefined";

        const handledSet = new Set();

        if (hasGsapST) {
            try {
                window.gsap.registerPlugin(window.ScrollTrigger);

                // Stagger groups
                document.querySelectorAll("[data-stagger-group]").forEach(function (group) {
                    const children = Array.from(group.querySelectorAll("[data-reveal]"));
                    if (!children.length) return;

                    children.forEach(function (child) {
                        handledSet.add(child);
                        // Clear CSS transition so GSAP owns transform/opacity cleanly
                        child.style.transition = "none";
                    });

                    const rect = group.getBoundingClientRect();
                    if (rect.top < window.innerHeight * 0.92 && rect.bottom > 0) {
                        window.gsap.fromTo(
                            children,
                            { opacity: 0, y: 20 },
                            {
                                opacity: 1,
                                y: 0,
                                duration: 0.56,
                                stagger: 0.06,
                                ease: "power3.out",
                                clearProps: "opacity,transform,transition",
                                onComplete: function () {
                                    children.forEach(revealElementNow);
                                }
                            }
                        );
                    } else {
                        window.gsap.fromTo(
                            children,
                            { opacity: 0, y: 22 },
                            {
                                opacity: 1,
                                y: 0,
                                duration: 0.58,
                                stagger: 0.065,
                                ease: "power3.out",
                                clearProps: "opacity,transform,transition",
                                scrollTrigger: {
                                    trigger: group,
                                    start: "top 88%",
                                    once: true
                                },
                                onComplete: function () {
                                    children.forEach(revealElementNow);
                                }
                            }
                        );
                    }
                });
            } catch (_err) {
                handledSet.clear();
            }
        }

        const remaining = revealEls.filter(function (el) {
            return !handledSet.has(el);
        });

        if (!remaining.length) return;

        if (typeof window.IntersectionObserver !== "function") {
            document.documentElement.classList.remove("js-motion-ready");
            remaining.forEach(revealElementNow);
            return;
        }

        remaining.forEach(function (el) {
            const delay = parseInt(el.getAttribute("data-reveal-delay") || "0", 10);
            if (delay > 0 && delay <= 1200) {
                el.style.transitionDelay = delay + "ms";
            }
        });

        const observer = new IntersectionObserver(
            function (entries, obs) {
                entries.forEach(function (entry) {
                    if (entry.isIntersecting) {
                        entry.target.classList.add("is-revealed");
                        obs.unobserve(entry.target);
                    }
                });
            },
            {
                root: null,
                rootMargin: "0px 0px -36px 0px",
                threshold: 0.05
            }
        );

        remaining.forEach(function (el) {
            const rect = el.getBoundingClientRect();
            if (rect.top < window.innerHeight * 0.96 && rect.bottom > 0) {
                el.classList.add("is-revealed");
            } else {
                observer.observe(el);
            }
        });
    }

    /**
     * Gentle vertical scroll parallax for [data-parallax="0.12"] on desktop only.
     */
    function initParallax() {
        if (
            reducedMotionQuery.matches ||
            !desktopQuery.matches ||
            typeof window.gsap === "undefined" ||
            typeof window.ScrollTrigger === "undefined"
        ) {
            return;
        }

        const parallaxEls = document.querySelectorAll("[data-parallax]");
        if (!parallaxEls.length) return;

        try {
            window.gsap.registerPlugin(window.ScrollTrigger);
            parallaxEls.forEach(function (el) {
                const factor = parseFloat(el.getAttribute("data-parallax") || "0.12");
                if (!Number.isFinite(factor) || factor === 0) return;

                const distance = Math.max(Math.min(factor * 240, 64), -64);
                window.gsap.fromTo(
                    el,
                    { y: -distance * 0.45 },
                    {
                        y: distance * 0.45,
                        ease: "none",
                        scrollTrigger: {
                            trigger: el,
                            start: "top bottom",
                            end: "bottom top",
                            scrub: true
                        }
                    }
                );
            });
        } catch (_err) {
            // Silently ignore if ScrollTrigger unavailable
        }
    }

    /**
     * Interactive 3D perspective card tilt + specular glare on fine-pointer desktops.
     */
    function initTilt3D() {
        if (reducedMotionQuery.matches || !finePointerQuery.matches) {
            return;
        }

        const cards = document.querySelectorAll("[data-tilt-3d]");
        cards.forEach(function (card) {
            if (card.dataset.tiltBound === "true") return;
            card.dataset.tiltBound = "true";

            let glare = card.querySelector(":scope > .huit-tilt-glare");
            if (!glare) {
                glare = document.createElement("span");
                glare.className = "huit-tilt-glare";
                glare.setAttribute("aria-hidden", "true");
                card.appendChild(glare);
            }

            let rafId = 0;
            const maxTilt = parseFloat(card.getAttribute("data-tilt-max") || "5");

            card.addEventListener("pointerenter", function () {
                if (!finePointerQuery.matches || reducedMotionQuery.matches) return;
                card.classList.add("is-tilting");
                card.style.transition =
                    "transform 0.1s ease-out, box-shadow 0.22s ease, border-color 0.22s ease";
            });

            card.addEventListener("pointermove", function (e) {
                if (!finePointerQuery.matches || reducedMotionQuery.matches) return;
                if (rafId) cancelAnimationFrame(rafId);

                rafId = requestAnimationFrame(function () {
                    const rect = card.getBoundingClientRect();
                    if (!rect.width || !rect.height) return;

                    const px = (e.clientX - rect.left) / rect.width;
                    const py = (e.clientY - rect.top) / rect.height;

                    const rotateY = (px - 0.5) * (maxTilt * 2);
                    const rotateX = (0.5 - py) * (maxTilt * 2);

                    card.style.transform =
                        "perspective(920px) rotateX(" +
                        rotateX.toFixed(2) +
                        "deg) rotateY(" +
                        rotateY.toFixed(2) +
                        "deg) translateY(-4px)";
                    card.style.setProperty("--glare-x", (px * 100).toFixed(1) + "%");
                    card.style.setProperty("--glare-y", (py * 100).toFixed(1) + "%");
                });
            });

            const resetTilt = function () {
                if (rafId) cancelAnimationFrame(rafId);
                card.classList.remove("is-tilting");
                card.style.transition =
                    "transform 0.36s cubic-bezier(0.22, 1, 0.36, 1), box-shadow 0.22s ease, border-color 0.22s ease";
                card.style.transform = "";
            };

            card.addEventListener("pointerleave", resetTilt);
            card.addEventListener("pointercancel", resetTilt);
        });
    }

    /**
     * Animated KPI counter for [data-count-to] elements when scrolled into view.
     */
    function parseCountTarget(rawAttr, initialText) {
        if (rawAttr === null || rawAttr === undefined) return null;
        const cleaned = String(rawAttr).trim().replace(/\s+/g, "");
        if (!cleaned) return null;

        let normalized = cleaned;
        if (/^-?\d+,\d{1,2}$/.test(cleaned)) {
            normalized = cleaned.replace(",", ".");
        } else {
            normalized = cleaned.replace(/,/g, "");
        }

        const value = Number(normalized);
        if (!Number.isFinite(value)) return null;

        const hasDecimals = Math.abs(value % 1) > 0.001;
        const decimals = hasDecimals ? 1 : 0;

        let prefix = "";
        let suffix = "";
        const trimmedText = (initialText || "").trim();
        const affixMatch = trimmedText.match(/^([^\d+-]*)[+-]?[\d.,\s]+([^\d]*)$/);
        if (affixMatch) {
            prefix = affixMatch[1] || "";
            suffix = affixMatch[2] || "";
        }

        return { value: value, decimals: decimals, prefix: prefix, suffix: suffix };
    }

    function formatCountValue(num, decimals) {
        if (decimals > 0) {
            return num.toFixed(decimals);
        }
        const rounded = Math.round(num);
        if (Math.abs(rounded) >= 1000) {
            return rounded.toLocaleString("vi-VN");
        }
        return String(rounded);
    }

    function initCounters() {
        const counterEls = document.querySelectorAll("[data-count-to]");
        if (!counterEls.length) return;

        const animateCounter = function (el) {
            if (el.dataset.counted === "true") return;
            el.dataset.counted = "true";

            const parsed = parseCountTarget(
                el.getAttribute("data-count-to"),
                el.textContent
            );
            if (!parsed) return;

            const prefix =
                el.getAttribute("data-count-prefix") !== null
                    ? el.getAttribute("data-count-prefix")
                    : parsed.prefix;
            const suffix =
                el.getAttribute("data-count-suffix") !== null
                    ? el.getAttribute("data-count-suffix")
                    : parsed.suffix;
            const decimals =
                el.getAttribute("data-count-decimals") !== null
                    ? parseInt(el.getAttribute("data-count-decimals"), 10) || 0
                    : parsed.decimals;

            const finalDisplay =
                prefix + formatCountValue(parsed.value, decimals) + suffix;

            if (reducedMotionQuery.matches || parsed.value === 0) {
                el.textContent = finalDisplay;
                return;
            }

            const duration = 1050;
            const startTime = performance.now();

            const tick = function (now) {
                const elapsed = now - startTime;
                const progress = Math.min(elapsed / duration, 1);
                const eased = 1 - Math.pow(1 - progress, 3);
                const current = parsed.value * eased;

                if (progress < 1) {
                    el.textContent =
                        prefix + formatCountValue(current, decimals) + suffix;
                    requestAnimationFrame(tick);
                } else {
                    el.textContent = finalDisplay;
                }
            };

            requestAnimationFrame(tick);
        };

        if (
            reducedMotionQuery.matches ||
            typeof window.IntersectionObserver !== "function"
        ) {
            counterEls.forEach(animateCounter);
            return;
        }

        const observer = new IntersectionObserver(
            function (entries, obs) {
                entries.forEach(function (entry) {
                    if (entry.isIntersecting) {
                        animateCounter(entry.target);
                        obs.unobserve(entry.target);
                    }
                });
            },
            { threshold: 0.15 }
        );

        counterEls.forEach(function (el) {
            observer.observe(el);
        });
    }

    /**
     * Ensure view-transition-name values are unique per page so MPA/SPA transitions never abort.
     */
    function deduplicateViewTransitionNames() {
        const seen = new Set();
        document.querySelectorAll('[style*="view-transition-name"]').forEach(function (el) {
            const name = (el.style.viewTransitionName || "").trim();
            if (!name || name === "none") return;
            if (seen.has(name)) {
                el.style.viewTransitionName = "none";
            } else {
                seen.add(name);
            }
        });
    }

    /**
     * Live listener for prefers-reduced-motion toggle.
     */
    if (typeof reducedMotionQuery.addEventListener === "function") {
        reducedMotionQuery.addEventListener("change", function (e) {
            if (e.matches) {
                document.documentElement.classList.remove("js-motion-ready");
                document.querySelectorAll("[data-reveal]").forEach(revealElementNow);
                if (lenisInstance) {
                    lenisInstance.destroy();
                    lenisInstance = null;
                }
                if (lenisRafId) {
                    cancelAnimationFrame(lenisRafId);
                    lenisRafId = 0;
                }
            }
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        deduplicateViewTransitionNames();
        initScrollChrome();
        initSmoothScroll();
        initLoadingForms();
        initTabViewTransitions();
        initScrollReveal();
        initParallax();
        initTilt3D();
        initCounters();
    });
})();
