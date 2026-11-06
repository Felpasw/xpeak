'use client';

import { Flag, Home, Plus, Trophy, User } from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useState } from 'react';

import { cn } from '@/lib/utils';

interface DockItem {
    id: string;
    name: string;
    href: string;
    Icon: LucideIcon;
}

const DOCK_ITEMS: DockItem[] = [
    { id: 'home', name: 'Home', href: '/home', Icon: Home },
    { id: 'challenges', name: 'Challenges', href: '/challenges', Icon: Flag },
    { id: 'checkin', name: 'Check-in', href: '/checkin', Icon: Plus },
    { id: 'leaderboard', name: 'Leaderboard', href: '/leaderboard', Icon: Trophy },
    { id: 'profile', name: 'Profile', href: '/profile', Icon: User },
];

const stripTrailingSlash = (path: string) => path.replace(/\/+$/, '') || '/';

const VIEW_W = 24;
const VIEW_H = 80;

function seededRandom(seed: number) {
    let s = (seed * 2654435761) >>> 0;
    return () => {
        s = (s * 1664525 + 1013904223) >>> 0;
        return s / 4294967296;
    };
}

function buildBoltPath(seed: number, segments: number, spread: number) {
    const rnd = seededRandom(seed);
    const cx = VIEW_W / 2;
    const step = VIEW_H / segments;
    let path = `M${cx.toFixed(2)} ${VIEW_H.toFixed(2)}`;
    for (let i = 1; i <= segments; i++) {
        const progress = i / segments;
        const y = VIEW_H - i * step + (rnd() - 0.5) * step * 0.45;
        // Horizontal wobble tapers toward the top so the tip reads as a point.
        const taper = 1 - progress * 0.55;
        const x = cx + (rnd() - 0.5) * VIEW_W * spread * taper;
        path += ` L${x.toFixed(2)} ${y.toFixed(2)}`;
    }
    return path;
}

function buildForkPath(seed: number) {
    const rnd = seededRandom(seed);
    const originY = 20 + rnd() * 40;
    const originX = VIEW_W / 2 + (rnd() - 0.5) * 6;
    const side = rnd() > 0.5 ? 1 : -1;
    const steps = 3 + Math.floor(rnd() * 2);
    let path = `M${originX.toFixed(2)} ${originY.toFixed(2)}`;
    let px = originX;
    let py = originY;
    for (let i = 0; i < steps; i++) {
        px += side * (2 + rnd() * 4);
        py -= 3 + rnd() * 5;
        path += ` L${px.toFixed(2)} ${py.toFixed(2)}`;
    }
    return path;
}

function LightningStrike({ seed }: { seed: number }) {
    const mainPath = buildBoltPath(seed, 8 + (seed % 3), 0.85);
    const ghostPath = buildBoltPath(seed ^ 0x5a5a, 7 + ((seed >> 1) % 3), 0.95);
    const forkPath = buildForkPath(seed ^ 0xdeadbeef);
    const tilt = ((seededRandom(seed ^ 0xa17e)() - 0.5) * 10).toFixed(2);

    return (
        <>
            <svg
                aria-hidden="true"
                viewBox={`0 0 ${VIEW_W} ${VIEW_H}`}
                preserveAspectRatio="none"
                className="dock-strike pointer-events-none absolute bottom-0 left-1/2 h-24 w-8"
                style={{
                    filter:
                        'drop-shadow(0 0 3px rgba(56,189,248,0.95)) drop-shadow(0 0 9px rgba(56,189,248,0.7))',
                    transform: `translate(-50%, 0) rotate(${tilt}deg)`,
                    transformOrigin: 'bottom center',
                }}
            >
                <path
                    d={ghostPath}
                    fill="none"
                    stroke="#38bdf8"
                    strokeOpacity="0.5"
                    strokeWidth="2.6"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                />
                <path
                    d={mainPath}
                    fill="none"
                    stroke="#bae6fd"
                    strokeWidth="1.5"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                />
                <path
                    d={forkPath}
                    fill="none"
                    stroke="#e0f2fe"
                    strokeWidth="0.9"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    opacity="0.85"
                />
            </svg>
            <span
                aria-hidden="true"
                className="dock-strike-flash pointer-events-none absolute -bottom-4 left-1/2 h-28 w-14 -translate-x-1/2 rounded-full bg-sky-300/60 blur-xl"
            />
        </>
    );
}

