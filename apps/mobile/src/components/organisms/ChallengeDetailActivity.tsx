'use client';

import { ChallengeActivityRow } from '@/components/atoms/ChallengeActivityRow';
import { ChallengeActivitySkeleton } from '@/components/atoms/ChallengeActivitySkeleton';
import challengesHooks from '@/hooks/useChallenges';

interface ChallengeDetailActivityProps {
    challengeId: string;
}

const SECTION_LABEL = 'Atividade';
const ERROR_COPY = 'Falhou carregar a atividade.';
const EMPTY_COPY = 'Sem check-ins no período ainda.';
const LOAD_MORE_COPY = 'Ver mais';
const LOADING_MORE_COPY = 'Carregando…';

export function ChallengeDetailActivity({ challengeId }: ChallengeDetailActivityProps) {
    const feed = challengesHooks.useCheckIns(challengeId);

    return (
        <section className="flex flex-col gap-4">
            <h2 className="font-mono text-[11px] uppercase tracking-[0.35em] text-sky-300">
                {SECTION_LABEL}
            </h2>
            {renderBody()}
        </section>
    );

    function renderBody() {
        if (feed.isPending) {
            return <ChallengeActivitySkeleton />;
        }
        if (feed.isError || !feed.data) {
            return <p className="text-sm text-zinc-400">{ERROR_COPY}</p>;
        }
        const entries = feed.data.pages.flatMap((page) => page.items);
        if (entries.length === 0) {
            return <p className="text-sm text-zinc-400">{EMPTY_COPY}</p>;
        }
        return (
            <>
                <ol className="flex flex-col gap-4">
                    {entries.map((entry) => (
                        <ChallengeActivityRow key={entry.checkIn.id} entry={entry} />
                    ))}
                </ol>
                {feed.hasNextPage && (
                    <button
                        type="button"
                        onClick={() => feed.fetchNextPage()}
                        disabled={feed.isFetchingNextPage}
                        className="self-center rounded-full border border-zinc-700 bg-zinc-900/60 px-4 py-1.5 text-xs font-medium text-zinc-200 transition hover:border-sky-500/50 hover:text-sky-200 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                        {feed.isFetchingNextPage ? LOADING_MORE_COPY : LOAD_MORE_COPY}
                    </button>
                )}
            </>
        );
    }
}
