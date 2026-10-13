'use client';

import { useRouter } from 'next/navigation';

const NOT_FOUND_COPY = 'Challenge não encontrado.';
const BACK_TO_LIST = 'Voltar';

export function ChallengeDetailNotFound() {
    const router = useRouter();
    return (
        <main className="flex flex-1 flex-col items-center justify-center gap-4 px-6 pt-10 pb-48 text-center">
            <p data-testid="challenge-detail-not-found" className="text-sm text-zinc-300">
                {NOT_FOUND_COPY}
            </p>
            <button
                type="button"
                onClick={() => router.replace('/challenges')}
                className="rounded-full border border-sky-500/40 bg-sky-500/10 px-4 py-2 text-xs font-semibold text-sky-300 transition-colors hover:border-sky-400/70 hover:text-sky-200"
            >
                {BACK_TO_LIST}
            </button>
        </main>
    );
}
