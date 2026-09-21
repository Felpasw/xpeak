'use client';

import Link from 'next/link';
import type { MouseEvent } from 'react';
import { useEffect, useRef } from 'react';

interface TextOptions {
    emph?: string;
    rest?: string;
    emphSize?: number;
    restSize?: number;
    color?: string;
    delay?: number;
    canvasWidth: number;
    canvasHeight: number;
}

interface ThunderOptions {
    x?: number;
    y?: number;
    lifespan?: number;
    color?: string;
    glow?: string;
    width?: number;
    direct?: number;
    max?: number;
}

interface SparkVelocity {
    direct: number;
    weight: number;
    friction: number;
}

interface SparkAcceleration {
    change: number;
    min: number;
    max: number;
}

interface SparkGravity {
    direct: number;
    weight: number;
}

interface SparkOptions {
    x?: number;
    y?: number;
    v?: SparkVelocity;
    a?: SparkAcceleration;
    g?: SparkGravity;
    width?: number;
    lifespan?: number;
    color?: string;
}

interface TextBounds {
    width: number;
    height: number;
}

class TextGlyph {
    private color: string;
    private delay: number;
    private basedelay: number;
    private bound: TextBounds;
    private x: number;
    private y: number;
    private data: ImageData;
    private index = 0;

    constructor(options: TextOptions) {
        const pool = document.createElement('canvas');
        const buffer = pool.getContext('2d');
        if (!buffer) throw new Error('2d context unavailable');

        pool.width = options.canvasWidth;
        pool.height = options.canvasHeight;

        const emph = options.emph ?? 'X';
        const rest = options.rest ?? 'PEAK';
        const emphSize = options.emphSize ?? 260;
        const restSize = options.restSize ?? 90;
        this.color = options.color ?? '#ffffff';
        this.delay = options.delay ?? 2;
        this.basedelay = this.delay;

        buffer.textBaseline = 'middle';
        buffer.strokeStyle = this.color;
        const padding = 8;

        buffer.font = `bold ${emphSize}px "Orbitron", "Arial Black", sans-serif`;
        const emphWidth = buffer.measureText(emph).width;

        buffer.font = `bold ${restSize}px "Orbitron", "Arial Black", sans-serif`;
        const restWidth = buffer.measureText(rest).width;

        const totalWidth = Math.ceil(emphWidth * 0.5 + restWidth + padding * 2);
        const totalHeight = Math.ceil(emphSize * 1.5);
        this.bound = { width: totalWidth, height: totalHeight };
        const centerY = totalHeight / 2;

        buffer.font = `bold ${emphSize}px "Orbitron", "Arial Black", sans-serif`;
        buffer.strokeText(emph, padding, centerY);

        buffer.font = `bold ${restSize}px "Orbitron", "Arial Black", sans-serif`;
        buffer.strokeText(rest, padding + emphWidth * 0.5, centerY);

        this.x = options.canvasWidth * 0.5 - totalWidth * 0.5;
        this.y = options.canvasHeight * 0.5 - totalHeight * 0.5;

        this.data = buffer.getImageData(0, 0, totalWidth, totalHeight);
    }

    update(thunder: Thunder[], particles: Particles[]) {
        if (this.index >= this.bound.width) {
            return;
        }

        const data = this.data.data;
        for (let i = this.index * 4; i < data.length; i += 4 * this.data.width) {
            const bitmap = data[i] + data[i + 1] + data[i + 2] + data[i + 3];
            if (bitmap > 255 && Math.random() > 0.94) {
                const x = this.x + this.index;
                const y = this.y + i / this.bound.width / 4;
                thunder.push(new Thunder({ x, y }));

                if (Math.random() > 0.3) {
                    particles.push(new Particles({ x, y }));
                }
            }
        }

        if (this.delay-- < 0) {
            this.index += 2;
            this.delay += this.basedelay;
        }
    }

    render(ctx: CanvasRenderingContext2D) {
        ctx.putImageData(
            this.data,
            this.x,
            this.y,
            0,
            0,
            this.index,
            this.bound.height,
        );
    }
}

class Thunder {
    private lifespan: number;
    private maxlife: number;
    private color: string;
    private glow: string;
    private x: number;
    private y: number;
    private width: number;
    private segments: { direct: number; length: number; change: number }[];

    constructor(options: ThunderOptions = {}) {
        this.lifespan = options.lifespan ?? Math.round(Math.random() * 10 + 10);
        this.maxlife = this.lifespan;
        this.color = options.color ?? '#fefefe';
        this.glow = options.glow ?? '#2323fe';
        this.x = options.x ?? Math.random() * window.innerWidth;
        this.y = options.y ?? Math.random() * window.innerHeight;
        this.width = options.width ?? 2;
        const direct = options.direct ?? Math.random() * Math.PI * 2;
        const max = options.max ?? Math.round(Math.random() * 10 + 20);
        this.segments = Array.from({ length: max }, () => ({
            direct: direct + (Math.PI * Math.random() * 0.2 - 0.1),
            length: Math.random() * 20 + 80,
            change: Math.random() * 0.04 - 0.02,
        }));
    }

