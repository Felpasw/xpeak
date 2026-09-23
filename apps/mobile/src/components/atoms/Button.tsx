'use client';

import type { ButtonHTMLAttributes } from 'react';
import { forwardRef } from 'react';

type Variant = 'primary' | 'secondary';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
    variant?: Variant;
}

const BASE_CLASSES =
    'rounded-md px-4 py-3 text-sm font-semibold transition-colors disabled:opacity-60 ' +
    'data-[variant=primary]:bg-sky-500 data-[variant=primary]:text-zinc-950 data-[variant=primary]:hover:bg-sky-400 ' +
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
