'use client';

import type { TextareaHTMLAttributes } from 'react';

import { cn } from '@/lib/utils';

interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
    label: string;
    value: string;
    counter?: boolean;
}

const REMAINING_SUFFIX = 'restantes';
const LOW_REMAINING_THRESHOLD = 20;

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
    ...rest
}: TextareaProps) {
    return (
        <div className={cn('flex flex-col gap-2', className)}>
            <div className="flex items-center justify-between text-sm text-zinc-400">
                <span>{label}</span>
                {counter && maxLength ? (
                    <CharacterCounter used={value.length} max={maxLength} />
                ) : null}
            </div>
            <textarea
                {...rest}
                value={value}
                maxLength={maxLength}
                aria-label={rest['aria-label'] ?? label}
                className="resize-none rounded-xl border border-sky-500 bg-transparent px-3 py-2 text-base font-medium text-zinc-100 caret-sky-400 outline-none transition-colors focus:border-sky-400"
            />
        </div>
    );
}