    update(index: number, array: Thunder[]) {
        this.segments.forEach((s) => {
            s.direct += s.change;
            if (Math.random() > 0.96) s.change *= -1;
        });
        if (this.lifespan > 0) {
            this.lifespan--;
        } else {
            array.splice(index, 1);
        }
    }

    render(ctx: CanvasRenderingContext2D) {
        if (this.lifespan <= 0) return;

        ctx.beginPath();
        ctx.globalAlpha = this.lifespan / this.maxlife;
        ctx.strokeStyle = this.color;
        ctx.lineWidth = this.width;
        ctx.shadowBlur = 32;
        ctx.shadowColor = this.glow;
        ctx.moveTo(this.x, this.y);

        let prev = { x: this.x, y: this.y };
        this.segments.forEach((s) => {
            const x = prev.x + Math.cos(s.direct) * s.length;
            const y = prev.y + Math.sin(s.direct) * s.length;
            prev = { x, y };
            ctx.lineTo(x, y);
        });

        ctx.stroke();
        ctx.closePath();
        ctx.shadowBlur = 0;

        const strength = Math.random() * 80 + 40;
        const light = ctx.createRadialGradient(
            this.x,
            this.y,
            0,
            this.x,
            this.y,
            strength,
        );
        light.addColorStop(0, 'rgba(250, 200, 50, 0.6)');
        light.addColorStop(0.1, 'rgba(250, 200, 50, 0.2)');
        light.addColorStop(0.4, 'rgba(250, 200, 50, 0.06)');
        light.addColorStop(0.65, 'rgba(250, 200, 50, 0.01)');
        light.addColorStop(0.8, 'rgba(250, 200, 50, 0)');

        ctx.beginPath();
        ctx.fillStyle = light;
        ctx.arc(this.x, this.y, strength, 0, Math.PI * 2);
        ctx.fill();
        ctx.closePath();
    }
}

class Spark {
    private x: number;
    private y: number;
    private v: SparkVelocity;
    private a: SparkAcceleration;
    private g: SparkGravity;
    private width: number;
    private lifespan: number;
    private maxlife: number;
    private color: string;
    private prev: { x: number; y: number };

    constructor(options: SparkOptions = {}) {
        this.x = options.x ?? window.innerWidth * 0.5;
        this.y = options.y ?? window.innerHeight * 0.5;
        this.v = options.v ?? {
            direct: Math.random() * Math.PI * 2,
            weight: Math.random() * 14 + 2,
            friction: 0.88,
        };
        this.a = options.a ?? {
            change: Math.random() * 0.4 - 0.2,
            min: this.v.direct - Math.PI * 0.4,
            max: this.v.direct + Math.PI * 0.4,
        };
        this.g = options.g ?? {
            direct: Math.PI * 0.5 + (Math.random() * 0.4 - 0.2),
            weight: Math.random() * 0.25 + 0.25,
        };
        this.width = options.width ?? Math.random() * 3;
        this.lifespan = options.lifespan ?? Math.round(Math.random() * 20 + 40);
        this.maxlife = this.lifespan;
        this.color = options.color ?? '#feca32';
        this.prev = { x: this.x, y: this.y };
    }

    update(index: number, array: Spark[]) {
        this.prev = { x: this.x, y: this.y };
        this.x += Math.cos(this.v.direct) * this.v.weight;
        this.x += Math.cos(this.g.direct) * this.g.weight;
        this.y += Math.sin(this.v.direct) * this.v.weight;
        this.y += Math.sin(this.g.direct) * this.g.weight;

        if (this.v.weight > 0.2) {
            this.v.weight *= this.v.friction;
        }

        this.v.direct += this.a.change;
        if (this.v.direct > this.a.max || this.v.direct < this.a.min) {
            this.a.change *= -1;
        }

        if (this.lifespan > 0) {
            this.lifespan--;
        } else {
            array.splice(index, 1);
        }
    }

    render(ctx: CanvasRenderingContext2D) {
        if (this.lifespan <= 0) return;

        ctx.beginPath();
        ctx.globalAlpha = this.lifespan / this.maxlife;
        ctx.strokeStyle = this.color;
        ctx.lineWidth = this.width;
        ctx.moveTo(this.x, this.y);
        ctx.lineTo(this.prev.x, this.prev.y);
        ctx.stroke();
        ctx.closePath();
    }
}

class Particles {
    private sparks: Spark[];

    constructor(options: SparkOptions & { max?: number } = {}) {
        const max = options.max ?? Math.round(Math.random() * 10 + 10);
        this.sparks = Array.from({ length: max }, () => new Spark(options));
    }

