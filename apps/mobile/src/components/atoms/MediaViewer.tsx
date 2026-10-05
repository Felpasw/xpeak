'use client';

import { X } from 'lucide-react';
import { AnimatePresence, motion } from 'motion/react';
import { useEffect } from 'react';

import type { GalleryItem } from '@/components/atoms/MediaGallery';
import { MediaFrame } from '@/components/atoms/MediaFrame';
import { MediaViewerNav } from '@/components/atoms/MediaViewerNav';

interface MediaViewerProps {
    open: boolean;
    items: GalleryItem[];
    activeIndex: number;
    onClose: () => void;
    onNavigate: (next: number) => void;
}

const DIALOG_LABEL = 'Visualizar mídia';
const CLOSE_LABEL = 'Fechar';
const PREV_LABEL = 'Anterior';
const NEXT_LABEL = 'Próxima';

export function MediaViewer({
    open,
    items,
    activeIndex,
    onClose,
    onNavigate,
}: MediaViewerProps) {
    useEffect(() => {
        if (!open) return;
        const onKey = (event: KeyboardEvent) => {
            if (event.key === 'Escape') onClose();
            if (event.key === 'ArrowLeft' && activeIndex > 0) onNavigate(activeIndex - 1);
            if (event.key === 'ArrowRight' && activeIndex < items.length - 1) {
                onNavigate(activeIndex + 1);
            }
        };
        document.addEventListener('keydown', onKey);
        return () => document.removeEventListener('keydown', onKey);
    }, [open, activeIndex, items.length, onClose, onNavigate]);

    const current = items[activeIndex];
    const hasPrev = activeIndex > 0;
    const hasNext = activeIndex < items.length - 1;

    return (
        <AnimatePresence>
            {open && current ? (
                <motion.div
                    role="dialog"
                    aria-modal="true"
                    aria-label={DIALOG_LABEL}
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    exit={{ opacity: 0 }}
                    transition={{ duration: 0.18 }}
                    className="fixed inset-0 z-50 flex items-center justify-center bg-black/95"
                >
                    <button
                        type="button"
                        aria-label={CLOSE_LABEL}
                        onClick={onClose}
                        className="absolute top-4 right-4 flex h-10 w-10 items-center justify-center rounded-full bg-white/10 text-white backdrop-blur-md transition-colors hover:bg-white/20"
                    >
                        <X className="h-5 w-5" />
                    </button>

                    {items.length > 1 ? (
                        <MediaViewerNav
                            label={PREV_LABEL}
                            side="left"
                            disabled={!hasPrev}
                            onClick={() => onNavigate(activeIndex - 1)}
                        />
                    ) : null}

                    <MediaFrame item={current} index={activeIndex} />

                    {items.length > 1 ? (
                        <MediaViewerNav
                            label={NEXT_LABEL}
                            side="right"
                            disabled={!hasNext}
                            onClick={() => onNavigate(activeIndex + 1)}
                        />
                    ) : null}

                    {items.length > 1 ? (
                        <span className="absolute bottom-6 left-1/2 -translate-x-1/2 rounded-full bg-black/60 px-3 py-1 text-xs font-medium tabular-nums text-white">
                            {activeIndex + 1} / {items.length}
                        </span>
                    ) : null}
                </motion.div>
            ) : null}
        </AnimatePresence>
    );
}
