'use client';

import { Home, Plus, Trophy, User, Users } from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import { motion } from 'motion/react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';

type DockVariant = 'default' | 'primary';

interface DockItem {
    id: string;
    name: string;
    href: string;
    Icon: LucideIcon;
    variant: DockVariant;
}

const DOCK_ITEMS: DockItem[] = [
    { id: 'home', name: 'Home', href: '/home', Icon: Home, variant: 'default' },
    {
        id: 'groups',
        name: 'Groups',
        href: '/groups',
        Icon: Users,
        variant: 'default',
    },
    {
        id: 'checkin',
        name: 'Check-in',
        href: '/checkin',
        Icon: Plus,
        variant: 'primary',
    },
    {
        id: 'leaderboard',
        name: 'Leaderboard',
        href: '/leaderboard',
        Icon: Trophy,
        variant: 'default',
    },
    {
        id: 'profile',
        name: 'Profile',
        href: '/profile',
        Icon: User,
        variant: 'default',
    },
];

const stripTrailingSlash = (path: string) => path.replace(/\/+$/, '') || '/';

interface DockIconProps {
    item: DockItem;
    active: boolean;
}

function DockIcon({ item, active }: DockIconProps) {
    const { Icon, variant } = item;

    if (variant === 'primary') {
        return (
            <Link
                href={item.href}
                aria-label={item.name}
                className="relative flex h-14 w-14 items-center justify-center"
            >
                <motion.span
                    className="relative flex h-16 w-16 -translate-y-6 items-center justify-center overflow-hidden rounded-full border-2 border-sky-300/70 bg-sky-500 text-zinc-950 shadow-[0_12px_28px_-6px_rgba(56,189,248,0.7)]"
                    whileTap={{ scale: 0.92 }}
                    transition={{ type: 'spring', stiffness: 400, damping: 20 }}
                >
                    <Icon className="h-7 w-7" strokeWidth={2.75} />
                    <span
                        aria-hidden="true"
                        className="pointer-events-none absolute inset-0 rounded-full bg-gradient-to-br from-white/40 to-transparent"
                    />
                </motion.span>
            </Link>
        );
    }

    return (
        <Link
            href={item.href}
            aria-label={item.name}
            aria-current={active ? 'page' : undefined}
            className="relative flex h-14 w-14 items-center justify-center"
        >
            <motion.span
                className="relative flex h-full w-full items-center justify-center overflow-hidden rounded-2xl border border-sky-400/20 bg-zinc-900/70 text-sky-300 shadow-lg backdrop-blur"
                whileTap={{ scale: 0.92 }}
                transition={{ type: 'spring', stiffness: 400, damping: 20 }}
            >
                <Icon className="h-5 w-5" strokeWidth={2.25} />
                <span
                    aria-hidden="true"
                    className="pointer-events-none absolute inset-0 rounded-2xl bg-gradient-to-br from-sky-300/20 to-transparent"
                />
            </motion.span>
        </Link>
    );
}

export function BottomDock() {
    const pathname = usePathname();
    const current = stripTrailingSlash(pathname ?? '');

    return (
        <motion.nav
            aria-label="Primary"
            className="fixed bottom-16 left-1/2 z-20 flex -translate-x-1/2 items-center gap-4 rounded-3xl border border-sky-400/20 bg-zinc-950/50 px-4 py-3 shadow-2xl backdrop-blur-md"
            initial={{ y: 100, opacity: 0 }}
            animate={{ y: 0, opacity: 1 }}
            transition={{ type: 'spring', stiffness: 260, damping: 20, delay: 0.1 }}
        >
            {DOCK_ITEMS.map((item) => (
                <DockIcon
                    key={item.id}
                    item={item}
                    active={current === stripTrailingSlash(item.href)}
                />
            ))}
        </motion.nav>
    );
}
