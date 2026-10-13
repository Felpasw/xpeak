'use client';

import { useParams } from 'next/navigation';
import { ChallengeMembershipRole } from '@xpeak/shared';

import { ChallengeDetailNotFound } from '@/components/atoms/ChallengeDetailNotFound';
import { ChallengeDetailDeleteButton } from '@/components/organisms/ChallengeDetailDeleteButton';
import { ChallengeDetailHeader } from '@/components/organisms/ChallengeDetailHeader';
import { ChallengeDetailProgress } from '@/components/organisms/ChallengeDetailProgress';
import { useChallenge } from '@/hooks/useChallenges';

export default function ChallengeDetailPage() {
    const params = useParams<{ id: string }>();
    const id = params.id ?? '';
    const detail = useChallenge(id);

    if (detail.isPending) {
        return (
            <main className="flex flex-1 flex-col gap-6 px-6 pt-10 pb-48">
                <div
                    data-testid="challenge-detail-skeleton"
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

    return (
        <main className="flex flex-1 flex-col gap-6 px-6 pt-10 pb-48">
            <ChallengeDetailHeader challenge={challenge} />
            <ChallengeDetailProgress challengeId={challenge.id} />
            {isOwner && <ChallengeDetailDeleteButton challengeId={challenge.id} />}
        </main>
    );
}
