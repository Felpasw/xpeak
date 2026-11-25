'use client';

import { Settings } from 'lucide-react';
import { Suspense } from 'react';
import { useSearchParams } from 'next/navigation';
import { ChallengeMembershipRole } from '@xpeak/shared';

import { ChallengeDetailNotFound } from '@/components/atoms/ChallengeDetailNotFound';
import { SkyIconButton } from '@/components/atoms/SkyIconButton';
import { ChallengeDetailActivity } from '@/components/organisms/ChallengeDetailActivity';
import { ChallengeDetailHeader } from '@/components/organisms/ChallengeDetailHeader';
import { ChallengeDetailMembers } from '@/components/organisms/ChallengeDetailMembers';
import { ChallengeDetailProgress } from '@/components/organisms/ChallengeDetailProgress';
import { ChallengeDetailShareButton } from '@/components/organisms/ChallengeDetailShareButton';
import { ChallengeDetailTiers } from '@/components/organisms/ChallengeDetailTiers';
import challengesHooks from '@/hooks/useChallenges';

function ChallengeDetailBody() {
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
                    data-testid="challenge-detail-skeleton"
                    className="h-24 animate-pulse rounded-2xl border border-zinc-800 bg-zinc-900/40"
                />
            </main>
        );
    }

    if (detail.isError || !detail.data) {
        return <ChallengeDetailNotFound />;
    }

    const { challenge, membership, shareLink, tiers, currentTier } = detail.data;
    const isOwner = membership?.role === ChallengeMembershipRole.Owner;

    return (
        <main className="flex flex-1 flex-col gap-6 px-6 pt-10 pb-48">
            <ChallengeDetailHeader challenge={challenge} />
            {isOwner && (
                <div className="flex items-stretch gap-3">
                    {shareLink && (
                        <div className="flex-1">
                            <ChallengeDetailShareButton invitationId={shareLink.id} />
                        </div>
                    )}
                    <SkyIconButton
                        href={`/challenges/view/settings?id=${challenge.id}`}
                        Icon={Settings}
                        ariaLabel="Configurar challenge"
                    />
                </div>
            )}
            <ChallengeDetailProgress challengeId={challenge.id} />
            <ChallengeDetailTiers tiers={tiers} currentTier={currentTier} />
            <ChallengeDetailMembers challengeId={challenge.id} />
            <ChallengeDetailActivity challengeId={challenge.id} />
        </main>
    );
}

export default function ChallengeDetailPage() {
    return (
        <Suspense fallback={null}>
            <ChallengeDetailBody />
        </Suspense>
    );
}
