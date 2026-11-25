'use client';

import type { ScoringSnapshot } from '@xpeak/shared';

interface XpBreakdownProps {
    scoring: ScoringSnapshot;
    xpEarned: number;
}

export function XpBreakdown({ scoring, xpEarned }: XpBreakdownProps) {
    const { baseXp, multipliers } = scoring;
    const hasMultipliers = multipliers.length > 0;

    return (
        <section
            data-testid="xp-breakdown"
            className="flex flex-col gap-3 rounded-2xl border border-zinc-800 bg-zinc-900/40 p-4"
        >
            <h3 className="font-mono text-[11px] uppercase tracking-[0.35em] text-white/50">
                XP ganho
            </h3>
            <div className="flex items-baseline justify-between text-sm">
                <span className="text-zinc-400">Base</span>
                <span className="font-mono text-zinc-200">{baseXp} XP</span>
            </div>
            {hasMultipliers && (
                <div
                    data-testid="xp-breakdown-multipliers"
                    className="flex flex-col gap-1.5 border-t border-zinc-800/80 pt-3 text-xs"
                >
                    {multipliers.map((m) => (
                        <div
                            key={`${m.source}-${m.value}`}
                            className="flex items-baseline justify-between text-zinc-400"
                        >
                            <span className="truncate">{m.source}</span>
                            <span className="font-mono text-sky-300">
                                {Number.isInteger(m.value)
                                    ? `${m.value}×`
                                    : `${m.value.toFixed(2).replace(/0+$/, '').replace(/\.$/, '')}×`}
                            </span>
                        </div>
                    ))}
                </div>
            )}
            <div className="flex items-baseline justify-between border-t border-zinc-800/80 pt-3">
                <span className="text-sm font-semibold text-zinc-100">Total</span>
                <span
                    data-testid="xp-breakdown-total"
                    className="rounded-full border border-sky-500/40 bg-sky-500/10 px-3 py-1 font-mono text-sm font-semibold text-sky-300"
                >
                    +{xpEarned} XP
                </span>
            </div>
        </section>
    );
}
