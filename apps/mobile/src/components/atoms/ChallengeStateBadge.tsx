import { ChallengeState } from '@xpeak/shared';

interface ChallengeStateBadgeProps {
    state: ChallengeState;
}

const STATE_LABEL: Record<ChallengeState, string> = {
    [ChallengeState.Active]: 'Ativo',
    [ChallengeState.Completed]: 'Completo',
    [ChallengeState.Failed]: 'Falhou',
};

const STATE_STYLES: Record<ChallengeState, string> = {
    [ChallengeState.Active]: 'border-sky-500/40 bg-sky-500/10 text-sky-300',
    [ChallengeState.Completed]: 'border-emerald-500/40 bg-emerald-500/10 text-emerald-300',
    [ChallengeState.Failed]: 'border-zinc-700 bg-zinc-800/60 text-zinc-400',
};

export function ChallengeStateBadge({ state }: ChallengeStateBadgeProps) {
    return (
        <span
            className={`shrink-0 self-start rounded-full border px-3 py-1 font-mono text-xs font-semibold ${STATE_STYLES[state]}`}
        >
            {STATE_LABEL[state]}
        </span>
    );
}
