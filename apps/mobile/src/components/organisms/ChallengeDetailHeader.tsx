import { formatDistanceToNow, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import type { Challenge } from '@xpeak/shared';

import { ChallengeStateBadge } from '@/components/atoms/ChallengeStateBadge';
import { PageHeader } from '@/components/atoms/PageHeader';

interface ChallengeDetailHeaderProps {
    challenge: Challenge;
}

export function ChallengeDetailHeader({ challenge }: ChallengeDetailHeaderProps) {
    const endsIn = formatDistanceToNow(parseISO(challenge.endsAt), {
        addSuffix: true,
        locale: ptBR,
    });
    return (
        <div className="flex flex-col gap-4">
            <PageHeader
                title={challenge.title}
                description={`Target level ${challenge.targetLevel} · termina ${endsIn}`}
            />
            <ChallengeStateBadge state={challenge.state} />
            {challenge.description && (
                <p className="text-sm text-zinc-400">{challenge.description}</p>
            )}
        </div>
    );
}
