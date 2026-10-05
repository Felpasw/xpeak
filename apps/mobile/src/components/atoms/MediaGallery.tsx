'use client';

import { Play } from 'lucide-react';
import Image from 'next/image';
import { useState } from 'react';

import { MediaViewer } from '@/components/atoms/MediaViewer';
import type { MediaKind } from '@xpeak/shared';
import { cn } from '@/lib/utils';

export interface GalleryItem {
    id: string;
    kind: MediaKind;
    url: string;
    thumbnailUrl?: string;
}

interface MediaGalleryProps {
    items: GalleryItem[];
    className?: string;
}

export function MediaGallery({ items, className }: MediaGalleryProps) {
    const [viewerIndex, setViewerIndex] = useState<number | null>(null);

    if (items.length === 0) return null;

    return (
        <>
            <ul className={cn('grid grid-cols-4 gap-2', className)}>
                {items.map((item, index) => (
                    <li key={item.id} className="aspect-square">
                        <button
                            type="button"
                            aria-label={`Abrir mídia ${index + 1}`}
                            onClick={() => setViewerIndex(index)}
                            className="relative block h-full w-full overflow-hidden rounded-lg border border-zinc-800 transition-opacity hover:opacity-80 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-400"
                        >
                            <Image
                                src={item.thumbnailUrl ?? item.url}
                                alt={`Mídia ${index + 1}`}
                                fill
                                unoptimized
                                sizes="25vw"
                                className="object-cover"
                            />
                            {item.kind === 'video' ? (
                                <span
                                    aria-hidden="true"
                                    className="absolute inset-0 flex items-center justify-center bg-black/30"
                                >
                                    <Play className="h-6 w-6 text-white" fill="currentColor" />
                                </span>
                            ) : null}
                        </button>
                    </li>
                ))}
            </ul>

            <MediaViewer
                open={viewerIndex !== null}
                items={items}
                activeIndex={viewerIndex ?? 0}
                onClose={() => setViewerIndex(null)}
                onNavigate={setViewerIndex}
            />
        </>
    );
}
