'use client';

import { Activity, CheckCircle2, Plus, XCircle } from 'lucide-react';
import Link from 'next/link';
import { useState } from 'react';
import { ChallengeState } from '@xpeak/shared';

import { DiscreteTabs, type DiscreteTab } from '@/components/atoms/DiscreteTabs';
import { PageHeader } from '@/components/atoms/PageHeader';
import { ChallengeList } from '@/components/organisms/ChallengeList';

const TITLE = 'Challenges';
const DESCRIPTION = 'Metas pessoais rumo ao próximo level.';
const NEW_LABEL = 'Novo';

const TABS: DiscreteTab[] = [
    { id: ChallengeState.Active, title: 'Ativos', Icon: Activity },
    { id: ChallengeState.Completed, title: 'Completos', Icon: CheckCircle2 },
    { id: ChallengeState.Failed, title: 'Falhos', Icon: XCircle },
];

export default function ChallengesPage() {
    const [state, setState] = useState<ChallengeState>(ChallengeState.Active);

    return (
        <main className="flex flex-1 flex-col gap-6 px-6 pt-10 pb-48">
            <header className="flex items-start justify-between gap-4">
                <PageHeader title={TITLE} description={DESCRIPTION} />
                <Link
                    href="/challenges/new"
                    className="flex items-center gap-1 rounded-full border border-sky-500/40 bg-sky-500/10 px-4 py-2 text-xs font-semibold text-sky-300 transition-colors hover:border-sky-400/70 hover:text-sky-200"
                    aria-label="Criar novo challenge"
                >
                    <Plus className="h-4 w-4" />
                    {NEW_LABEL}
                </Link>
            </header>

            <DiscreteTabs
                tabs={TABS}
                activeId={state}
                onChange={(id) => setState(id as ChallengeState)}
            />

            <ChallengeList state={state} />
        </main>
    );
}
