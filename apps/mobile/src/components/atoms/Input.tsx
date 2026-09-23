'use client';

import { Eye, EyeOff } from 'lucide-react';
import { motion, type Variants } from 'motion/react';
import { useState, type InputHTMLAttributes } from 'react';

import { cn } from '@/lib/utils';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
    label: string;
    value: string;
    showPasswordToggle?: boolean;
}

const TOGGLE_SHOW = 'Show password';
const TOGGLE_HIDE = 'Hide password';

const containerVariants: Variants = {
    initial: {},
    animate: {
        transition: {
            staggerChildren: 0.05,
        },
    },
};

const LABEL_REST_COLOR = 'var(--color-zinc-100)';
const LABEL_FLOAT_COLOR = 'var(--color-zinc-500)';

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

export function Input({
    label,
    className,
    value,
    type = 'text',
    onFocus,
    onBlur,
    showPasswordToggle = false,
    ...rest
}: InputProps) {
    const [isFocused, setIsFocused] = useState(false);
    const [isRevealed, setIsRevealed] = useState(false);
    const showLabel = isFocused || value.length > 0;
    const animationState = showLabel ? 'animate' : 'initial';

    const isPassword = type === 'password';
    const toggleEnabled = showPasswordToggle && isPassword;
    const effectiveType = toggleEnabled && isRevealed ? 'text' : type;
    const toggleLabel = isRevealed ? TOGGLE_HIDE : TOGGLE_SHOW;
    const ToggleIcon = isRevealed ? EyeOff : Eye;

    return (
        <div className={cn('relative', className)}>
            <motion.div
                aria-hidden="true"
                className="absolute top-1/2 -translate-y-1/2 pointer-events-none text-zinc-100"
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
                        {char === ' ' ? ' ' : char}
                    </motion.span>
                ))}
            </motion.div>

            <input
                {...rest}
                aria-label={rest['aria-label'] ?? label}
                value={value}
                type={effectiveType}
                onFocus={(event) => {
                    setIsFocused(true);
                    onFocus?.(event);
                }}
                onBlur={(event) => {
                    setIsFocused(false);
                    onBlur?.(event);
                }}
                className={cn(
                    'w-full border-b-2 border-sky-500 bg-transparent py-2 text-base font-medium text-zinc-100 caret-sky-400 outline-none placeholder-transparent transition-colors focus:border-sky-400',
                    toggleEnabled && 'pr-8',
                )}
            />

            {toggleEnabled ? (
                <button
                    type="button"
                    aria-label={toggleLabel}
                    onClick={() => setIsRevealed((prev) => !prev)}
                    tabIndex={-1}
                    className="absolute bottom-2 right-0 flex h-6 w-6 items-center justify-center text-zinc-500 transition-colors hover:text-sky-400 focus-visible:text-sky-400 focus-visible:outline-none"
                >
                    <ToggleIcon className="h-4 w-4" aria-hidden="true" />
                </button>
            ) : null}
        </div>
    );
}
