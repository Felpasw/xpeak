'use client';

import { formatDistanceToNow, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { Flag } from 'lucide-react';
import Link from 'next/link';
import type { Challenge } from '@xpeak/shared';

import { ChallengeStateBadge } from '@/components/atoms/ChallengeStateBadge';

interface ChallengeCardProps {
    challenge: Challenge;
}

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
            <ChallengeStateBadge state={challenge.state} />
        </Link>
    );
}
