'use client';

import { Backgrounds } from '@xpeak/shared';
import { useEffect, useRef } from 'react';

import { cn } from '@/lib/utils';

import type { BackgroundManifest, BackgroundProps } from './types';

const PERMUTATION = [
    151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140,
    36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148, 247, 120,
    234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33,
    88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71,
    134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133,
    230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54, 65, 25, 63, 161,
    1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196, 135, 130,
    116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250,
    124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227,
    47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213, 119, 248, 152, 2, 44,
    154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9, 129, 22, 39, 253, 19, 98,
    108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228, 251, 34,
    242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14,
    239, 107, 49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121,
    50, 45, 127, 4, 150, 254, 138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243,
    141, 128, 195, 78, 66, 215, 61, 156, 180,
];

interface FluidTuning {
    particleCount?: number;
    noiseIntensity?: number;
    particleSizeMin?: number;
    particleSizeMax?: number;
    color?: string;
    backgroundColor?: string;
}

interface Particle {
    x: number;
    y: number;
    size: number;
    life: number;
    maxLife: number;
}

function fade(t: number) {
    return t * t * t * (t * (t * 6 - 15) + 10);
}

function lerp(t: number, a: number, b: number) {
    return a + t * (b - a);
}

function grad(hash: number, x: number, y: number, z: number) {
    const h = hash & 15;
    const u = h < 8 ? x : y;
    const v = h < 4 ? y : h === 12 || h === 14 ? x : z;
    return ((h & 1) === 0 ? u : -u) + ((h & 2) === 0 ? v : -v);
}

function buildNoise() {
    const p = new Array<number>(512);
    for (let i = 0; i < 256; i += 1) {
        p[i] = PERMUTATION[i] ?? 0;
        p[256 + i] = PERMUTATION[i] ?? 0;
    }
    return (x: number, y: number, z: number) => {
        const X = Math.floor(x) & 255;
        const Y = Math.floor(y) & 255;
        const Z = Math.floor(z) & 255;
        const xf = x - Math.floor(x);
        const yf = y - Math.floor(y);
        const zf = z - Math.floor(z);
        const u = fade(xf);
        const v = fade(yf);
        const w = fade(zf);
        const A = (p[X] ?? 0) + Y;
        const AA = (p[A] ?? 0) + Z;
        const AB = (p[A + 1] ?? 0) + Z;
        const B = (p[X + 1] ?? 0) + Y;
        const BA = (p[B] ?? 0) + Z;
        const BB = (p[B + 1] ?? 0) + Z;
        return lerp(
            w,
            lerp(
                v,
                lerp(u, grad(p[AA] ?? 0, xf, yf, zf), grad(p[BA] ?? 0, xf - 1, yf, zf)),
                lerp(
                    u,
                    grad(p[AB] ?? 0, xf, yf - 1, zf),
                    grad(p[BB] ?? 0, xf - 1, yf - 1, zf),
                ),
            ),
            lerp(
                v,
                lerp(
                    u,
                    grad(p[AA + 1] ?? 0, xf, yf, zf - 1),
                    grad(p[BA + 1] ?? 0, xf - 1, yf, zf - 1),
                ),
                lerp(
                    u,
                    grad(p[AB + 1] ?? 0, xf, yf - 1, zf - 1),
                    grad(p[BB + 1] ?? 0, xf - 1, yf - 1, zf - 1),
                ),
            ),
        );
    };
}

