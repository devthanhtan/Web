// ==========================================================================
// HUIT Learning v2 — Articulated 3D Academic Orbital Astrolabe / Knowledge Core
// Mounts onto <canvas id="huit-hero-3d-canvas"></canvas>.
// Features: studio 3-point lighting, sapphire-glass icosahedron core, warm amber
// inner prism, dual beveled metallic orbital rings, 6 orbiting knowledge modules,
// subtle depth particles, pointer parallax + scroll-linked rotation, ResizeObserver,
// IntersectionObserver + visibilitychange pause, and silent WebGL/offline fallback.
// ==========================================================================

const THREE_CDN_URL = "https://cdn.jsdelivr.net/npm/three@0.165.0/build/three.module.js";

function supportsWebGL() {
    try {
        const probe = document.createElement("canvas");
        return !!(
            window.WebGLRenderingContext &&
            (probe.getContext("webgl2") ||
                probe.getContext("webgl") ||
                probe.getContext("experimental-webgl"))
        );
    } catch (_err) {
        return false;
    }
}

function createGlowSpriteTexture(THREE) {
    const size = 64;
    const spriteCanvas = document.createElement("canvas");
    spriteCanvas.width = size;
    spriteCanvas.height = size;
    const ctx = spriteCanvas.getContext("2d");
    if (!ctx) return null;

    const grad = ctx.createRadialGradient(
        size / 2,
        size / 2,
        0,
        size / 2,
        size / 2,
        size / 2
    );
    grad.addColorStop(0, "rgba(255, 255, 255, 1)");
    grad.addColorStop(0.32, "rgba(255, 255, 255, 0.72)");
    grad.addColorStop(1, "rgba(255, 255, 255, 0)");
    ctx.fillStyle = grad;
    ctx.fillRect(0, 0, size, size);

    const texture = new THREE.CanvasTexture(spriteCanvas);
    texture.needsUpdate = true;
    return texture;
}

function buildAstrolabeRing(THREE, radius, tubeRadius, color, emissive, tickCount) {
    const group = new THREE.Group();

    // Primary beveled metallic torus
    const mainGeo = new THREE.TorusGeometry(radius, tubeRadius, 20, 128);
    const mainMat = new THREE.MeshStandardMaterial({
        color: color,
        emissive: emissive,
        emissiveIntensity: 0.35,
        metalness: 0.72,
        roughness: 0.22
    });
    group.add(new THREE.Mesh(mainGeo, mainMat));

    // Subtle secondary precision inner rim
    const rimGeo = new THREE.TorusGeometry(radius - 0.065, tubeRadius * 0.42, 12, 96);
    const rimMat = new THREE.MeshStandardMaterial({
        color: 0xe2e8f0,
        emissive: color,
        emissiveIntensity: 0.2,
        metalness: 0.8,
        roughness: 0.25,
        transparent: true,
        opacity: 0.78
    });
    group.add(new THREE.Mesh(rimGeo, rimMat));

    // Astrolabe calibration degree ticks
    const tickGeo = new THREE.BoxGeometry(0.018, 0.08, 0.018);
    const tickMat = new THREE.MeshStandardMaterial({
        color: 0xffffff,
        emissive: color,
        emissiveIntensity: 0.45,
        metalness: 0.5,
        roughness: 0.2
    });

    for (let i = 0; i < tickCount; i++) {
        const theta = (i / tickCount) * Math.PI * 2;
        const tick = new THREE.Mesh(tickGeo, tickMat);
        tick.position.set(Math.cos(theta) * radius, Math.sin(theta) * radius, 0);
        tick.rotation.z = theta - Math.PI / 2;
        group.add(tick);
    }

    return group;
}

