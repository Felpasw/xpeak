'use client';

import { PageHeader } from '@/components/atoms/PageHeader';
import { CreateChallengeForm } from '@/components/organisms/CreateChallengeForm';

const TITLE = 'Novo challenge';
const DESCRIPTION = 'Define o alvo e as categorias que contam.';

export default function NewChallengePage() {
    return (
        <main className="flex flex-1 flex-col gap-8 px-6 pt-10 pb-48">
            <PageHeader title={TITLE} description={DESCRIPTION} />
            <CreateChallengeForm />
        </main>
    );
}
