'use client';

import { ArrowLeft } from 'lucide-react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Suspense } from 'react';
import { ChallengeMembershipRole } from '@xpeak/shared';

import { ChallengeDetailNotFound } from '@/components/atoms/ChallengeDetailNotFound';
import { ChallengeDetailDeleteButton } from '@/components/organisms/ChallengeDetailDeleteButton';
import challengesHooks from '@/hooks/useChallenges';

const BACK_LABEL = 'Voltar';
const SECTION_LABEL = 'Configurações';

function ChallengeSettingsBody() {
    const searchParams = useSearchParams();
    const id = searchParams.get('id') ?? '';
    const detail = challengesHooks.useDetail(id);

    if (!id) {
        return <ChallengeDetailNotFound />;
    }

    if (detail.isPending) {
        return (
            <main className="flex flex-1 flex-col gap-6 px-6 pt-10 pb-48">
                <div
                    data-testid="challenge-settings-skeleton"
                    className="h-24 animate-pulse rounded-2xl border border-zinc-800 bg-zinc-900/40"
                />
            </main>
        );
    }

    if (detail.isError || !detail.data) {
        return <ChallengeDetailNotFound />;
    }

    const { challenge, membership } = detail.data;
    const isOwner = membership?.role === ChallengeMembershipRole.Owner;

    if (!isOwner) {
        return <ChallengeDetailNotFound />;
    }

    const backHref = `/challenges/view?id=${challenge.id}`;

    return (
        <main className="flex flex-1 flex-col gap-6 px-6 pt-10 pb-48">
            <Link
                href={backHref}
                className="inline-flex w-fit items-center gap-2 text-xs font-semibold text-sky-300 transition-colors hover:text-sky-200"
            >
                <ArrowLeft className="h-3.5 w-3.5" />
                {BACK_LABEL}
            </Link>
            <header className="flex flex-col gap-1">
                <p className="font-mono text-[11px] uppercase tracking-[0.35em] text-sky-300">
                    {SECTION_LABEL}
                </p>
                <h1 className="truncate text-2xl font-semibold tracking-tight text-zinc-100">
                    {challenge.title}
                </h1>
            </header>

            <section className="flex flex-col gap-3">
                <h2 className="font-mono text-[11px] uppercase tracking-[0.35em] text-sky-300">
                    Área perigosa
                </h2>
                <ChallengeDetailDeleteButton challengeId={challenge.id} />
            </section>
        </main>
    );
}

export default function ChallengeSettingsPage() {
    return (
        <Suspense fallback={null}>
            <ChallengeSettingsBody />
        </Suspense>
    );
}