async function mountHuitHero3D() {
    const canvas = document.getElementById("huit-hero-3d-canvas");
    if (!canvas || canvas.dataset.threeMounted === "true") {
        return;
    }
    canvas.dataset.threeMounted = "true";

    if (!supportsWebGL()) {
        canvas.dataset.webglFallback = "true";
        return;
    }

    try {
        const THREE = await import(THREE_CDN_URL);

        const container = canvas.parentElement || canvas;
        const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");

        const renderer = new THREE.WebGLRenderer({
            canvas: canvas,
            alpha: true,
            antialias: true,
            powerPreference: "high-performance"
        });
        renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
        renderer.setClearColor(0x000000, 0);
        renderer.toneMapping = THREE.ACESFilmicToneMapping;
        renderer.toneMappingExposure = 1.15;

        const scene = new THREE.Scene();
        const camera = new THREE.PerspectiveCamera(40, 1, 0.1, 100);
        camera.position.set(0, 0.12, 6.55);

        // Studio 3-Point Lighting: Daylight Key + Sky Fill + Warm Amber Rim
        const hemiLight = new THREE.HemisphereLight(0xf0f9ff, 0x0b192c, 1.3);
        scene.add(hemiLight);

        const keyLight = new THREE.DirectionalLight(0xffffff, 2.45);
        keyLight.position.set(4.8, 5.8, 5.2);
        scene.add(keyLight);

        const fillLight = new THREE.PointLight(0x0ea5e9, 3.0, 16);
        fillLight.position.set(3.8, 2.5, 3.0);
        scene.add(fillLight);

        const rimLight = new THREE.PointLight(0xf97316, 3.4, 16);
        rimLight.position.set(-3.8, -2.2, 3.2);
        scene.add(rimLight);

        // Root group for smooth pointer parallax & scroll-linked rotation
        const rootGroup = new THREE.Group();
        scene.add(rootGroup);

        // 1. Central Articulated Knowledge Core (Faceted Sapphire-Glass Icosahedron)
        const coreGroup = new THREE.Group();
        rootGroup.add(coreGroup);

        const coreGeo = new THREE.IcosahedronGeometry(1.06, 1);
        const coreMat = new THREE.MeshPhysicalMaterial({
            color: 0x0f4c81,
            emissive: 0x093257,
            emissiveIntensity: 0.38,
            metalness: 0.18,
            roughness: 0.14,
            clearcoat: 1.0,
            clearcoatRoughness: 0.1,
            transmission: 0.28,
            thickness: 1.2,
            ior: 1.48,
            flatShading: true
        });
        const coreMesh = new THREE.Mesh(coreGeo, coreMat);
        coreGroup.add(coreMesh);

        // Warm Amber Inner Prism
        const prismGeo = new THREE.OctahedronGeometry(0.56, 0);
        const prismMat = new THREE.MeshStandardMaterial({
            color: 0xf97316,
            emissive: 0xea580c,
            emissiveIntensity: 0.85,
            roughness: 0.2,
            metalness: 0.15,
            flatShading: true
        });
        const innerPrismMesh = new THREE.Mesh(prismGeo, prismMat);
        coreGroup.add(innerPrismMesh);

        // Precision Geodesic Wireframe Cage
        const cageGeo = new THREE.IcosahedronGeometry(1.34, 1);
        const cageEdges = new THREE.EdgesGeometry(cageGeo);
        const cageMat = new THREE.LineBasicMaterial({
            color: 0x38bdf8,
            transparent: true,
            opacity: 0.55
        });
        const cageWireframe = new THREE.LineSegments(cageEdges, cageMat);
        coreGroup.add(cageWireframe);

        // 2. Dual Beveled Metallic Orbital Astrolabe Rings
        const ringPrimary = buildAstrolabeRing(THREE, 1.92, 0.024, 0x0ea5e9, 0x0f4c81, 12);
        ringPrimary.rotation.set(Math.PI * 0.35, Math.PI * 0.14, 0);
        rootGroup.add(ringPrimary);

        const ringSecondary = buildAstrolabeRing(THREE, 2.34, 0.021, 0xf97316, 0xc2410c, 16);
        ringSecondary.rotation.set(-Math.PI * 0.28, -Math.PI * 0.22, Math.PI * 0.08);
        rootGroup.add(ringSecondary);

        // 3. Six Orbiting Knowledge-Module Cubes & Octahedra
        const satellites = [];
        const satConfigs = [
            { geo: new THREE.OctahedronGeometry(0.18, 0), color: 0xf97316, emissive: 0xc2410c, radius: 1.92, speed: 0.58, angle: 0.0, parent: ringPrimary, bob: 0.07 },
            { geo: new THREE.BoxGeometry(0.21, 0.21, 0.21), color: 0x0ea5e9, emissive: 0x0369a1, radius: 1.92, speed: 0.58, angle: (Math.PI * 2) / 3, parent: ringPrimary, bob: 0.06 },
            { geo: new THREE.OctahedronGeometry(0.16, 0), color: 0x1d4ed8, emissive: 0x0f4c81, radius: 1.92, speed: 0.58, angle: (Math.PI * 4) / 3, parent: ringPrimary, bob: 0.08 },
            { geo: new THREE.BoxGeometry(0.22, 0.22, 0.22), color: 0xfb923c, emissive: 0xea580c, radius: 2.34, speed: -0.44, angle: 0.55, parent: ringSecondary, bob: 0.09 },
            { geo: new THREE.OctahedronGeometry(0.19, 0), color: 0x38bdf8, emissive: 0x0284c7, radius: 2.34, speed: -0.44, angle: 0.55 + (Math.PI * 2) / 3, parent: ringSecondary, bob: 0.08 },
            { geo: new THREE.BoxGeometry(0.18, 0.18, 0.18), color: 0x0f4c81, emissive: 0x093257, radius: 2.34, speed: -0.44, angle: 0.55 + (Math.PI * 4) / 3, parent: ringSecondary, bob: 0.07 }
        ];

        satConfigs.forEach((cfg) => {
            const mat = new THREE.MeshStandardMaterial({
                color: cfg.color,
                emissive: cfg.emissive,
                emissiveIntensity: 0.38,
                roughness: 0.2,
                metalness: 0.42,
                flatShading: true
            });
            const mesh = new THREE.Mesh(cfg.geo, mat);
            cfg.parent.add(mesh);
            satellites.push({ mesh, ...cfg });
        });

        // 4. Subtle Depth Particle Field
        const particleCount = 140;
        const positions = new Float32Array(particleCount * 3);
        const colors = new Float32Array(particleCount * 3);
        const palette = [
            new THREE.Color(0x0f4c81),
            new THREE.Color(0x1d4ed8),
            new THREE.Color(0x0ea5e9),
            new THREE.Color(0xf97316)
        ];

        for (let i = 0; i < particleCount; i++) {
            const r = 1.65 + Math.random() * 1.85;
            const theta = Math.random() * Math.PI * 2;
            const phi = Math.acos(2 * Math.random() - 1);

            positions[i * 3] = r * Math.sin(phi) * Math.cos(theta);
            positions[i * 3 + 1] = r * Math.sin(phi) * Math.sin(theta);
            positions[i * 3 + 2] = r * Math.cos(phi);

            const picked = palette[i % palette.length];
            colors[i * 3] = picked.r;
            colors[i * 3 + 1] = picked.g;
            colors[i * 3 + 2] = picked.b;
        }

        const particleGeo = new THREE.BufferGeometry();
        particleGeo.setAttribute("position", new THREE.BufferAttribute(positions, 3));
        particleGeo.setAttribute("color", new THREE.BufferAttribute(colors, 3));

        const spriteTexture = createGlowSpriteTexture(THREE);
        const particleMat = new THREE.PointsMaterial({
            size: 0.08,
            map: spriteTexture || null,
            vertexColors: true,
            transparent: true,
            opacity: 0.82,
            depthWrite: false
        });

        const particleSystem = new THREE.Points(particleGeo, particleMat);
        rootGroup.add(particleSystem);

        // Pointer Parallax + Scroll-Linked Rotation State
        let targetPointerX = 0;
        let targetPointerY = 0;
        let smoothPointerX = 0;
        let smoothPointerY = 0;
        let smoothScrollAngle = 0;

        const onPointerMove = (e) => {
            if (reducedMotion.matches) return;
            const nx = (e.clientX / Math.max(window.innerWidth, 1)) * 2 - 1;
            const ny = (e.clientY / Math.max(window.innerHeight, 1)) * 2 - 1;
            targetPointerX = nx * 0.34;
            targetPointerY = ny * 0.22;
        };
        window.addEventListener("pointermove", onPointerMove, { passive: true });

        // Responsive Canvas Sizing via ResizeObserver
        const updateCanvasSize = () => {
            const rect = container.getBoundingClientRect();
            const width = Math.max(Math.floor(rect.width || canvas.clientWidth || 320), 1);
            const height = Math.max(Math.floor(rect.height || canvas.clientHeight || 280), 1);

            camera.aspect = width / height;
            camera.position.z = width < 420 ? 7.25 : 6.55;
            camera.updateProjectionMatrix();
            renderer.setSize(width, height, false);
        };

        updateCanvasSize();

        let rafId = 0;
        let isVisibleInViewport = true;
        let isPageVisible = !document.hidden;
        const clock = new THREE.Clock();

        const renderSceneFrame = (elapsed) => {
            if (!reducedMotion.matches) {
                smoothPointerX += (targetPointerX - smoothPointerX) * 0.055;
                smoothPointerY += (targetPointerY - smoothPointerY) * 0.055;

                const targetScrollAngle = Math.min(window.scrollY || 0, 900) * 0.0014;
                smoothScrollAngle += (targetScrollAngle - smoothScrollAngle) * 0.07;

                rootGroup.rotation.y = smoothPointerX + smoothScrollAngle;
                rootGroup.rotation.x = smoothPointerY + smoothScrollAngle * 0.35;
                rootGroup.position.y = Math.sin(elapsed * 1.1) * 0.09;

                coreMesh.rotation.y = elapsed * 0.26;
                coreMesh.rotation.x = elapsed * 0.14;
                innerPrismMesh.rotation.y = -elapsed * 0.58;
                innerPrismMesh.rotation.z = elapsed * 0.32;
                cageWireframe.rotation.y = -elapsed * 0.16;
                cageWireframe.rotation.z = elapsed * 0.11;

                ringPrimary.rotation.z = elapsed * 0.08;
                ringSecondary.rotation.z = -elapsed * 0.06;

                particleSystem.rotation.y = elapsed * 0.05;
                particleSystem.rotation.x = Math.sin(elapsed * 0.24) * 0.07;
            } else {
                coreMesh.rotation.set(0.32, 0.52, 0.08);
                cageWireframe.rotation.set(-0.2, 0.3, 0.15);
            }

            satellites.forEach((sat, idx) => {
                const currentAngle =
                    sat.angle + (reducedMotion.matches ? 0 : elapsed * sat.speed);
                sat.mesh.position.set(
                    Math.cos(currentAngle) * sat.radius,
                    Math.sin(currentAngle) * sat.radius,
                    Math.sin(elapsed * 1.5 + idx) * sat.bob
                );
                if (!reducedMotion.matches) {
                    sat.mesh.rotation.x = elapsed * 0.85 + idx;
                    sat.mesh.rotation.y = elapsed * 0.65 + idx;
                }
            });

            renderer.render(scene, camera);
        };

        const tick = () => {
            rafId = 0;
            if (!isVisibleInViewport || !isPageVisible || reducedMotion.matches) {
                return;
            }
            const elapsed = clock.getElapsedTime();
            renderSceneFrame(elapsed);
            rafId = requestAnimationFrame(tick);
        };

        const syncAnimationLoop = () => {
            if (reducedMotion.matches) {
                if (rafId) {
                    cancelAnimationFrame(rafId);
                    rafId = 0;
                }
                renderSceneFrame(0);
                return;
            }
            if (isVisibleInViewport && isPageVisible && !rafId) {
                clock.getDelta();
                rafId = requestAnimationFrame(tick);
            } else if ((!isVisibleInViewport || !isPageVisible) && rafId) {
                cancelAnimationFrame(rafId);
                rafId = 0;
            }
        };

        renderSceneFrame(0);
        canvas.dataset.threeReady = "true";
        syncAnimationLoop();

        let ro = null;
        if (typeof ResizeObserver === "function") {
            ro = new ResizeObserver(() => {
                updateCanvasSize();
                if (reducedMotion.matches || !rafId) {
                    renderSceneFrame(clock.getElapsedTime());
                }
            });
            ro.observe(container);
        } else {
            window.addEventListener("resize", updateCanvasSize, { passive: true });
        }

        let io = null;
        if (typeof IntersectionObserver === "function") {
            io = new IntersectionObserver(
                (entries) => {
                    entries.forEach((entry) => {
                        isVisibleInViewport = entry.isIntersecting;
                    });
                    syncAnimationLoop();
                },
                { threshold: 0.02 }
            );
            io.observe(canvas);
        }

        const onVisibilityChange = () => {
            isPageVisible = !document.hidden;
            syncAnimationLoop();
        };
        document.addEventListener("visibilitychange", onVisibilityChange);

        if (typeof reducedMotion.addEventListener === "function") {
            reducedMotion.addEventListener("change", syncAnimationLoop);
        }

        // Clean GPU disposal on page unload
        window.addEventListener(
            "pagehide",
            () => {
                if (rafId) cancelAnimationFrame(rafId);
                if (ro) ro.disconnect();
                if (io) io.disconnect();
                window.removeEventListener("pointermove", onPointerMove);
                document.removeEventListener("visibilitychange", onVisibilityChange);
                scene.traverse((obj) => {
                    if (obj.geometry) obj.geometry.dispose();
                    if (obj.material) {
                        if (Array.isArray(obj.material)) {
                            obj.material.forEach((m) => m.dispose());
                        } else {
                            obj.material.dispose();
                        }
                    }
                });
                if (spriteTexture) spriteTexture.dispose();
                renderer.dispose();
            },
            { once: true }
        );
    } catch (_err) {
        canvas.dataset.webglFallback = "true";
    }
}

if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", mountHuitHero3D);
} else {
    mountHuitHero3D();
}
