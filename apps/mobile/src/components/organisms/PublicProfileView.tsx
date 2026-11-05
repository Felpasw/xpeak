'use client';

import { useSearchParams } from 'next/navigation';

import { ProfileBackground } from '@/components/atoms/ProfileBackground';
import usersHooks from '@/hooks/useUsers';

export function PublicProfileView() {
    const params = useSearchParams();
    const username = params?.get('username') ?? null;
    const { data: user, isLoading } = usersHooks.useOne(username);

    if (!username) {
        return (
            <main className="flex flex-1 flex-col items-center justify-center text-sm text-zinc-400">
                Nenhum usuário informado.
            </main>
        );
    }

    if (isLoading) {
        return (
            <main className="flex flex-1 flex-col items-center justify-center text-sm text-zinc-400">
                Carregando…
            </main>
        );
    }

    if (!user) {
        return (
            <main className="flex flex-1 flex-col items-center justify-center gap-2 text-zinc-300">
                <p className="text-lg font-semibold">Usuário não encontrado</p>
                <p className="text-sm text-zinc-500">
                    O perfil que você procura não existe mais ou nunca existiu.
                </p>
            </main>
        );
    }

    const initials = user.username.slice(0, 2).toUpperCase();

    return (
        <main
            className="relative flex flex-1 flex-col overflow-hidden"
            data-testid="profile-background"
            data-slug={user.background ?? ''}
        >
            <ProfileBackground background={user.background} />

            <div className="relative z-10 flex flex-1 flex-col items-center px-6 pt-20 pb-32">
                {user.avatarUrl ? (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img
                        src={user.avatarUrl}
                        alt={`${user.username} avatar`}
                        className="h-32 w-32 rounded-full border-4 border-zinc-950/80 object-cover shadow-2xl shadow-black/60"
                    />
                ) : (
                    <div
                        aria-label="Default avatar"
                        className="flex h-32 w-32 items-center justify-center rounded-full border-4 border-zinc-950/80 bg-gradient-to-br from-zinc-800 to-zinc-900 text-3xl font-bold text-sky-100 shadow-2xl shadow-black/60"
                    >
                        {initials}
                    </div>
                )}

                <h1 className="mt-4 text-2xl font-bold tracking-tight text-zinc-100 drop-shadow-lg">
                    @{user.username}
                </h1>

                <div className="mt-6 grid grid-cols-2 gap-3 rounded-2xl border border-zinc-800/60 bg-zinc-950/70 px-4 py-3 backdrop-blur">
                    <Stat label={`Level ${user.level}`} value={user.level} />
                    <Stat label="XP" value={user.xp} />
                </div>
            </div>
        </main>
    );
}

function Stat({ label, value }: { label: string; value: number }) {
    return (
        <div className="min-w-24 text-center">
            <p className="text-xs uppercase tracking-[0.15em] text-zinc-400">
                {label}
            </p>
            <p className="mt-1 text-2xl font-semibold text-sky-100">{value}</p>
        </div>
    );
}
