'use client';

import { useEffect, useRef } from 'react';

import { CheckinCard } from '@/components/atoms/CheckinCard';
import { CheckinListSkeleton } from '@/components/atoms/CheckinListSkeleton';
import { useCheckInsList } from '@/hooks/useCheckIn';

interface CheckinListProps {
    groupId?: string;
    initialLimit?: number;
}

const EMPTY_COPY = 'Ainda sem check-ins neste grupo. Bora treinar?';
const ERROR_COPY = 'Falhou carregar o histórico.';
const RETRY_LABEL = 'Tentar de novo';

export function CheckinList({ groupId, initialLimit }: CheckinListProps) {
    const query = useCheckInsList({ groupId, limit: initialLimit });
    const sentinelRef = useRef<HTMLDivElement>(null);

    const { fetchNextPage, hasNextPage, isFetchingNextPage } = query;

    useEffect(() => {
        const node = sentinelRef.current;
        if (!node || !hasNextPage) {
            return;
        }
        const observer = new IntersectionObserver(([entry]) => {
            if (entry?.isIntersecting && !isFetchingNextPage) {
                fetchNextPage();
            }
        });
        observer.observe(node);
        return () => observer.disconnect();
    }, [fetchNextPage, hasNextPage, isFetchingNextPage]);

    if (query.isPending) {
        return <CheckinListSkeleton />;
    }

    if (query.isError) {
        return (
            <div
                data-testid="checkin-list-error"
                className="flex flex-col items-center gap-3 rounded-2xl border border-zinc-800 bg-zinc-900/40 p-8 text-center"
            >
                <p className="text-sm text-zinc-300">{ERROR_COPY}</p>
                <button
                    type="button"
                    onClick={() => query.refetch()}
                    className="rounded-full border border-sky-500/40 bg-sky-500/10 px-4 py-2 text-xs font-semibold text-sky-300 transition-colors hover:border-sky-400/70 hover:text-sky-200"
                >
                    {RETRY_LABEL}
                </button>
            </div>
        );
    }

    const items = query.data.pages.flatMap((p) => p.checkIns);

    if (items.length === 0) {
        return (
            <div
                data-testid="checkin-list-empty"
                className="flex flex-col items-center gap-2 rounded-2xl border border-dashed border-zinc-800 bg-zinc-900/20 p-10 text-center text-sm text-zinc-400"
            >
                {EMPTY_COPY}
            </div>
        );
    }

    return (
        <div className="flex flex-col gap-3">
            {items.map((checkIn) => (
                <CheckinCard key={checkIn.id} checkIn={checkIn} />
            ))}
            <div ref={sentinelRef} aria-hidden="true" className="h-4" />
        </div>
    );
}
