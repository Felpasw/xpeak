'use client';

import { X } from 'lucide-react';
import Image from 'next/image';

interface MediaThumbProps {
    src: string;
    index: number;
    onRemove: () => void;
}

export function MediaThumb({ src, index, onRemove }: MediaThumbProps) {
    const position = index + 1;
    return (
        <li className="relative aspect-square overflow-hidden rounded-lg border border-zinc-800">
            <Image
                src={src}
                alt={`Mídia ${position}`}
                fill
                unoptimized
                sizes="25vw"
                className="object-cover"
            />
            <button
                type="button"
                aria-label={`Remover mídia ${position}`}
                onClick={onRemove}
                className="absolute -top-1.5 -right-1.5 flex h-6 w-6 items-center justify-center rounded-full bg-zinc-900 text-zinc-300 ring-1 ring-zinc-700 transition-colors hover:bg-zinc-800 hover:text-zinc-100"
            >
                <X className="h-3.5 w-3.5" />
            </button>
        </li>
    );
}
