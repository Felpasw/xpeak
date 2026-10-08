'use client';

import { formatDistanceToNow, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { Flag } from 'lucide-react';
import Link from 'next/link';
import type { Challenge } from '@xpeak/shared';
import { ChallengeState } from '@xpeak/shared';

interface ChallengeCardProps {
    challenge: Challenge;
}

const STATE_STYLES: Record<ChallengeState, string> = {
    [ChallengeState.Active]: 'border-sky-500/40 bg-sky-500/10 text-sky-300',
    [ChallengeState.Completed]: 'border-emerald-500/40 bg-emerald-500/10 text-emerald-300',
    [ChallengeState.Failed]: 'border-zinc-700 bg-zinc-800/60 text-zinc-400',
};

const STATE_LABEL: Record<ChallengeState, string> = {
    [ChallengeState.Active]: 'Ativo',
    [ChallengeState.Completed]: 'Completo',
    [ChallengeState.Failed]: 'Falhou',
};

export function ChallengeCard({ challenge }: ChallengeCardProps) {
    const endsIn = formatDistanceToNow(parseISO(challenge.endsAt), {
        addSuffix: true,
        locale: ptBR,
    });
    return (
        <Link
            href={`/challenges/${challenge.id}`}
            className="flex items-center gap-4 rounded-2xl border border-zinc-800 bg-zinc-900/40 p-4 transition-colors hover:border-zinc-700"
        >
            <div className="flex h-14 w-14 shrink-0 items-center justify-center rounded-xl border border-zinc-800 bg-zinc-900/60 text-sky-300">
                <Flag className="h-6 w-6" />
            </div>
            <div className="flex min-w-0 flex-1 flex-col gap-0.5">
                <h3 className="truncate text-sm font-semibold text-zinc-100">
                    {challenge.title}
                </h3>
                <p className="truncate text-xs text-zinc-400">
                    Level {challenge.targetLevel} · termina {endsIn}
                </p>
            </div>
            <span
                className={`shrink-0 rounded-full border px-3 py-1 font-mono text-xs font-semibold ${STATE_STYLES[challenge.state]}`}
            >
                {STATE_LABEL[challenge.state]}
            </span>
        </Link>
    );
}
