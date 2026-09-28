'use client';

import { motion, type Variants } from 'motion/react';
import { useState, type Ref, type TextareaHTMLAttributes } from 'react';

import { cn } from '@/lib/utils';

interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
    label: string;
    value: string;
    counter?: boolean;
    ref?: Ref<HTMLTextAreaElement>;
}

const REMAINING_SUFFIX = 'restantes';
const LOW_REMAINING_THRESHOLD = 20;

const LABEL_REST_COLOR = 'var(--color-zinc-100)';
const LABEL_FLOAT_COLOR = 'var(--color-zinc-500)';

const containerVariants: Variants = {
    initial: {},
    animate: {
        transition: {
            staggerChildren: 0.05,
        },
    },
};

const letterVariants: Variants = {
    initial: {
        y: 0,
        color: LABEL_REST_COLOR,
    },
    animate: {
        y: '-120%',
        color: LABEL_FLOAT_COLOR,
        transition: {
            type: 'spring',
            stiffness: 300,
            damping: 20,
        },
    },
};

function CharacterCounter({ used, max }: { used: number; max: number }) {
    const remaining = Math.max(0, max - used);
    const isLow = remaining <= LOW_REMAINING_THRESHOLD;

    return (
        <span
            aria-live="polite"
            className={cn('text-xs tabular-nums', isLow ? 'text-amber-400' : 'text-zinc-500')}
        >
            {used}/{max} · {remaining} {REMAINING_SUFFIX}
        </span>
    );
}

export function Textarea({
    label,
    className,
    value,
    counter = false,
    maxLength,
    ref,
    onFocus,
    onBlur,
    ...rest
}: TextareaProps) {
    const [isFocused, setIsFocused] = useState(false);
    const showFloating = isFocused || value.length > 0;
    const animationState = showFloating ? 'animate' : 'initial';

    return (
        <div className={cn('flex flex-col gap-2', className)}>
            {counter && maxLength ? (
                <div className="flex items-center justify-end text-sm">
                    <CharacterCounter used={value.length} max={maxLength} />
                </div>
            ) : null}

            <div className="relative">
                <motion.div
                    aria-hidden="true"
                    className="absolute top-2 left-0 pointer-events-none text-zinc-100"
                    variants={containerVariants}
                    initial="initial"
                    animate={animationState}
                >
                    {label.split('').map((char, index) => (
                        <motion.span
                            key={`${char}-${index}`}
                            className="inline-block text-sm"
                            variants={letterVariants}
                            style={{ willChange: 'transform' }}
                        >
                            {char === ' ' ? ' ' : char}
                        </motion.span>
                    ))}
                </motion.div>

                <textarea
                    {...rest}
                    ref={ref}
                    value={value}
                    maxLength={maxLength}
                    onFocus={(event) => {
                        setIsFocused(true);
                        onFocus?.(event);
                    }}
                    onBlur={(event) => {
                        setIsFocused(false);
                        onBlur?.(event);
                    }}
                    aria-label={rest['aria-label'] ?? label}
                    className="w-full resize-none border-b-2 border-sky-500 bg-transparent py-2 text-base font-medium text-zinc-100 caret-sky-400 outline-none placeholder-transparent transition-colors focus:border-sky-400"
                />
            </div>
        </div>
    );
}
