'use client';

import { Dumbbell } from 'lucide-react';
import Image from 'next/image';
import type { MediaPreview } from '@xpeak/shared';

interface CheckinMediaHeroProps {
    mediaPreview: MediaPreview | null;
    title: string;
}

export function CheckinMediaHero({ mediaPreview, title }: CheckinMediaHeroProps) {
    if (!mediaPreview) {
        return (
            <div
                data-testid="checkin-media-hero-placeholder"
                className="relative flex aspect-square w-full items-center justify-center overflow-hidden rounded-2xl border border-zinc-800 bg-zinc-900/60 text-zinc-600"
                aria-hidden="true"
            >
                <Dumbbell className="h-16 w-16" />
            </div>
        );
    }

    return (
        <div
            data-testid="checkin-media-hero"
            className="relative aspect-square w-full overflow-hidden rounded-2xl border border-zinc-800 bg-black"
        >
            <Image
                src={mediaPreview.thumbUrl}
                alt={`Mídia do check-in ${title}`}
                fill
                unoptimized
                sizes="(max-width: 480px) 100vw, 480px"
                className="object-cover"
                priority
            />
        </div>
    );
}
