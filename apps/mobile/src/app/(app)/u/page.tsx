'use client';

import { Suspense } from 'react';

import { PublicProfileView } from '@/components/organisms/PublicProfileView';

export default function PublicProfilePage() {
    return (
        <Suspense
            fallback={
                <main className="flex flex-1 flex-col items-center justify-center text-sm text-zinc-400">
                    Carregando…
                </main>
            }
        >
            <PublicProfileView />
        </Suspense>
    );
}
