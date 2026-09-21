'use client';

import { useRouter } from 'next/navigation';

import { Button } from '@/components/atoms/Button';
import authHooks from '@/hooks/useAuth';

export default function ProfilePage() {
    const router = useRouter();
    const { me, logout } = authHooks.use();
    const user = me.data;

    if (!user) {
        return null;
    }

    const initials = user.username.slice(0, 2).toUpperCase();

    const onLogout = async () => {
        try {
            await logout.mutateAsync();
        } finally {
            router.replace('/login');
        }
    };

    return (
        <main className="flex flex-1 flex-col gap-8 px-6 py-10">
            <header className="flex items-center gap-4">
                {user.avatarUrl ? (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img
                        src={user.avatarUrl}
                        alt={`${user.username} avatar`}
                        className="h-16 w-16 rounded-full object-cover"
                    />
                ) : (
                    <div
                        aria-label="Default avatar"
                        className="flex h-16 w-16 items-center justify-center rounded-full bg-zinc-800 text-lg font-semibold"
                    >
                        {initials}
                    </div>
                )}
                <div className="flex flex-col">
                    <span className="text-lg font-semibold">@{user.username}</span>
                    <span className="text-sm text-zinc-400">{user.email}</span>
                </div>
            </header>

            <section className="grid grid-cols-2 gap-4">
                <Stat label="Level" value={user.level} />
                <Stat label="XP" value={user.xp} />
                <Stat label="Current streak" value={user.currentStreakDays} suffix="d" />
                <Stat label="Longest streak" value={user.longestStreakDays} suffix="d" />
            </section>

            <Button
                variant="secondary"
                onClick={onLogout}
                disabled={logout.isPending}
                className="mt-auto w-full"
            >
                {logout.isPending ? 'Logging out…' : 'Log out'}
            </Button>
        </main>
    );
}

function Stat({ label, value, suffix }: { label: string; value: number; suffix?: string }) {
    return (
        <div className="rounded-md border border-zinc-800 bg-zinc-900/60 px-4 py-3">
            <p className="text-xs uppercase tracking-wide text-zinc-400">{label}</p>
            <p className="mt-1 text-2xl font-semibold">
                {value}
                {suffix ? <span className="ml-1 text-sm text-zinc-500">{suffix}</span> : null}
            </p>
        </div>
    );
}
