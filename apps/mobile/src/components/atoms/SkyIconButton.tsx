'use client';

import Link from 'next/link';
import type { LucideIcon } from 'lucide-react';

interface SkyIconButtonProps {
    href: string;
    Icon: LucideIcon;
    ariaLabel: string;
}

const CLASSES =
    'inline-flex aspect-square shrink-0 items-center justify-center rounded-2xl px-3 ' +
    'border border-sky-500/40 bg-gradient-to-br from-sky-500/15 via-sky-500/10 to-transparent ' +
    'text-sky-100 shadow-[0_0_24px_-10px_rgba(56,189,248,0.6)] transition-colors ' +
    'hover:border-sky-400/70 hover:bg-sky-500/25';

export function SkyIconButton({ href, Icon, ariaLabel }: SkyIconButtonProps) {
    return (
        <Link href={href} aria-label={ariaLabel} className={CLASSES}>
            <Icon className="h-4 w-4" />
        </Link>
    );
}