    update() {
        this.sparks.forEach((s, i) => s.update(i, this.sparks));
    }

    render(ctx: CanvasRenderingContext2D) {
        this.sparks.forEach((s) => s.render(ctx));
    }
}

interface LightningTextProps {
    emph?: string;
    rest?: string;
    emphSize?: number;
    restSize?: number;
    width?: number;
    height?: number;
    subtitle?: string;
    className?: string;
    href?: string;
    ariaLabel?: string;
}

export function LightningText({
    emph = 'X',
    rest = 'PEAK',
    emphSize,
    restSize,
    width,
    height,
    subtitle,
    className,
    href,
    ariaLabel,
}: LightningTextProps) {
    const canvasRef = useRef<HTMLCanvasElement | null>(null);
    const animationRef = useRef<number | null>(null);
    const thunderRef = useRef<Thunder[]>([]);
    const particlesRef = useRef<Particles[]>([]);
    const textRef = useRef<TextGlyph | null>(null);

    useEffect(() => {
        const canvas = canvasRef.current;
        if (!canvas) return;
        const ctx = canvas.getContext('2d');
        if (!ctx) return;

        let cancelled = false;

        const loop = () => {
            const glyph = textRef.current;
            if (!glyph) return;

            glyph.update(thunderRef.current, particlesRef.current);
            thunderRef.current.forEach((l, i) => l.update(i, thunderRef.current));
            particlesRef.current.forEach((p) => p.update());

            ctx.globalCompositeOperation = 'source-over';
            ctx.globalAlpha = 1;
            ctx.clearRect(0, 0, canvas.width, canvas.height);

            ctx.globalCompositeOperation = 'screen';
            glyph.render(ctx);
            thunderRef.current.forEach((l) => l.render(ctx));
            particlesRef.current.forEach((p) => p.render(ctx));

            animationRef.current = requestAnimationFrame(loop);
        };

        const resolveSize = () => ({
            w: width ?? window.innerWidth,
            h: height ?? window.innerHeight,
        });

        const init = (w: number, h: number) => {
            canvas.width = w;
            canvas.height = h;
            textRef.current = new TextGlyph({
                emph,
                rest,
                emphSize,
                restSize,
                canvasWidth: w,
                canvasHeight: h,
            });
        };

        const start = async () => {
            if (typeof document !== 'undefined' && document.fonts?.load) {
                await Promise.race([
                    Promise.all([
                        document.fonts.load(`bold ${emphSize ?? 260}px "Orbitron"`),
                        document.fonts.load(`bold ${restSize ?? 90}px "Orbitron"`),
                    ]),
                    new Promise((resolve) => setTimeout(resolve, 3000)),
                ]);
            }
            if (cancelled) return;
            const { w, h } = resolveSize();
            init(w, h);
            loop();
        };

        start();

        const isFixedSize = width !== undefined && height !== undefined;
        const handleResize = () => {
            const { w, h } = resolveSize();
            init(w, h);
        };

        if (!isFixedSize) {
            window.addEventListener('resize', handleResize);
        }

        return () => {
            cancelled = true;
            if (animationRef.current) cancelAnimationFrame(animationRef.current);
            if (!isFixedSize) {
                window.removeEventListener('resize', handleResize);
            }
        };
    }, [emph, rest, emphSize, restSize, width, height]);

    const handleCanvasClick = (e: MouseEvent<HTMLCanvasElement>) => {
        const rect = e.currentTarget.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;
        thunderRef.current.push(new Thunder({ x, y }));
        particlesRef.current.push(new Particles({ x, y }));
    };

    const defaultWrapperClass =
        width !== undefined && height !== undefined
            ? 'relative overflow-hidden'
            : 'relative w-full h-screen overflow-hidden';
    const wrapperClass = className ?? defaultWrapperClass;
    const wrapperStyle =
        width !== undefined && height !== undefined
            ? { width, height }
            : undefined;
    const subtitleNode = subtitle ? (
        <p className="pointer-events-none absolute left-1/2 top-[calc(50%+180px)] z-10 -translate-x-1/2 whitespace-nowrap px-4 text-center font-mono text-[11px] uppercase tracking-[0.35em] text-white/50">
            {subtitle}
        </p>
    ) : null;

    if (href) {
        return (
            <Link
                href={href}
                aria-label={ariaLabel ?? `${emph}${rest}`}
                className={wrapperClass}
                style={wrapperStyle}
            >
                <canvas
                    ref={canvasRef}
                    className="block w-full h-full cursor-pointer"
                />
                {subtitleNode}
            </Link>
        );
    }

    return (
        <div className={wrapperClass} style={wrapperStyle}>
            <canvas
                ref={canvasRef}
                onClick={handleCanvasClick}
                className="block w-full h-full cursor-crosshair"
            />
            {subtitleNode}
        </div>
    );
}
