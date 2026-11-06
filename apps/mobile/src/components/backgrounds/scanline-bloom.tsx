'use client';

import { Backgrounds } from '@xpeak/shared';
import { useEffect, useRef } from 'react';

import { cn } from '@/lib/utils';

import type { BackgroundManifest, BackgroundProps } from './types';

type Bloom = {
    kind: 'leaf' | 'lily' | 'rose' | 'bud';
    x: number;
    y: number;
    r: number;
    a: number;
    k: number;
    b: number;
    tone: number;
};

const TAU = Math.PI * 2;
const INK = '#38bdf8';
const HIGHLIGHT = '#bae6fd';
const BACKGROUND = '#020617';

function mulberry32(seed: number) {
    let s = seed >>> 0;
    return () => {
        s = (s + 0x6d2b79f5) >>> 0;
        let t = s;
        t = Math.imul(t ^ (t >>> 15), t | 1);
        t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
        return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
}

function hexToRgb(hex: string): [number, number, number] {
    let h = hex.trim().replace(/^#/, '');
    if (h.length === 3) h = h.replace(/./g, '$&$&');
    const n = /^[0-9a-f]{6}$/i.test(h) ? parseInt(h, 16) : 0;
    return [((n >> 16) & 255) / 255, ((n >> 8) & 255) / 255, (n & 255) / 255];
}

function layoutBouquet(seed: number, aspect: number) {
    const rnd = mulberry32(seed);
    const jit = (v: number, amt: number) => v + (rnd() * 2 - 1) * amt;
    const tall = aspect < 0.85;
    const heart: [number, number] = [jit(0.5, 0.05), jit(tall ? 0.52 : 0.58, 0.04)];
    const leaves: Bloom[] = [];
    const flowers: Bloom[] = [];
    const buds: Bloom[] = [];

    for (const dir of [-2.55, -2.1, -1.7, -1.3, -0.85, -0.35, 0.15, 0.6, 2.5, 2.95, 3.4]) {
        leaves.push({
            kind: 'leaf',
            x: heart[0] + jit(0, 0.03),
            y: heart[1] + jit(0, 0.03),
            r: jit(tall ? 0.4 : 0.47, 0.08),
            a: jit(dir, 0.14),
            k: jit(0.27, 0.05),
            b: jit(0, 0.3),
            tone: jit(0.55, 0.15),
        });
    }

    const spots: Array<[number, number, number]> = tall
        ? [[0.5, 0.1, 0.32], [0.26, 0.4, 0.26], [0.74, 0.63, 0.28], [0.3, 0.88, 0.29]]
        : [[0.47, 0.12, 0.33], [0.24, 0.8, 0.29], [0.7, 0.8, 0.32]];
    spots.forEach(([x, y, r], i) => {
        const kind = rnd() < (i === 0 ? 0.75 : 0.5) ? 'lily' : 'rose';
        const tucked = 2 + Math.floor(rnd() * 2);
        for (let l = 0; l < tucked; l++) {
            leaves.push({
                kind: 'leaf',
                x: x + jit(0, 0.02),
                y: y + jit(0, 0.02),
                r: r * jit(1.45, 0.25),
                a: rnd() * TAU,
                k: jit(0.3, 0.05),
                b: jit(0, 0.35),
                tone: jit(0.5, 0.15),
            });
        }
        flowers.push({
            kind,
            x: jit(x, 0.05),
            y: jit(y, 0.04),
            r: jit(r, 0.035) * (kind === 'rose' ? 0.85 : 1),
            a: rnd() * TAU,
            k: jit(0.78, 0.14),
            b: 0,
            tone: jit(0.94, 0.05),
        });
    });
    if (!flowers.some((f) => f.kind === 'rose')) {
        flowers[1] = { ...flowers[1]!, kind: 'rose', r: flowers[1]!.r * 0.85 };
    }
    if (!flowers.some((f) => f.kind === 'lily')) {
        flowers[2] = { ...flowers[2]!, kind: 'lily' };
    }

    const budSpots: Array<[number, number]> = [
        [0.19, 0.3],
        [0.84, 0.17],
        [0.9, 0.56],
    ];
    for (const [x, y] of budSpots) {
        const bx = jit(x, 0.04);
        const by = jit(y, 0.04);
        buds.push({
            kind: 'bud',
            x: bx,
            y: by,
            r: jit(0.075, 0.02),
            a: Math.atan2(by - heart[1], (bx - heart[0]) * aspect) + jit(0, 0.3),
            k: 0,
            b: 0,
            tone: jit(0.8, 0.1),
        });
    }

    flowers.sort((p, q) => p.y - q.y);
    return { heart, blooms: [...leaves, ...buds, ...flowers] };
}

function paintSize(w: number, h: number, dpr: number): [number, number] {
    const scale = Math.min(Math.min(dpr, 1.5), 1600 / Math.max(w, h, 1));
    return [Math.max(2, Math.round(w * scale)), Math.max(2, Math.round(h * scale))];
}

const gray = (v: number) => {
    const c = Math.round(Math.min(1, Math.max(0, v)) * 255);
    return `rgb(${c},${c},${c})`;
};
const shade = (a: number) => `rgba(0,0,0,${a})`;
const glow = (a: number) => `rgba(255,255,255,${a})`;

function paintLeaf(ctx: CanvasRenderingContext2D, x: number, y: number, len: number, leaf: Bloom) {
    const wid = len * leaf.k;
    const off = (f: number) => leaf.b * len * f * f;
    const t = leaf.tone;
    ctx.save();
    ctx.translate(x, y);
    ctx.rotate(leaf.a);

    const upper = new Path2D();
    upper.moveTo(0, 0);
    upper.bezierCurveTo(len * 0.22, -wid * 1.05 + off(0.22), len * 0.68, -wid * 0.8 + off(0.68), len, off(1));
    upper.quadraticCurveTo(len * 0.5, 0, 0, 0);
    const lower = new Path2D();
    lower.moveTo(0, 0);
    lower.bezierCurveTo(len * 0.22, wid * 0.95 + off(0.22), len * 0.68, wid * 0.72 + off(0.68), len, off(1));
    lower.quadraticCurveTo(len * 0.5, 0, 0, 0);
    const whole = new Path2D();
    whole.addPath(upper);
    whole.addPath(lower);

    const mid = off(0.5);
    const up = ctx.createLinearGradient(0, mid, 0, mid - wid * 0.9);
    up.addColorStop(0, gray(t * 0.42));
    up.addColorStop(0.35, gray(t * 0.78));
    up.addColorStop(1, gray(t * 0.98));
    ctx.fillStyle = up;
    ctx.fill(upper);
    const low = ctx.createLinearGradient(0, mid, 0, mid + wid * 0.85);
    low.addColorStop(0, gray(t * 0.2));
    low.addColorStop(0.5, gray(t * 0.46));
    low.addColorStop(1, gray(t * 0.3));
    ctx.fillStyle = low;
    ctx.fill(lower);

    ctx.save();
    ctx.clip(whole);
    const along = ctx.createLinearGradient(0, 0, len, 0);
    along.addColorStop(0, shade(0.85));
    along.addColorStop(0.3, shade(0.15));
    along.addColorStop(0.85, shade(0));
    along.addColorStop(1, shade(0.25));
    ctx.fillStyle = along;
    ctx.fillRect(-4, -len, len + 8, len * 2);
    ctx.lineCap = 'round';
    for (let i = 1; i < 8; i++) {
        const f = i / 8.5;
        const g = Math.min(1, f + 0.16);
        for (const side of [-1, 1]) {
            ctx.beginPath();
            ctx.moveTo(len * f, off(f));
            ctx.quadraticCurveTo(
                len * (f + 0.07),
                off(f + 0.07) + side * wid * 0.35,
                len * g,
                off(g) + side * wid * 0.85 * (1 - f * 0.8),
            );
            ctx.strokeStyle = shade(0.32);
            ctx.lineWidth = Math.max(1, wid * 0.035);
            ctx.stroke();
        }
    }
    ctx.restore();

    ctx.beginPath();
    ctx.moveTo(0, 0);
    ctx.quadraticCurveTo(len * 0.5, 0, len * 0.97, off(0.97));
    ctx.strokeStyle = shade(0.6);
    ctx.lineWidth = Math.max(1, wid * 0.09);
    ctx.stroke();
    ctx.beginPath();
    ctx.moveTo(len * 0.05, -wid * 0.06);
    ctx.quadraticCurveTo(len * 0.5, -wid * 0.06, len * 0.9, off(0.9) - wid * 0.04);
    ctx.strokeStyle = glow(0.22);
    ctx.lineWidth = Math.max(1, wid * 0.03);
    ctx.stroke();

    ctx.strokeStyle = shade(0.7);
    ctx.lineWidth = Math.max(1, len * 0.006);
    ctx.stroke(whole);
    ctx.restore();
}

function petalPath(len: number, wid: number, curl: number) {
    const p = new Path2D();
    p.moveTo(0, 0);
    p.bezierCurveTo(len * 0.25, -wid * 1.1, len * 0.66, -wid * 0.95, len, curl);
    p.bezierCurveTo(len * 0.66, wid * 0.8, len * 0.25, wid * 1.0, 0, 0);
    return p;
}

function paintLily(ctx: CanvasRenderingContext2D, x: number, y: number, R: number, lily: Bloom, rnd: () => number) {
    ctx.save();
    ctx.translate(x, y);
    ctx.rotate(lily.a);
    ctx.scale(1, lily.k);

    for (const back of [true, false]) {
        for (let i = 0; i < 3; i++) {
            const ang = (i * TAU) / 3 + (back ? Math.PI / 3 : 0) + (rnd() - 0.5) * 0.25;
            const len = R * (back ? 0.9 : 1) * (0.92 + rnd() * 0.12);
            const wid = R * (back ? 0.29 : 0.34);
            const curl = wid * (rnd() - 0.3) * 0.5;
            const tone = lily.tone * (back ? 0.68 : 1);
            ctx.save();
            ctx.rotate(ang);
            const petal = petalPath(len, wid, curl);
            const g = ctx.createLinearGradient(0, 0, len, 0);
            g.addColorStop(0, gray(0.04));
            g.addColorStop(0.16, gray(tone * 0.32));
            g.addColorStop(0.5, gray(tone * 0.97));
            g.addColorStop(0.82, gray(tone * 0.86));
            g.addColorStop(1, gray(tone * 0.5));
            ctx.fillStyle = g;
            ctx.fill(petal);

            ctx.save();
            ctx.clip(petal);
            const side = ctx.createLinearGradient(0, -wid, 0, wid);
            side.addColorStop(0, shade(0));
            side.addColorStop(0.48, shade(0.05));
            side.addColorStop(0.55, shade(0.4));
            side.addColorStop(1, shade(0.55));
            ctx.fillStyle = side;
            ctx.fillRect(0, -wid * 1.2, len, wid * 2.4);
            for (let s = 0; s < 22; s++) {
                const fx = len * (0.14 + rnd() * 0.36);
                const fy = (rnd() - 0.5) * wid * 1.1 * (fx / len + 0.3);
                ctx.beginPath();
                ctx.arc(fx, fy, R * (0.006 + rnd() * 0.01), 0, TAU);
                ctx.fillStyle = shade(0.75);
                ctx.fill();
            }
            ctx.restore();

            ctx.beginPath();
            ctx.moveTo(len * 0.08, 0);
            ctx.quadraticCurveTo(len * 0.5, wid * 0.05, len * 0.86, curl * 0.8);
            ctx.strokeStyle = shade(0.55);
            ctx.lineWidth = Math.max(1, wid * 0.1);
            ctx.stroke();
            ctx.beginPath();
            ctx.moveTo(len * 0.12, -wid * 0.1);
            ctx.quadraticCurveTo(len * 0.5, -wid * 0.08, len * 0.8, curl * 0.8 - wid * 0.08);
            ctx.strokeStyle = glow(0.28);
            ctx.lineWidth = Math.max(1, wid * 0.04);
            ctx.stroke();

            ctx.strokeStyle = shade(0.65);
            ctx.lineWidth = Math.max(1, R * 0.01);
            ctx.stroke(petal);
            ctx.restore();
        }
    }

    const throat = ctx.createRadialGradient(0, 0, 0, 0, 0, R * 0.24);
    throat.addColorStop(0, shade(0.95));
    throat.addColorStop(1, shade(0));
    ctx.fillStyle = throat;
    ctx.beginPath();
    ctx.arc(0, 0, R * 0.24, 0, TAU);
    ctx.fill();

    ctx.lineCap = 'round';
    for (let i = 0; i < 7; i++) {
        const pistil = i === 6;
        const ang = pistil ? rnd() * TAU : (i * TAU) / 6 + (rnd() - 0.5) * 0.6;
        const len = R * (pistil ? 0.62 : 0.45 + rnd() * 0.15);
        const ex = Math.cos(ang) * len;
        const ey = Math.sin(ang) * len;
        const bow = (rnd() - 0.5) * R * 0.2;
        ctx.beginPath();
        ctx.moveTo(0, 0);
        ctx.quadraticCurveTo(ex * 0.5 - Math.sin(ang) * bow, ey * 0.5 + Math.cos(ang) * bow, ex, ey);
        ctx.strokeStyle = shade(0.8);
        ctx.lineWidth = Math.max(1.5, R * 0.022);
        ctx.stroke();
        ctx.strokeStyle = gray(0.72);
        ctx.lineWidth = Math.max(1, R * 0.011);
        ctx.stroke();
        ctx.save();
        ctx.translate(ex, ey);
        ctx.rotate(ang + Math.PI / 2 + (rnd() - 0.5) * 0.8);
        ctx.beginPath();
        if (pistil) ctx.arc(0, 0, R * 0.03, 0, TAU);
        else ctx.ellipse(0, 0, R * 0.06, R * 0.022, 0, 0, TAU);
        const pollen = ctx.createLinearGradient(0, -R * 0.022, 0, R * 0.022);
        pollen.addColorStop(0, gray(pistil ? 0.8 : 0.9));
        pollen.addColorStop(1, gray(0.35));
        ctx.fillStyle = pollen;
        ctx.fill();
        ctx.restore();
    }
    ctx.restore();
}

function paintRose(ctx: CanvasRenderingContext2D, x: number, y: number, R: number, rose: Bloom, rnd: () => number) {
    ctx.save();
    ctx.translate(x, y);
    ctx.rotate(rose.a);
    ctx.lineJoin = 'round';
    const t = rose.tone;

    const petal = (cx: number, cy: number, rx: number, ry: number, rot: number, reach: number, tone: number) => {
        ctx.beginPath();
        const k = 3 + Math.floor(rnd() * 3);
        const ph = rnd() * TAU;
        for (let j = 0; j <= 64; j++) {
            const th = (j / 64) * TAU;
            const outer = Math.max(0, Math.cos(th));
            const w =
                1 +
                0.05 * Math.sin(k * th + ph) * outer +
                0.025 * Math.sin(11 * th + ph * 2) -
                0.07 * Math.pow(outer, 24);
            const ex = Math.cos(th) * rx * w;
            const ey = Math.sin(th) * ry * w;
            const px = cx + ex * Math.cos(rot) - ey * Math.sin(rot);
            const py = cy + ex * Math.sin(rot) + ey * Math.cos(rot);
            if (j) ctx.lineTo(px, py);
            else ctx.moveTo(px, py);
        }
        ctx.closePath();
        const g = ctx.createRadialGradient(0, 0, reach * 0.1, 0, 0, reach);
        g.addColorStop(0, gray(0.03));
        g.addColorStop(0.55, gray(tone * 0.45));
        g.addColorStop(0.9, gray(tone * 0.95));
        g.addColorStop(1, gray(tone));
        ctx.fillStyle = g;
        ctx.fill();
        ctx.strokeStyle = shade(0.75);
        ctx.lineWidth = Math.max(1.5, R * 0.014);
        ctx.stroke();
        ctx.beginPath();
        ctx.ellipse(cx, cy, rx * 0.94, ry * 0.94, rot, -1.1, 1.1);
        ctx.strokeStyle = glow(0.3);
        ctx.lineWidth = Math.max(1, R * 0.01);
        ctx.stroke();
    };

    for (let i = 0; i < 5; i++) {
        const ang = (i * TAU) / 5 + (rnd() - 0.5) * 0.4;
        const d = R * 0.42;
        petal(Math.cos(ang) * d, Math.sin(ang) * d, R * 0.6, R * 0.46, ang, R, t * 0.85);
    }
    const layers = 16;
    for (let i = 0; i < layers; i++) {
        const f = 1 - i / layers;
        const rr = R * 0.72 * Math.pow(f, 0.9);
        const ang = i * 2.39996 + rnd() * 0.2;
        const d = rr * 0.28;
        petal(Math.cos(ang) * d, Math.sin(ang) * d, rr * 0.78, rr * 0.6, ang, rr * 1.1, t * (0.7 + 0.3 * f));
    }
    ctx.beginPath();
    ctx.arc(0, 0, R * 0.05, 0, TAU);
    ctx.fillStyle = shade(0.9);
    ctx.fill();
    ctx.restore();
}

function paintBud(ctx: CanvasRenderingContext2D, x: number, y: number, R: number, bud: Bloom) {
    ctx.save();
    ctx.translate(x, y);
    ctx.rotate(bud.a);
    const body = new Path2D();
    body.moveTo(0, 0);
    body.bezierCurveTo(R * 0.2, -R * 0.55, R * 0.9, -R * 0.35, R * 1.5, 0);
    body.bezierCurveTo(R * 0.9, R * 0.35, R * 0.2, R * 0.55, 0, 0);
    const g = ctx.createLinearGradient(0, -R * 0.45, 0, R * 0.45);
    g.addColorStop(0, gray(bud.tone));
    g.addColorStop(0.45, gray(bud.tone * 0.7));
    g.addColorStop(1, gray(bud.tone * 0.2));
    ctx.fillStyle = g;
    ctx.fill(body);
    ctx.strokeStyle = shade(0.7);
    ctx.lineWidth = Math.max(1, R * 0.04);
    ctx.stroke(body);
    ctx.beginPath();
    ctx.moveTo(R * 0.1, 0);
    ctx.quadraticCurveTo(R * 0.8, -R * 0.08, R * 1.45, 0);
    ctx.strokeStyle = shade(0.5);
    ctx.stroke();
    for (const side of [-1, 1]) {
        ctx.beginPath();
        ctx.moveTo(0, 0);
        ctx.quadraticCurveTo(R * 0.3, side * R * 0.5, R * 0.75, side * R * 0.32);
        ctx.quadraticCurveTo(R * 0.3, side * R * 0.2, 0, 0);
        ctx.fillStyle = gray(0.3);
        ctx.fill();
        ctx.stroke();
    }
    ctx.restore();
}

function paintBouquet(canvas: HTMLCanvasElement, w: number, h: number, seed: number) {
    canvas.width = w;
    canvas.height = h;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    ctx.fillStyle = '#000';
    ctx.fillRect(0, 0, w, h);
    const unit = Math.sqrt(w * h);
    const { heart, blooms } = layoutBouquet(seed, w / h);
    const rnd = mulberry32(seed ^ 0x5bd1e995);
    const hx = heart[0] * w;
    const hy = heart[1] * h;

    ctx.lineCap = 'round';
    for (const b of blooms) {
        if (b.kind === 'leaf') continue;
        const ex = b.x * w;
        const ey = b.y * h;
        ctx.beginPath();
        ctx.moveTo(hx, hy);
        ctx.quadraticCurveTo((hx + ex) / 2 + (rnd() - 0.5) * unit * 0.15, (hy + ey) / 2, ex, ey);
        ctx.strokeStyle = shade(1);
        ctx.lineWidth = unit * 0.016;
        ctx.stroke();
        ctx.strokeStyle = gray(0.4);
        ctx.lineWidth = unit * 0.008;
        ctx.stroke();
    }

    for (const b of blooms) {
        const x = b.x * w;
        const y = b.y * h;
        const size = b.r * unit;
        if (b.kind === 'leaf') paintLeaf(ctx, x, y, size, b);
        else if (b.kind === 'bud') paintBud(ctx, x, y, size, b);
        else if (b.kind === 'lily') paintLily(ctx, x, y, size, b, rnd);
        else paintRose(ctx, x, y, size, b, rnd);
    }
}

const VERT = `
attribute vec2 a_position;
void main() {
  gl_Position = vec4(a_position, 0.0, 1.0);
}
`;

const FRAG = `
#ifdef GL_FRAGMENT_PRECISION_HIGH
precision highp float;
#else
precision mediump float;
#endif

uniform sampler2D u_tex;
uniform vec4 u_scene;    // resolution.xy, time, dpr
uniform vec4 u_print;    // line spacing, mode, dot size, sway
uniform vec4 u_fx;       // _, _, intro, scan offset
uniform vec4 u_frame;    // aspect, _, frame inset, field seed
uniform vec4 u_post;     // grain, vignette
uniform vec3 u_ink;
uniform vec3 u_hot;
uniform vec3 u_bg;
uniform vec3 u_frameColor;

float hash(vec2 p) {
  vec3 p3 = fract(vec3(p.xyx) * 0.1031);
  p3 += dot(p3, p3.yzx + 33.33);
  return fract((p3.x + p3.y) * p3.z);
}

float noise(vec2 p) {
  vec2 i = floor(p);
  vec2 f = fract(p);
  vec2 u = f * f * (3.0 - 2.0 * f);
  return mix(
    mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x),
    mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x),
    u.y);
}

vec2 cover(vec2 uv, float texAspect) {
  float ca = u_scene.x / u_scene.y;
  vec2 s = ca > texAspect ? vec2(1.0, texAspect / ca) : vec2(ca / texAspect, 1.0);
  return clamp((uv - 0.5) * s + 0.5, 0.0, 1.0);
}

float scanned(float fromTop, float row, float progress) {
  float front = progress * 1.2 - 0.1;
  return step(fromTop + (hash(vec2(row, 3.7)) - 0.5) * 0.05, front);
}

void main() {
  vec2 res = u_scene.xy;
  float time = u_scene.z;
  float dpr = u_scene.w;
  vec2 frag = gl_FragCoord.xy;
  vec2 uv = frag / res;
  float fromTop = 1.0 - uv.y;

  float sp = max(u_print.x, 1.5) * dpr;
  float ly = frag.y + u_fx.w * dpr;
  float row = floor(ly / sp);
  float band = floor(frag.y / (sp * 3.0));

  vec2 s = uv;
  s += u_print.w * 0.004 * vec2(
    sin(uv.y * 7.0 + time * 0.8) + 0.5 * sin(uv.y * 17.0 - time * 1.3),
    0.6 * sin(uv.x * 5.0 + time * 0.6));

  float tone = texture2D(u_tex, cover(s, u_frame.x)).r;
  tone *= scanned(fromTop, row, u_fx.z);
  tone = clamp((tone - 0.03) * 1.12, 0.0, 1.0);

  float field = noise(uv * vec2(2.2, 2.8) + u_frame.w * 1.37);
  float m = smoothstep(0.5, 0.18, uv.y + (field - 0.5) * 0.55);
  m = clamp(m * min(u_print.y, 1.0) + max(u_print.y - 1.0, 0.0), 0.0, 1.0);

  float across = abs(fract(ly / sp) - 0.5) * 2.0;
  float rag = (noise(vec2(frag.x / (dpr * 2.5), row * 1.7)) - 0.5) * 0.45;
  float th = clamp(pow(tone, 0.8) * 1.05 + rag * tone, 0.0, 1.0);
  float aa = 1.4 / sp;
  float lines = 1.0 - smoothstep(th - aa, th + aa, across);
  float dotPx = max(1.0, floor(u_print.z * dpr + 0.5));
  vec2 cell = floor(frag / dotPx);
  lines *= step(0.1, hash(cell + 7.3)) * step(0.015, tone);

  float stipple = step(hash(cell + 17.0), pow(tone, 1.3) * 1.04);
  float lit = hash(cell + 91.0) < m ? stipple : lines;

  float grain = u_post.x;
  vec3 paper = u_bg * (1.0 - grain * 0.35 + grain * 0.7 * hash(cell + 5.0));
  paper += u_bg * (noise(frag / (dpr * 40.0)) - 0.5) * grain * 0.6;
  paper += u_ink * tone * 0.1;
  if (u_frame.z > 0.0) {
    vec2 edge = min(frag, res - frag) / dpr;
    float d = abs(min(edge.x, edge.y) - u_frame.z);
    float inside = step(u_frame.z - 1.0, min(edge.x, edge.y));
    paper = mix(paper, u_frameColor, (1.0 - smoothstep(0.5, 1.3, d)) * inside * 0.9);
  }

  vec3 ink = mix(u_ink * 0.5, u_ink, smoothstep(0.08, 0.55, tone));
  ink = mix(ink, u_hot, smoothstep(0.7, 1.0, tone) * 0.75);
  vec3 col = mix(paper, ink, lit);

  float introFront = u_fx.z * 1.2 - 0.1;
  col += u_hot * 0.55 * lines * exp(-abs(fromTop - introFront) * 60.0) * step(u_fx.z, 0.999);

  float vd = length(uv - 0.5) * 1.41421356;
  col *= 1.0 - u_post.y * smoothstep(0.4, 1.05, vd);
  gl_FragColor = vec4(clamp(col, 0.0, 1.0), 1.0);
}
`;

function compile(gl: WebGLRenderingContext, vert: string, frag: string) {
    const program = gl.createProgram();
    if (!program) return null;
    for (const [type, source] of [
        [gl.VERTEX_SHADER, vert],
        [gl.FRAGMENT_SHADER, frag],
    ] as const) {
        const shader = gl.createShader(type);
        if (!shader) return null;
        gl.shaderSource(shader, source);
        gl.compileShader(shader);
        if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
            gl.deleteShader(shader);
            gl.deleteProgram(program);
            return null;
        }
        gl.attachShader(program, shader);
        gl.deleteShader(shader);
    }
    gl.linkProgram(program);
    if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
        gl.deleteProgram(program);
        return null;
    }
    return program;
}

