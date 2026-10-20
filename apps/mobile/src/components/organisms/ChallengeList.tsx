'use client';

import type { ChallengeState } from '@xpeak/shared';

import { ChallengeCard } from '@/components/atoms/ChallengeCard';
import { CheckinListSkeleton } from '@/components/atoms/CheckinListSkeleton';
import challengesHooks from '@/hooks/useChallenges';

interface ChallengeListProps {
    state?: ChallengeState;
}

const EMPTY_COPY = 'Nenhum challenge por aqui ainda. Cria o primeiro?';
const ERROR_COPY = 'Falhou carregar os challenges.';
const RETRY_LABEL = 'Tentar de novo';

export function ChallengeList({ state }: ChallengeListProps) {
    const query = challengesHooks.useList({ state });

    if (query.isPending) {
        return <CheckinListSkeleton />;
    }

    if (query.isError) {
        return (
            <div
                data-testid="challenge-list-error"
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

    if (query.data.length === 0) {
        return (
            <div
                data-testid="challenge-list-empty"
                className="flex flex-col items-center gap-2 rounded-2xl border border-dashed border-zinc-800 bg-zinc-900/20 p-10 text-center text-sm text-zinc-400"
            >
                {EMPTY_COPY}
            </div>
        );
    }

    return (
        <div className="flex flex-col gap-3">
            {query.data.map((challenge) => (
                <ChallengeCard key={challenge.id} challenge={challenge} />
            ))}
        </div>
    );
}
