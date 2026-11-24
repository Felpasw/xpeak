'use client';

import { Copy } from 'lucide-react';
import { toast } from 'sonner';

interface ChallengeDetailShareButtonProps {
    invitationId: string;
}

const COPY_LABEL = 'Copiar link';
const COPIED_TOAST = 'Link copiado.';
const COPY_ERROR = 'Deu ruim copiando. Copia manual aí.';

function buildShareUrl(invitationId: string): string {
    const origin = typeof window === 'undefined' ? '' : window.location.origin;
    return `${origin}/challenges/join?invitation=${invitationId}`;
}

export function ChallengeDetailShareButton({ invitationId }: ChallengeDetailShareButtonProps) {
    const url = buildShareUrl(invitationId);

    const onCopy = async () => {
        try {
            await navigator.clipboard.writeText(url);
            toast.success(COPIED_TOAST);
        } catch {
            toast.error(COPY_ERROR);
        }
    };

    return (
        <button
            type="button"
            onClick={onCopy}
            data-testid="challenge-share-url"
            data-share-url={url}
            className="flex w-full items-center justify-center gap-2 rounded-2xl border border-sky-500/40 bg-gradient-to-br from-sky-500/15 via-sky-500/10 to-transparent px-4 py-3 text-sm font-semibold text-sky-100 shadow-[0_0_24px_-10px_rgba(56,189,248,0.6)] transition-colors hover:border-sky-400/70 hover:bg-sky-500/25"
        >
            <Copy className="h-4 w-4" />
            {COPY_LABEL}
        </button>
    );
}
