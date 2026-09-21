'use client';

import { motion } from 'motion/react';

import { cn } from '@/lib/utils';

interface XpeakWordmarkProps {
    emph?: string;
    rest?: string;
    emphSize?: number;
    restSize?: number;
    className?: string;
}

const FONT_FAMILY = '"Orbitron", "Arial Black", sans-serif';
const STROKE_STYLE = {
    color: 'transparent',
    WebkitTextStroke: '1px #ffffff',
} as const;

const CHAR_INITIAL = { opacity: 0, filter: 'blur(18px)', y: 12 };
const CHAR_ANIMATE = { opacity: 1, filter: 'blur(0px)', y: 0 };
const CHAR_DURATION = 0.9;
const CHAR_STAGGER = 0.08;
const REST_START_DELAY = 0.2;

export function XpeakWordmark({
    emph = 'X',
    rest = 'PEAK',
    emphSize = 96,
    restSize = 32,
    className,
}: XpeakWordmarkProps) {
    const restChars = rest.split('');
    return (
        <span
            aria-hidden="true"
            className={cn(
                'relative inline-flex items-center font-bold leading-none tracking-tight',
                className,
            )}
            style={{ fontFamily: FONT_FAMILY }}
        >
            <motion.span
                initial={CHAR_INITIAL}
                animate={CHAR_ANIMATE}
                transition={{ duration: CHAR_DURATION, ease: 'easeOut' }}
                style={{ ...STROKE_STYLE, fontSize: `${emphSize}px` }}
            >
                {emph}
            </motion.span>
            <span
                className="inline-flex"
                style={{ marginLeft: `-${emphSize * 0.35}px` }}
            >
                {restChars.map((char, index) => (
                    <motion.span
                        key={`${char}-${index}`}
                        initial={CHAR_INITIAL}
                        animate={CHAR_ANIMATE}
                        transition={{
                            duration: CHAR_DURATION,
                            ease: 'easeOut',
                            delay: REST_START_DELAY + index * CHAR_STAGGER,
                        }}
                        style={{ ...STROKE_STYLE, fontSize: `${restSize}px` }}
                    >
                        {char}
                    </motion.span>
                ))}
            </span>
        </span>
    );
}
