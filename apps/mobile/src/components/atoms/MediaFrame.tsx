'use client';

import type { GalleryItem } from '@/components/atoms/MediaGallery';

interface MediaFrameProps {
    item: GalleryItem;
    index: number;
}

export function MediaFrame({ item, index }: MediaFrameProps) {
    if (item.kind === 'video') {
        return (
            <video
                key={item.id}
                controls
                playsInline
                className="max-h-[85vh] max-w-[90vw] rounded-lg"
            >
                <source src={item.url} />
            </video>
        );
    }
    return (
        // eslint-disable-next-line @next/next/no-img-element
        <img
            key={item.id}
            src={item.url}
            alt={`Mídia ${index + 1}`}
            className="max-h-[85vh] max-w-[90vw] rounded-lg object-contain"
        />
    );
}
