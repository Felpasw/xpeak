'use client';

import { Flag } from 'lucide-react';
import Link from 'next/link';

import { ProgressBar } from '@/components/atoms/ProgressBar';
import challengesHooks from '@/hooks/useChallenges';

const SECTION_LABEL = 'Meus challenges';
const ERROR_COPY = 'Falhou carregar os challenges.';
const RETRY_LABEL = 'Tentar de novo';

function Skeleton() {
    return (
        <div
            data-testid="my-challenges-skeleton"
            className="flex flex-col gap-3"
        >
            <div className="h-20 animate-pulse rounded-2xl border border-zinc-800 bg-zinc-900/40" />
            <div className="h-20 animate-pulse rounded-2xl border border-zinc-800 bg-zinc-900/40" />
        </div>
    );
}

export function MyChallengesSummary() {
    const query = challengesHooks.useSummary();

    if (query.isPending) {
        return <Skeleton />;
    }

    if (query.isError) {
        return (
            <div
                data-testid="my-challenges-error"
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

    const summaries = query.data;
    if (summaries.length === 0) {
        return null;
    }

    return (
        <section className="flex flex-col gap-4">
            <h2 className="font-mono text-[11px] uppercase tracking-[0.35em] text-white/50">
                {SECTION_LABEL}
            </h2>
            <div className="flex flex-col gap-3">
                {summaries.map((summary) => (
                    <Link
                        key={summary.id}
                        href={`/challenges/view?id=${summary.id}`}
                        className="flex flex-col gap-3 rounded-2xl border border-zinc-800 bg-zinc-900/40 p-4 transition-colors hover:border-zinc-700"
                    >
                        <div className="flex items-center gap-3">
                            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg border border-zinc-800 bg-zinc-900/60 text-sky-300">
                                <Flag className="h-4 w-4" />
                            </div>
                            <h3 className="truncate text-sm font-semibold text-zinc-100">
                                {summary.title}
                            </h3>
                        </div>
                        <ProgressBar
                            value={summary.progress.value}
                            target={summary.progress.target}
                            achieved={summary.progress.achievedAt !== null}
                        />
                    </Link>
                ))}
            </div>
        </section>
    );
}
