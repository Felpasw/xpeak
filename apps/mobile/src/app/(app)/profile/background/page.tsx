'use client';

import type { Backgrounds } from '@xpeak/shared';
import { useRouter } from 'next/navigation';
import { useState } from 'react';

import { listBackgrounds } from '@/components/backgrounds/registry';
import authHooks from '@/hooks/useAuth';

export default function BackgroundPickerPage() {
    const router = useRouter();
    const { me, patchBackground } = authHooks.use();
    const current = me.data?.background ?? null;
    const [override, setOverride] = useState<Backgrounds | null>(null);
    const selection = override ?? current;
    const canSave = Boolean(
        override && override !== current && !patchBackground.isPending,
    );

    const handleSave = () => {
        if (!canSave || !override) {
            return;
        }
        patchBackground.mutate(override, {
            onSuccess: () => router.push('/profile'),
        });
    };

    return (
        <main className="flex flex-1 flex-col px-6 pt-12 pb-32">
            <h1 className="text-2xl font-bold tracking-tight text-zinc-100">
                Background
            </h1>
            <p className="mt-2 text-sm text-zinc-400">
                Escolha o cenário que acompanha seu perfil.
            </p>

            <ul
                className="mt-8 grid grid-cols-2 gap-4"
                role="radiogroup"
                aria-label="Backgrounds"
            >
                {listBackgrounds().map(({ slug, name, Component }) => {
                    const isSelected = selection === slug;
                    const isCurrent = current === slug;
                    return (
                        <li key={slug}>
                            <button
                                type="button"
                                role="radio"
                                aria-checked={isSelected}
                                aria-label={name}
                                data-selected={isSelected}
                                onClick={() => setOverride(slug)}
                                className="relative flex aspect-[3/4] w-full flex-col overflow-hidden rounded-2xl border border-zinc-800 bg-zinc-900 text-left transition data-[selected=true]:border-sky-400 data-[selected=true]:ring-2 data-[selected=true]:ring-sky-400/40"
                            >
                                <div className="relative h-full w-full">
                                    <Component className="pointer-events-none absolute inset-0" />
                                </div>
                                <div className="absolute bottom-0 left-0 right-0 flex items-center justify-between bg-zinc-950/80 px-3 py-2 text-sm backdrop-blur">
                                    <span className="font-medium text-zinc-100">
                                        {name}
                                    </span>
                                    {isCurrent ? (
                                        <span className="text-xs uppercase tracking-wide text-sky-400">
                                            Atual
                                        </span>
                                    ) : null}
                                </div>
                            </button>
                        </li>
                    );
                })}
            </ul>

            <div className="mt-8 flex gap-3">
                <button
                    type="button"
                    onClick={() => router.push('/profile')}
                    className="flex-1 rounded-xl border border-zinc-700 bg-zinc-900/60 py-3 text-sm font-medium text-zinc-200 transition hover:bg-zinc-800"
                >
                    Cancelar
                </button>
                <button
                    type="button"
                    disabled={!canSave}
                    onClick={handleSave}
                    className="flex-1 rounded-xl bg-sky-500 py-3 text-sm font-semibold text-zinc-950 transition hover:bg-sky-400 disabled:cursor-not-allowed disabled:bg-zinc-700 disabled:text-zinc-400"
                >
                    Salvar
                </button>
            </div>
        </main>
    );
}
