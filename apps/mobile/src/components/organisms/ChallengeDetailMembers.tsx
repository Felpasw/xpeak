'use client';

import { ChallengeMemberRow } from '@/components/atoms/ChallengeMemberRow';
import { ChallengeMembersSkeleton } from '@/components/atoms/ChallengeMembersSkeleton';
import authHooks from '@/hooks/useAuth';
import challengesHooks from '@/hooks/useChallenges';

interface ChallengeDetailMembersProps {
    challengeId: string;
}

const SECTION_LABEL = 'Membros';
const ERROR_COPY = 'Falhou carregar os membros.';
const EMPTY_COPY = 'Ninguém no ranking ainda.';

export function ChallengeDetailMembers({ challengeId }: ChallengeDetailMembersProps) {
    const members = challengesHooks.useMembers(challengeId);
    const { me } = authHooks.use();
    const viewerId = me.data?.id ?? null;

    return (
        <section className="flex flex-col gap-3">
            <h2 className="font-mono text-[11px] uppercase tracking-[0.35em] text-sky-300">
                {SECTION_LABEL}
            </h2>
            {renderBody()}
        </section>
    );

    function renderBody() {
        if (members.isPending) {
            return <ChallengeMembersSkeleton />;
        }
        if (members.isError || !members.data) {
            return <p className="text-sm text-zinc-400">{ERROR_COPY}</p>;
        }
        if (members.data.length === 0) {
            return <p className="text-sm text-zinc-400">{EMPTY_COPY}</p>;
        }
        return (
            <ol className="flex flex-col gap-2">
                {members.data.map((member, index) => (
                    <ChallengeMemberRow
                        key={member.user.id}
                        member={member}
                        rank={index + 1}
                        isViewer={member.user.id === viewerId}
                    />
                ))}
            </ol>
        );
    }
}
