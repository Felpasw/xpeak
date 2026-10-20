'use client';

import { Copy } from 'lucide-react';
import { toast } from 'sonner';

interface ChallengeDetailShareButtonProps {
    invitationId: string;
}

const COPY_LABEL = 'Copiar link';
const COPIED_TOAST = 'Link copiado.';
const COPY_ERROR = 'Deu ruim copiando. Copia manual aí.';
const SECTION_LABEL = 'Convite';

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
        <div className="mt-2 flex flex-col gap-3 rounded-2xl border border-zinc-800 bg-zinc-900/40 p-4">
            <p className="text-xs uppercase tracking-widest text-zinc-500">{SECTION_LABEL}</p>
            <code
                data-testid="challenge-share-url"
                className="truncate rounded-lg bg-zinc-950/60 px-3 py-2 text-xs text-zinc-300"
            >
                {url}
            </code>
            <button
                type="button"
                onClick={onCopy}
                className="flex items-center justify-center gap-2 rounded-full border border-zinc-700 bg-zinc-900/60 px-4 py-2.5 text-sm font-semibold text-zinc-100 transition-colors hover:border-zinc-500"
            >
                <Copy className="h-4 w-4" />
                {COPY_LABEL}
            </button>
        </div>
    );
}
