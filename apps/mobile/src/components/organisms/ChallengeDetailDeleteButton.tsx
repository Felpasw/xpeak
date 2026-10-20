'use client';

import { Trash2 } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { toast } from 'sonner';

import challengesHooks from '@/hooks/useChallenges';

interface ChallengeDetailDeleteButtonProps {
    challengeId: string;
}

const DELETE_LABEL = 'Deletar';
const DELETE_IN_PROGRESS = 'Deletando...';
const DELETE_CONFIRM = 'Deletar esse challenge? A ação não tem volta.';
const DELETED_TOAST = 'Challenge deletado.';
const DELETE_ERROR = 'Deu ruim deletando. Tenta de novo.';

export function ChallengeDetailDeleteButton({ challengeId }: ChallengeDetailDeleteButtonProps) {
    const router = useRouter();
    const mutation = challengesHooks.useDelete();

    const onDelete = async () => {
        if (!window.confirm(DELETE_CONFIRM)) {
            return;
        }
        try {
            await mutation.mutateAsync(challengeId);
            toast.success(DELETED_TOAST);
            router.replace('/challenges');
        } catch {
            toast.error(DELETE_ERROR);
        }
    };

    return (
        <button
            type="button"
            onClick={onDelete}
            disabled={mutation.isPending}
            className="mt-4 flex items-center justify-center gap-2 rounded-full border border-red-500/40 bg-red-500/10 px-4 py-3 text-sm font-semibold text-red-300 transition-colors hover:border-red-400/70 hover:text-red-200 disabled:opacity-60"
        >
            <Trash2 className="h-4 w-4" />
            {mutation.isPending ? DELETE_IN_PROGRESS : DELETE_LABEL}
        </button>
    );
}
