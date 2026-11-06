'use client';

import { BarChart3, Flame, Trophy } from 'lucide-react';
import { useSearchParams } from 'next/navigation';
import { useState } from 'react';

import { DiscreteTabs } from '@/components/atoms/DiscreteTabs';
import type { DiscreteTab } from '@/components/atoms/DiscreteTabs';
import { ProfileBackground } from '@/components/atoms/ProfileBackground';
import usersHooks from '@/hooks/useUsers';

enum ProfileTab {
    Stats = 'stats',
    Streaks = 'streaks',
    Titles = 'titles',
}

const TABS: DiscreteTab[] = [
    { id: ProfileTab.Stats, title: 'Stats', Icon: BarChart3 },
    { id: ProfileTab.Streaks, title: 'Streaks', Icon: Flame },
    { id: ProfileTab.Titles, title: 'Titles', Icon: Trophy },
];

const EMPTY_TITLES = 'No titles unlocked yet — keep training to earn them.';

export function PublicProfileView() {
    const params = useSearchParams();
    const username = params?.get('username') ?? null;
    const { data: user, isLoading } = usersHooks.useOne(username);
    const [activeTab, setActiveTab] = useState<ProfileTab>(ProfileTab.Stats);

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
            className="relative flex flex-1 flex-col overflow-hidden pt-12 pb-48"
            data-testid="profile-background"
            data-slug={user.background ?? ''}
        >
            <ProfileBackground background={user.background} />

            <div className="relative z-10 flex flex-col items-center px-6">
                {user.avatarUrl ? (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img
                        src={user.avatarUrl}
                        alt={`${user.username} avatar`}
                        className="h-32 w-32 rounded-full border-4 border-zinc-950 object-cover shadow-2xl shadow-sky-500/40"
                    />
                ) : (
                    <div
                        aria-label="Default avatar"
                        className="flex h-32 w-32 items-center justify-center rounded-full border-4 border-zinc-950 bg-gradient-to-br from-zinc-800 to-zinc-900 text-3xl font-bold text-sky-100 shadow-2xl shadow-sky-500/40"
                    >
                        {initials}
                    </div>
                )}

                <div className="mt-4 flex flex-col items-center gap-1 text-center">
                    <h1 className="text-2xl font-bold tracking-tight text-zinc-100">
                        @{user.username}
                    </h1>
                </div>

                <DiscreteTabs
                    tabs={TABS}
                    activeId={activeTab}
                    onChange={(id) => setActiveTab(id as ProfileTab)}
                    className="mt-8"
                />

                <section className="mt-6 w-full max-w-md">
                    {activeTab === ProfileTab.Stats && (
                        <div className="grid grid-cols-2 gap-3">
                            <Stat label="Level" value={user.level} />
                            <Stat label="XP" value={user.xp} />
                        </div>
                    )}

                    {activeTab === ProfileTab.Streaks && (
                        <div className="grid grid-cols-2 gap-3">
                            <Stat
                                label="Current streak"
                                value={user.currentStreakDays}
                                suffix="d"
                            />
                            <Stat
                                label="Longest streak"
                                value={user.longestStreakDays}
                                suffix="d"
                            />
                        </div>
                    )}

                    {activeTab === ProfileTab.Titles && (
                        <div className="rounded-2xl border border-zinc-800 bg-zinc-900/60 px-4 py-8 text-center text-sm text-zinc-400 backdrop-blur">
                            {EMPTY_TITLES}
                        </div>
                    )}
                </section>
            </div>
        </main>
    );
}

function Stat({ label, value, suffix }: { label: string; value: number; suffix?: string }) {
    return (
        <div className="rounded-2xl border border-zinc-800 bg-zinc-900/60 px-4 py-3 backdrop-blur">
            <p className="text-xs uppercase tracking-[0.15em] text-zinc-400">{label}</p>
            <p className="mt-1 text-2xl font-semibold text-sky-100">
                {value}
                {suffix ? <span className="ml-1 text-sm text-zinc-500">{suffix}</span> : null}
            </p>
        </div>
    );
}
