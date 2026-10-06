'use client';

import { ChevronLeft, ChevronRight } from 'lucide-react';

import { cn } from '@/lib/utils';

interface MediaViewerNavProps {
    label: string;
    side: 'left' | 'right';
    disabled: boolean;
    onClick: () => void;
}

const ICONS = {
    left: ChevronLeft,
    right: ChevronRight,
} as const;

export function MediaViewerNav({ label, side, disabled, onClick }: MediaViewerNavProps) {
    const Icon = ICONS[side];
    return (
        <button
            type="button"
            aria-label={label}
            data-side={side}
            onClick={onClick}
            disabled={disabled}
            className={cn(
                'absolute top-1/2 flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full bg-white/10 text-white backdrop-blur-md transition-colors',
                'hover:bg-white/20 disabled:cursor-not-allowed disabled:opacity-30 disabled:hover:bg-white/10',
                'data-[side=left]:left-4 data-[side=right]:right-4',
            )}
        >
            <Icon className="h-6 w-6" />
        </button>
    );
}