interface DockIconProps {
    item: DockItem;
    active: boolean;
}

function DockIcon({ item, active }: DockIconProps) {
    const { Icon } = item;
    const [strike, setStrike] = useState(0);
    return (
        <Link
            href={item.href}
            aria-label={item.name}
            aria-current={active ? 'page' : undefined}
            data-active={active}
            onClick={() =>
                setStrike(() => (Date.now() ^ Math.floor(Math.random() * 0xffff)) >>> 0)
            }
            className="group relative flex h-12 flex-1 items-center justify-center"
        >
            {strike > 0 && <LightningStrike key={strike} seed={strike} />}
            <Icon
                className={cn(
                    'relative h-6 w-6 -translate-y-0.5 transition-colors duration-300',
                    active
                        ? 'text-white'
                        : 'text-zinc-400 group-hover:text-zinc-200',
                )}
                strokeWidth={active ? 2.5 : 2}
            />
        </Link>
    );
}

export function BottomDock() {
    const pathname = usePathname();
    const current = stripTrailingSlash(pathname ?? '');
    const activeIndex = Math.max(
        0,
        DOCK_ITEMS.findIndex((item) => stripTrailingSlash(item.href) === current),
    );
    const slotRatio = (activeIndex + 0.5) / DOCK_ITEMS.length;

    return (
        <nav
            aria-label="Primary"
            className="fixed bottom-14 left-4 right-4 z-20 flex items-end rounded-2xl border border-white/10 bg-zinc-950/85 px-2 py-3 shadow-2xl backdrop-blur-md"
        >
            <div
                aria-hidden="true"
                className="pointer-events-none absolute bottom-0 h-full transition-all duration-500 ease-[cubic-bezier(0.2,0.8,0.2,1)]"
                style={{ left: `calc(0.5rem + (100% - 1rem) * ${slotRatio})` }}
            >
                <span className="absolute bottom-0 left-1/2 h-24 w-16 -translate-x-1/2 rounded-full bg-gradient-to-t from-sky-400/70 via-sky-400/25 to-transparent blur-xl" />
                <span
                    className="absolute bottom-0 left-1/2 h-14 w-[1.5px] rounded-full bg-gradient-to-t from-sky-400/80 via-sky-400/40 to-transparent blur-[1px]"
                    style={{
                        transform: 'translateX(-50%) rotate(-24deg)',
                        transformOrigin: 'bottom center',
                    }}
                />
                <span
                    className="absolute bottom-0 left-1/2 h-14 w-[1.5px] rounded-full bg-gradient-to-t from-sky-400/80 via-sky-400/40 to-transparent blur-[1px]"
                    style={{
                        transform: 'translateX(-50%) rotate(24deg)',
                        transformOrigin: 'bottom center',
                    }}
                />
                <span
                    className="absolute bottom-0 left-1/2 h-10 w-[1px] rounded-full bg-gradient-to-t from-sky-200/80 to-transparent"
                    style={{
                        transform: 'translateX(-50%) rotate(-42deg)',
                        transformOrigin: 'bottom center',
                    }}
                />
                <span
                    className="absolute bottom-0 left-1/2 h-10 w-[1px] rounded-full bg-gradient-to-t from-sky-200/80 to-transparent"
                    style={{
                        transform: 'translateX(-50%) rotate(42deg)',
                        transformOrigin: 'bottom center',
                    }}
                />
                <span className="absolute bottom-0 left-1/2 h-[2px] w-10 -translate-x-1/2 rounded-full bg-sky-400 shadow-[0_0_10px_rgba(56,189,248,0.95)]" />
            </div>

            {DOCK_ITEMS.map((item, index) => (
                <DockIcon
                    key={item.id}
                    item={item}
                    active={index === activeIndex}
                />
            ))}
        </nav>
    );
}