function FluidParticlesBackground({
    backgroundColor = 'rgba(2, 6, 23, 0.12)',
    className,
    color = '56, 189, 248',
    noiseIntensity = 0.003,
    particleCount = 800,
    particleSizeMin = 0.5,
    particleSizeMax = 2,
}: BackgroundProps & FluidTuning) {
    const rootRef = useRef<HTMLDivElement>(null);
    const canvasRef = useRef<HTMLCanvasElement>(null);

    useEffect(() => {
        const root = rootRef.current;
        const canvas = canvasRef.current;
        if (!root || !canvas) {
            return;
        }
        const context = canvas.getContext('2d', { alpha: true });
        if (!context) {
            return;
        }
        const renderRoot = root;
        const renderCanvas = canvas;
        const renderContext = context;
        const noise = buildNoise();

        let particles: Particle[] = [];
        let frame: number | undefined;
        let visible = true;
        let reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

        function seedParticles(width: number, height: number) {
            particles = Array.from({ length: particleCount }, () => ({
                x: Math.random() * width,
                y: Math.random() * height,
                size:
                    Math.random() * (particleSizeMax - particleSizeMin) + particleSizeMin,
                life: Math.random() * 100,
                maxLife: 100 + Math.random() * 50,
            }));
        }

        function resize() {
            const bounds = renderRoot.getBoundingClientRect();
            const ratio = Math.min(window.devicePixelRatio || 1, 2);
            const width = Math.max(1, Math.round(bounds.width * ratio));
            const height = Math.max(1, Math.round(bounds.height * ratio));
            if (renderCanvas.width !== width || renderCanvas.height !== height) {
                renderCanvas.width = width;
                renderCanvas.height = height;
            }
            renderContext.setTransform(ratio, 0, 0, ratio, 0, 0);
            seedParticles(bounds.width, bounds.height);
        }

        function drawTick() {
            const bounds = renderRoot.getBoundingClientRect();
            renderContext.fillStyle = backgroundColor;
            renderContext.fillRect(0, 0, bounds.width, bounds.height);

            const time = Date.now() * 0.0001;
            for (const particle of particles) {
                particle.life += 1;
                if (particle.life > particle.maxLife) {
                    particle.life = 0;
                    particle.x = Math.random() * bounds.width;
                    particle.y = Math.random() * bounds.height;
                }
                const opacity =
                    Math.sin((particle.life / particle.maxLife) * Math.PI) * 0.35;
                const n = noise(
                    particle.x * noiseIntensity,
                    particle.y * noiseIntensity,
                    time,
                );
                const angle = n * Math.PI * 4;
                particle.x += Math.cos(angle) * 2;
                particle.y += Math.sin(angle) * 2;
                if (particle.x < 0) particle.x = bounds.width;
                if (particle.x > bounds.width) particle.x = 0;
                if (particle.y < 0) particle.y = bounds.height;
                if (particle.y > bounds.height) particle.y = 0;

                renderContext.fillStyle = `rgba(${color}, ${opacity})`;
                renderContext.beginPath();
                renderContext.arc(particle.x, particle.y, particle.size, 0, Math.PI * 2);
                renderContext.fill();
            }
        }

        function tick() {
            drawTick();
            frame =
                visible && !reduceMotion ? window.requestAnimationFrame(tick) : undefined;
        }

        function start() {
            if (frame !== undefined || !visible || reduceMotion) {
                return;
            }
            frame = window.requestAnimationFrame(tick);
        }

        function stop() {
            if (frame === undefined) {
                return;
            }
            window.cancelAnimationFrame(frame);
            frame = undefined;
        }

        const motionPreference = window.matchMedia('(prefers-reduced-motion: reduce)');
        const handleMotionPreference = () => {
            reduceMotion = motionPreference.matches;
            if (reduceMotion) {
                stop();
                drawTick();
            } else {
                start();
            }
        };

        const resizeObserver = new ResizeObserver(() => {
            resize();
            drawTick();
        });
        const visibilityObserver = new IntersectionObserver(([entry]) => {
            visible = Boolean(entry?.isIntersecting);
            if (visible) {
                start();
            } else {
                stop();
            }
        });

        resize();
        drawTick();
        start();
        resizeObserver.observe(root);
        visibilityObserver.observe(root);
        motionPreference.addEventListener('change', handleMotionPreference);

        return () => {
            stop();
            resizeObserver.disconnect();
            visibilityObserver.disconnect();
            motionPreference.removeEventListener('change', handleMotionPreference);
        };
    }, [
        backgroundColor,
        color,
        noiseIntensity,
        particleCount,
        particleSizeMax,
        particleSizeMin,
    ]);

    return (
        <div
            ref={rootRef}
            aria-hidden="true"
            className={cn('relative overflow-hidden bg-slate-950', className)}
        >
            <canvas
                ref={canvasRef}
                aria-hidden="true"
                className="absolute inset-0 h-full w-full"
            />
        </div>
    );
}

function FluidDefault({ className }: BackgroundProps) {
    return <FluidParticlesBackground className={className} />;
}

export const fluidManifest: BackgroundManifest = {
    slug: Backgrounds.Fluid,
    name: 'Fluido',
    description: 'Partículas em canvas 2D seguindo um campo de Perlin noise.',
    Component: FluidDefault,
};
