'use client';

import type { ButtonHTMLAttributes } from 'react';
import { forwardRef } from 'react';

type Variant = 'primary' | 'secondary';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
    variant?: Variant;
}

const BASE_CLASSES =
    'flex items-center justify-center rounded-2xl px-4 py-3 text-sm font-semibold transition-colors disabled:opacity-40 disabled:cursor-not-allowed ' +
    'data-[variant=primary]:border data-[variant=primary]:border-sky-500/40 data-[variant=primary]:bg-gradient-to-br data-[variant=primary]:from-sky-500/15 data-[variant=primary]:via-sky-500/10 data-[variant=primary]:to-transparent data-[variant=primary]:text-sky-100 data-[variant=primary]:shadow-[0_0_24px_-10px_rgba(56,189,248,0.6)] data-[variant=primary]:hover:border-sky-400/70 data-[variant=primary]:hover:bg-sky-500/25 data-[variant=primary]:disabled:shadow-none ' +
    'data-[variant=secondary]:border data-[variant=secondary]:border-zinc-800 data-[variant=secondary]:bg-zinc-900 data-[variant=secondary]:text-zinc-100 data-[variant=secondary]:hover:bg-zinc-800';

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button(
    { variant = 'primary', className, type = 'button', ...rest },
    ref,
) {
    return (
        <button
            ref={ref}
            type={type}
            data-variant={variant}
            className={`${BASE_CLASSES} ${className ?? ''}`}
            {...rest}
        />
    );
});
