'use client';

import type { ChallengeTier } from '@xpeak/shared';
import clsx from 'clsx';

interface ChallengeDetailTiersProps {
    tiers: ChallengeTier[];
    currentTier: ChallengeTier | null;
}

const SECTION_LABEL = 'Fases';

enum TierStatus {
    Reached = 'reached',
    Current = 'current',
    Next = 'next',
    Upcoming = 'upcoming',
}

const STATUS_LABELS: Record<TierStatus, string> = {
    [TierStatus.Reached]: 'Alcançado',
    [TierStatus.Current]: 'Atual',
    [TierStatus.Next]: 'Próximo',
    [TierStatus.Upcoming]: 'Em breve',
};

const STATUS_STYLES: Record<TierStatus, { row: string; dot: string; pill: string }> = {
    [TierStatus.Reached]: {
        row: 'text-zinc-500',
        dot: 'bg-zinc-700',
        pill: 'bg-zinc-800 text-zinc-500',
    },
    [TierStatus.Current]: {
        row: 'text-emerald-200',
        dot: 'bg-emerald-400 ring-2 ring-emerald-400/30',
        pill: 'bg-emerald-500/20 text-emerald-200',
    },
    [TierStatus.Next]: {
        row: 'text-zinc-100',
        dot: 'bg-zinc-500',
        pill: 'bg-sky-500/20 text-sky-200',
    },
    [TierStatus.Upcoming]: {
        row: 'text-zinc-400',
        dot: 'bg-zinc-700',
        pill: 'bg-zinc-800 text-zinc-500',
    },
};

function resolveStatus(index: number, currentIndex: number): TierStatus {
    if (index === currentIndex) {
        return TierStatus.Current;
    }
    if (index < currentIndex) {
        return TierStatus.Reached;
    }
    if (index === currentIndex + 1) {
        return TierStatus.Next;
    }
    return TierStatus.Upcoming;
}

export function ChallengeDetailTiers({ tiers, currentTier }: ChallengeDetailTiersProps) {
    if (tiers.length === 0) {
        return null;
    }

    const currentIndex = currentTier
        ? tiers.findIndex((t) => t.id === currentTier.id)
        : -1;

    return (
        <section
            data-testid="challenge-detail-tiers"
            className="flex flex-col gap-3 rounded-2xl border border-zinc-800 bg-zinc-900/40 p-5"
        >
            <h2 className="font-mono text-xs uppercase tracking-[0.25em] text-zinc-500">
                {SECTION_LABEL}
            </h2>
            <ol className="flex flex-col gap-2">
                {tiers.map((tier, index) => {
                    const status = resolveStatus(index, currentIndex);
                    const styles = STATUS_STYLES[status];
                    return (
                        <li
                            key={tier.id}
                            data-testid={`challenge-tier-${index}`}
                            data-status={status}
                            className={clsx(
                                'flex items-center gap-3 rounded-xl bg-zinc-950/40 px-3 py-2',
                                styles.row,
                            )}
                        >
                            {tier.imageUrl ? (
                                // Cloudinary signed URL built server-side; cannot
                                // enumerate every challenge owner's cloud name in
                                // images.remotePatterns ahead of time.
                                // eslint-disable-next-line @next/next/no-img-element
                                <img
                                    src={tier.imageUrl}
                                    alt=""
                                    className="h-8 w-8 rounded-md border border-zinc-800 object-cover"
                                />
                            ) : (
                                <span
                                    aria-hidden="true"
                                    className={clsx('h-2.5 w-2.5 rounded-full', styles.dot)}
                                />
                            )}
                            <span className="w-16 font-mono text-xs text-zinc-500">
                                Lv {tier.minLevel}
                            </span>
                            <span className="flex-1 text-sm">{tier.title}</span>
                            <span
                                className={clsx(
                                    'rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-widest',
                                    styles.pill,
                                )}
                            >
                                {STATUS_LABELS[status]}
                            </span>
                        </li>
                    );
                })}
            </ol>
        </section>
    );
}
