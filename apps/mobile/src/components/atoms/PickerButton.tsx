'use client';

import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

interface PickerButtonProps {
    label: string;
    icon: ReactNode;
    disabled: boolean;
    onClick: () => void;
}

export function PickerButton({ label, icon, disabled, onClick }: PickerButtonProps) {
    return (
        <button
            type="button"
            onClick={onClick}
            disabled={disabled}
            className={cn(
                'inline-flex items-center gap-2 rounded-xl border border-zinc-800 bg-zinc-900/60 px-3 py-2 text-sm font-medium text-zinc-200 transition-colors',
                'hover:border-zinc-700 hover:bg-zinc-800/60',
                'disabled:cursor-not-allowed disabled:opacity-50 disabled:hover:border-zinc-800 disabled:hover:bg-zinc-900/60',
            )}
        >
            {icon}
            {label}
        </button>
    );
}
