'use client';

import { Flame } from 'lucide-react';

import type { LevelProgress } from '@xpeak/shared';

interface HeroUser {
    username: string;
    level: number;
    xp: number;
    currentStreakDays: number;
    levelProgress: LevelProgress;
}

interface LevelProgressHeroProps {
    user: HeroUser;
}

export function LevelProgressHero({ user }: LevelProgressHeroProps) {
    const { username, level, xp, currentStreakDays, levelProgress } = user;
    const atCeiling = levelProgress.xpToNextLevel === 0;
    const fillPercent = Math.max(
        0,
        Math.min(1, atCeiling ? 1 : levelProgress.percent),
    );
    const remaining = atCeiling
        ? 0
        : Math.max(0, levelProgress.xpToNextLevel - levelProgress.xpIntoLevel);

    return (
        <section className="flex flex-col gap-4">
            <div className="flex items-start justify-between gap-4">
                <div className="flex flex-col gap-1">
                    <p className="font-mono text-[11px] uppercase tracking-[0.35em] text-white/50">
                        Welcome back
                    </p>
                    <h1 className="text-3xl font-semibold tracking-tight">
                        @{username}
                    </h1>
                    <p className="text-sm text-zinc-400">
                        Level {level} · {xp} XP
                    </p>
                </div>
                {currentStreakDays > 0 && (
                    <span
                        data-testid="streak-chip"
                        className="inline-flex items-center gap-1 rounded-full border border-amber-500/40 bg-amber-500/10 px-3 py-1 text-xs font-semibold text-amber-300"
                    >
                        <Flame className="h-3.5 w-3.5" />
                        {currentStreakDays}d
                    </span>
                )}
            </div>

            <div className="flex flex-col gap-2">
                <div
                    role="progressbar"
                    aria-valuemin={0}
                    aria-valuemax={100}
                    aria-valuenow={Math.round(fillPercent * 100)}
                    className="h-2 w-full overflow-hidden rounded-full bg-zinc-800"
                >
                    <div
                        data-testid="level-progress-fill"
                        className="h-full rounded-full bg-gradient-to-r from-sky-500 to-sky-300 transition-[width] duration-500"
                        style={{ width: `${fillPercent * 100}%` }}
                    />
                </div>
                <p className="text-xs text-zinc-500">
                    {atCeiling
                        ? 'Nível máximo alcançado'
                        : `Faltam ${remaining} XP pra Level ${level + 1}`}
                </p>
            </div>
        </section>
    );
}
