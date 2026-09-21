'use client';

import { motion } from 'motion/react';
import Link from 'next/link';
import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

interface AnimatedBorderButtonProps {
    href: string;
    children: ReactNode;
    className?: string;
    ariaLabel?: string;
}

export function AnimatedBorderButton({
    href,
    children,
    className,
    ariaLabel,
}: AnimatedBorderButtonProps) {
    return (
        <Link
            href={href}
            aria-label={ariaLabel}
            className={cn(
                'group relative inline-flex h-12 items-center justify-center overflow-hidden rounded-md border border-white/15 bg-white/5 px-10 text-sm font-semibold uppercase tracking-[0.25em] text-white backdrop-blur-md transition-colors hover:bg-white/10',
                className,
            )}
        >
            <span
                aria-hidden="true"
                className={cn(
                    'pointer-events-none absolute -inset-px rounded-[inherit] border-2 border-transparent',
                    '[mask-clip:padding-box,border-box]',
                    '[mask-composite:intersect] [mask-image:linear-gradient(transparent,transparent),linear-gradient(#000,#000)]',
                )}
            >
                <motion.span
                    className="absolute aspect-square bg-gradient-to-r from-transparent via-emerald-400 to-emerald-300"
                    animate={{ offsetDistance: ['0%', '100%'] }}
                    style={{
                        width: 24,
                        offsetPath: 'rect(0 auto auto 0 round 24px)',
                    }}
                    transition={{
                        repeat: Number.POSITIVE_INFINITY,
                        duration: 4,
                        ease: 'linear',
                    }}
                />
            </span>
            <span className="relative">{children}</span>
        </Link>
    );
}
