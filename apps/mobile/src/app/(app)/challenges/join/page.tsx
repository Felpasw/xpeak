'use client';

import { Suspense, useEffect, useRef } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { toast } from 'sonner';

import { PageHeader } from '@/components/atoms/PageHeader';
import challengesHooks from '@/hooks/useChallenges';

const TITLE = 'Entrando no challenge';
const DESCRIPTION = 'Validando o convite...';
const INVALID_LINK = 'Convite inválido.';
const JOIN_ERROR = 'Não deu pra entrar nesse challenge. Convite pode ter expirado.';
const JOIN_SUCCESS = 'Entrou no challenge!';

function JoinChallengeBody() {
    const searchParams = useSearchParams();
    const router = useRouter();
    const invitationId = searchParams.get('invitation') ?? '';
    const joinChallenge = challengesHooks.useJoin();
    // StrictMode double-mounts client components in dev — the ref keeps
    // the mutation from firing twice on the same invitation.
    const firedRef = useRef(false);

    useEffect(() => {
        if (firedRef.current) {
            return;
        }
        firedRef.current = true;

        if (!invitationId) {
            toast.error(INVALID_LINK);
            router.replace('/challenges');
            return;
        }

        joinChallenge.mutate(
            { invitationId },
            {
                onSuccess: (result) => {
                    toast.success(JOIN_SUCCESS);
                    router.replace(`/challenges/view?id=${result.challenge.id}`);
                },
                onError: () => {
                    toast.error(JOIN_ERROR);
                    router.replace('/challenges');
                },
            },
        );
    }, [invitationId, joinChallenge, router]);

    return (
        <main className="flex flex-1 flex-col gap-8 px-6 pt-10 pb-48">
            <PageHeader title={TITLE} description={DESCRIPTION} />
            <div
                data-testid="join-challenge-skeleton"
                className="h-24 animate-pulse rounded-2xl border border-zinc-800 bg-zinc-900/40"
            />
        </main>
    );
}

export default function JoinChallengePage() {
    return (
        <Suspense fallback={null}>
            <JoinChallengeBody />
        </Suspense>
    );
}
