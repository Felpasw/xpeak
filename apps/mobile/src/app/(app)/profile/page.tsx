'use client';

import { BarChart3, Flame, Trophy } from 'lucide-react';
import { useState } from 'react';

import { DiscreteTabs } from '@/components/atoms/DiscreteTabs';
import type { DiscreteTab } from '@/components/atoms/DiscreteTabs';
import authHooks from '@/hooks/useAuth';

const TABS: DiscreteTab[] = [
    { id: 'stats', title: 'Stats', Icon: BarChart3 },
    { id: 'streaks', title: 'Streaks', Icon: Flame },
    { id: 'titles', title: 'Titles', Icon: Trophy },
];

const EMPTY_TITLES = 'No titles unlocked yet — keep training to earn them.';

export default function ProfilePage() {
    const { me } = authHooks.use();
    const user = me.data;
    const [activeTab, setActiveTab] = useState('stats');

    if (!user) {
        return null;
    }

    const initials = user.username.slice(0, 2).toUpperCase();

    return (
        <main className="flex flex-1 flex-col pb-48">
            <div className="relative h-40 w-full overflow-hidden">
                <div className="absolute inset-0 bg-gradient-to-br from-sky-500 via-sky-800 to-zinc-950" />
                <div className="absolute inset-0 bg-[radial-gradient(circle_at_30%_20%,_rgba(125,211,252,0.45),_transparent_60%)]" />
                <div className="absolute inset-0 bg-[radial-gradient(circle_at_80%_100%,_rgba(2,132,199,0.5),_transparent_60%)]" />
            </div>

            <div className="relative -mt-16 flex flex-col items-center px-6">
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
                    <p className="text-sm text-zinc-400">{user.email}</p>
                </div>

                <DiscreteTabs
                    tabs={TABS}
                    activeId={activeTab}
                    onChange={setActiveTab}
                    className="mt-8"
                />

                <section className="mt-6 w-full max-w-md">
                    {activeTab === 'stats' ? (
                        <div className="grid grid-cols-2 gap-3">
                            <Stat label="Level" value={user.level} />
                            <Stat label="XP" value={user.xp} />
                        </div>
                    ) : null}

                    {activeTab === 'streaks' ? (
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
                    ) : null}

                    {activeTab === 'titles' ? (
                        <div className="rounded-2xl border border-zinc-800 bg-zinc-900/60 px-4 py-8 text-center text-sm text-zinc-400 backdrop-blur">
                            {EMPTY_TITLES}
                        </div>
                    ) : null}
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
