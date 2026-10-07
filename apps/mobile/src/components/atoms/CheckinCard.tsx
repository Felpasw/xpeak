'use client';

import { formatDistanceToNow } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { Dumbbell } from 'lucide-react';
import Image from 'next/image';
import type { CheckIn } from '@xpeak/shared';

interface CheckinCardProps {
    checkIn: CheckIn;
}

export function CheckinCard({ checkIn }: CheckinCardProps) {
    const when = formatDistanceToNow(new Date(checkIn.performedAt), {
        addSuffix: true,
        locale: ptBR,
    });
    const subtitle = `${checkIn.category.name} · ${when}`;

    return (
        <article className="flex items-center gap-4 rounded-2xl border border-zinc-800 bg-zinc-900/40 p-4">
            {checkIn.mediaPreview ? (
                <div className="relative h-16 w-16 shrink-0 overflow-hidden rounded-xl border border-zinc-800">
                    <Image
                        src={checkIn.mediaPreview.thumbUrl}
                        alt={`Mídia do check-in ${checkIn.title}`}
                        fill
                        unoptimized
                        sizes="64px"
                        className="object-cover"
                    />
                </div>
            ) : (
                <div
                    data-testid="checkin-card-placeholder"
                    className="flex h-16 w-16 shrink-0 items-center justify-center rounded-xl border border-zinc-800 bg-zinc-900/60 text-zinc-500"
                    aria-hidden="true"
                >
                    <Dumbbell className="h-6 w-6" />
                </div>
            )}

            <div className="flex min-w-0 flex-1 flex-col gap-0.5">
                <h3 className="truncate text-sm font-semibold text-zinc-100">
                    {checkIn.title}
                </h3>
                <p className="truncate text-xs text-zinc-400">{subtitle}</p>
                {checkIn.notes && (
                    <p className="truncate text-xs text-zinc-500">{checkIn.notes}</p>
                )}
            </div>

            <span className="shrink-0 rounded-full border border-sky-500/40 bg-sky-500/10 px-3 py-1 font-mono text-xs font-semibold text-sky-300">
                +{checkIn.xpEarned} XP
            </span>
        </article>
    );
}
