'use client';

import { ProgressBar } from '@/components/atoms/ProgressBar';
import challengesHooks from '@/hooks/useChallenges';

interface ChallengeDetailProgressProps {
    challengeId: string;
}

const SECTION_LABEL = 'Progresso';
const ERROR_COPY = 'Falhou carregar o progresso.';

export function ChallengeDetailProgress({ challengeId }: ChallengeDetailProgressProps) {
    const query = challengesHooks.useProgress(challengeId);

    return (
        <section className="flex flex-col gap-3 rounded-2xl border border-zinc-800 bg-zinc-900/40 p-5">
            <h2 className="font-mono text-xs uppercase tracking-[0.25em] text-zinc-500">
                {SECTION_LABEL}
            </h2>
            {renderBody()}
        </section>
    );

    function renderBody() {
        if (query.isPending) {
            return (
                <div
                    data-testid="challenge-progress-skeleton"
                    className="h-6 animate-pulse rounded bg-zinc-800"
                />
            );
        }
        if (query.isError || !query.data) {
            return <p className="text-sm text-zinc-400">{ERROR_COPY}</p>;
        }
        return (
            <ProgressBar
                value={query.data.progress.value}
                target={query.data.progress.target}
                achieved={query.data.progress.achievedAt !== null}
            />
        );
    }
}
