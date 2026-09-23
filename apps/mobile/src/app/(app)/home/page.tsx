'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';

import { LightningText } from '@/components/atoms/LightningText';
import authHooks from '@/hooks/useAuth';

const PROFILE_HREF = '/profile';

export default function HomePage() {
    const router = useRouter();
    const { me } = authHooks.use();
    const user = me.data;

    if (me.isPending) {
        return null;
    }

    if (!user) {
        router.replace('/login');
        return null;
    }

    return (
        <main className="flex flex-1 flex-col gap-10 px-6 py-10">
            <header className="flex items-center justify-between">
                <LightningText
                    emph="X"
                    rest="PEAK"
                    emphSize={72}
                    restSize={24}
                    width={140}
                    height={90}
                />
                <Link
                    href={PROFILE_HREF}
                    className="flex h-11 w-11 items-center justify-center rounded-full border border-zinc-800 bg-zinc-900/60 text-sm font-semibold text-zinc-100 transition-colors hover:border-sky-500/60 hover:text-sky-400"
                    aria-label="Profile"
                >
                    {user.username.slice(0, 2).toUpperCase()}
                </Link>
            </header>

            <section className="flex flex-col gap-2">
                <p className="font-mono text-[11px] uppercase tracking-[0.35em] text-white/50">
                    Welcome back
                </p>
                <h1 className="text-3xl font-semibold tracking-tight">
                    @{user.username}
                </h1>
                <p className="text-sm text-zinc-400">
                    Level {user.level} · {user.xp} XP · {user.currentStreakDays}d streak
                </p>
            </section>

            <section className="rounded-2xl border border-zinc-800 bg-zinc-900/40 p-6 text-center">
                <p className="text-sm text-zinc-400">
                    Your workout log lives here soon. Check-ins, streaks and titles are
                    on the way.
                </p>
            </section>
        </main>
    );
}