const INTRO_MS = 1700;
const ease = (x: number) => (x < 0.5 ? 2 * x * x : 1 - Math.pow(-2 * x + 2, 2) / 2);
const SEED = 7;
const LINE_SPACING = 5;
const DOT_SIZE = 1.6;
const SWAY = 1;
const SCAN_SPEED = 5;
const GRAIN = 0.5;
const VIGNETTE = 0.35;
const FRAME_INSET = 18;

function ScanlineBloomRender({ className }: BackgroundProps) {
    const rootRef = useRef<HTMLDivElement>(null);
    const canvasRef = useRef<HTMLCanvasElement>(null);

    useEffect(() => {
        const root = rootRef.current;
        const canvas = canvasRef.current;
        if (!root || !canvas) {
            return;
        }
        const gl = canvas.getContext('webgl', { antialias: false, alpha: false });
        if (!gl) {
            return;
        }
        const program = compile(gl, VERT, FRAG);
        if (!program) {
            return;
        }
        const renderRoot = root;
        const renderCanvas = canvas;
        const renderGl = gl;

        const triangleBuffer = renderGl.createBuffer();
        renderGl.bindBuffer(renderGl.ARRAY_BUFFER, triangleBuffer);
        renderGl.bufferData(
            renderGl.ARRAY_BUFFER,
            new Float32Array([-1, -1, 3, -1, -1, 3]),
            renderGl.STATIC_DRAW,
        );
        const position = renderGl.getAttribLocation(program, 'a_position');
        renderGl.enableVertexAttribArray(position);
        renderGl.vertexAttribPointer(position, 2, renderGl.FLOAT, false, 0, 0);
        renderGl.useProgram(program);

        const u = (name: string) => renderGl.getUniformLocation(program, name);
        const loc = {
            tex: u('u_tex'),
            scene: u('u_scene'),
            print: u('u_print'),
            fx: u('u_fx'),
            frame: u('u_frame'),
            post: u('u_post'),
            ink: u('u_ink'),
            hot: u('u_hot'),
            bg: u('u_bg'),
            frameColor: u('u_frameColor'),
        };
        renderGl.uniform1i(loc.tex, 0);

        const makeTexture = () => {
            const tex = renderGl.createTexture();
            renderGl.bindTexture(renderGl.TEXTURE_2D, tex);
            renderGl.texParameteri(renderGl.TEXTURE_2D, renderGl.TEXTURE_WRAP_S, renderGl.CLAMP_TO_EDGE);
            renderGl.texParameteri(renderGl.TEXTURE_2D, renderGl.TEXTURE_WRAP_T, renderGl.CLAMP_TO_EDGE);
            renderGl.texParameteri(renderGl.TEXTURE_2D, renderGl.TEXTURE_MIN_FILTER, renderGl.LINEAR);
            renderGl.texParameteri(renderGl.TEXTURE_2D, renderGl.TEXTURE_MAG_FILTER, renderGl.LINEAR);
            renderGl.texImage2D(
                renderGl.TEXTURE_2D,
                0,
                renderGl.RGBA,
                1,
                1,
                0,
                renderGl.RGBA,
                renderGl.UNSIGNED_BYTE,
                new Uint8Array([0, 0, 0, 255]),
            );
            return tex;
        };
        const tex = makeTexture();
        let aspect = 1;
        renderGl.pixelStorei(renderGl.UNPACK_FLIP_Y_WEBGL, true);

        const painter = document.createElement('canvas');
        let paintedW = 0;
        let paintedH = 0;

        const paint = () => {
            const dpr = window.devicePixelRatio || 1;
            const [w, h] = paintSize(
                renderCanvas.clientWidth || 1,
                renderCanvas.clientHeight || 1,
                dpr,
            );
            paintBouquet(painter, w, h, SEED);
            paintedW = w;
            paintedH = h;
            renderGl.bindTexture(renderGl.TEXTURE_2D, tex);
            renderGl.texImage2D(
                renderGl.TEXTURE_2D,
                0,
                renderGl.RGBA,
                renderGl.RGBA,
                renderGl.UNSIGNED_BYTE,
                painter,
            );
            aspect = w / h;
        };

        let reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        let time = 0;
        let scan = 0;
        let last = 0;
        let raf = 0;
        let introStart = -1;
        let visible = true;

        const resize = () => {
            const dpr = Math.min(window.devicePixelRatio || 1, 2);
            const w = Math.max(1, Math.round(renderCanvas.clientWidth * dpr));
            const h = Math.max(1, Math.round(renderCanvas.clientHeight * dpr));
            if (renderCanvas.width !== w || renderCanvas.height !== h) {
                renderCanvas.width = w;
                renderCanvas.height = h;
            }
        };

        const inkRgb = hexToRgb(INK);
        const hotRgb = hexToRgb(HIGHLIGHT);
        const bgRgb = hexToRgb(BACKGROUND);

        const draw = (now: number) => {
            raf = 0;
            const dt = last ? Math.min((now - last) / 1000, 0.05) : 0.016;
            last = now;
            resize();

            if (!reduceMotion) {
                time += dt;
                scan = (scan + dt * SCAN_SPEED) % (LINE_SPACING * 240);
            }
            if (introStart < 0) introStart = now;
            const intro = reduceMotion ? 1 : Math.min(1, (now - introStart) / INTRO_MS);

            const dpr = renderCanvas.width / Math.max(1, renderCanvas.clientWidth);

            renderGl.viewport(0, 0, renderCanvas.width, renderCanvas.height);
            renderGl.activeTexture(renderGl.TEXTURE0);
            renderGl.bindTexture(renderGl.TEXTURE_2D, tex);
            renderGl.uniform4f(loc.scene, renderCanvas.width, renderCanvas.height, time, dpr);
            renderGl.uniform4f(loc.print, LINE_SPACING, 1, DOT_SIZE, reduceMotion ? 0 : SWAY);
            renderGl.uniform4f(loc.fx, 0, 0, ease(intro), scan);
            renderGl.uniform4f(loc.frame, aspect, 0, FRAME_INSET, SEED % 97);
            renderGl.uniform4f(loc.post, GRAIN, VIGNETTE, 0, 0);
            renderGl.uniform3fv(loc.ink, inkRgb);
            renderGl.uniform3fv(loc.hot, hotRgb);
            renderGl.uniform3fv(loc.bg, bgRgb);
            renderGl.uniform3fv(loc.frameColor, inkRgb);
            renderGl.drawArrays(renderGl.TRIANGLES, 0, 3);

            const settling = intro < 1;
            if ((!reduceMotion || settling) && !document.hidden && visible) {
                raf = requestAnimationFrame(draw);
            }
        };

        const kick = () => {
            if (!raf && !document.hidden && visible) {
                raf = requestAnimationFrame(draw);
            }
        };

        const stop = () => {
            if (raf) {
                cancelAnimationFrame(raf);
                raf = 0;
            }
        };

        paint();
        kick();

        let repaintTimer = 0;
        const maybeRepaint = () => {
            if (!paintedW) return;
            const [w, h] = paintSize(
                renderCanvas.clientWidth || 1,
                renderCanvas.clientHeight || 1,
                window.devicePixelRatio || 1,
            );
            const aspectDrift = Math.abs(w / h - paintedW / paintedH) / (paintedW / paintedH);
            if (aspectDrift < 0.04 && w <= paintedW * 1.25) return;
            window.clearTimeout(repaintTimer);
            repaintTimer = window.setTimeout(() => {
                paint();
                kick();
            }, 160);
        };

        const motionPreference = window.matchMedia('(prefers-reduced-motion: reduce)');
        const handleMotionPreference = () => {
            reduceMotion = motionPreference.matches;
            last = 0;
            kick();
        };
        motionPreference.addEventListener('change', handleMotionPreference);

        const resizeObserver = new ResizeObserver(() => {
            maybeRepaint();
            kick();
        });
        resizeObserver.observe(renderCanvas);

        const visibilityObserver = new IntersectionObserver(([entry]) => {
            visible = Boolean(entry?.isIntersecting);
            if (visible) {
                last = 0;
                kick();
            } else {
                stop();
            }
        });
        visibilityObserver.observe(renderRoot);

        const onVisibility = () => {
            if (document.hidden) {
                stop();
            } else {
                last = 0;
                kick();
            }
        };
        document.addEventListener('visibilitychange', onVisibility);

        return () => {
            stop();
            window.clearTimeout(repaintTimer);
            resizeObserver.disconnect();
            visibilityObserver.disconnect();
            motionPreference.removeEventListener('change', handleMotionPreference);
            document.removeEventListener('visibilitychange', onVisibility);
            renderGl.deleteTexture(tex);
            renderGl.deleteBuffer(triangleBuffer);
            renderGl.deleteProgram(program);
        };
    }, []);

    return (
        <div
            ref={rootRef}
            aria-hidden="true"
            className={cn('relative overflow-hidden', className)}
            style={{ backgroundColor: BACKGROUND }}
        >
            <canvas
                ref={canvasRef}
                aria-hidden="true"
                className="pointer-events-none absolute inset-0 block h-full w-full"
            />
        </div>
    );
}

function ScanlineBloomDefault({ className }: BackgroundProps) {
    return <ScanlineBloomRender className={className} />;
}

export const scanlineBloomManifest: BackgroundManifest = {
    slug: Backgrounds.ScanlineBloom,
    name: 'Scanline bloom',
    description: 'Buquê de lírios e rosas impresso em scanlines azuis.',
    Component: ScanlineBloomDefault,
};
