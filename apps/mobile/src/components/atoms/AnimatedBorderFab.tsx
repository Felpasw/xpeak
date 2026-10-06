'use client';

import { motion } from 'motion/react';
import type { ButtonHTMLAttributes, ReactNode } from 'react';

import { cn } from '@/lib/utils';

interface AnimatedBorderFabProps extends ButtonHTMLAttributes<HTMLButtonElement> {
    icon: ReactNode;
    label: string;
}

export function AnimatedBorderFab({
    icon,
    label,
    className,
    type = 'button',
    ...rest
}: AnimatedBorderFabProps) {
    return (
        <button
            type={type}
            aria-label={label}
            className={cn(
                'group relative inline-flex h-16 w-16 items-center justify-center overflow-hidden rounded-full border border-white/15 bg-white/5 text-white backdrop-blur-md transition-colors hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-60',
                className,
            )}
            {...rest}
        >
            <span
                aria-hidden="true"
                className={cn(
                    'pointer-events-none absolute -inset-px rounded-full border-2 border-transparent',
                    '[mask-clip:padding-box,border-box]',
                    '[mask-composite:intersect] [mask-image:linear-gradient(transparent,transparent),linear-gradient(#000,#000)]',
                )}
            >
                <motion.span
                    className="absolute aspect-square bg-gradient-to-r from-transparent via-sky-400 to-sky-300"
                    animate={{ offsetDistance: ['0%', '100%'] }}
                    style={{
                        width: 24,
                        offsetPath: 'circle(50% at 50% 50%)',
                    }}
                    transition={{
                        repeat: Number.POSITIVE_INFINITY,
                        duration: 4,
                        ease: 'linear',
                    }}
                />
            </span>
            <span className="relative flex items-center justify-center">{icon}</span>
        </button>
    );
}
