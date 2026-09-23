import Link from 'next/link';

import { XpeakWordmark } from '@/components/atoms/XpeakWordmark';

const SUBTITLE = 'Join the climb';
const NOTE = 'Sign up is on the way. Log in with an existing account for now.';
const LOGIN_HREF = '/login';
const LOGIN_LABEL = 'Back to log in';

export const metadata = {
    title: 'Sign up · Xpeak',
};

export default function RegisterPage() {
    return (
        <main className="flex flex-1 flex-col items-center justify-center gap-8 px-6 py-12">
            <header className="flex flex-col items-center gap-4 text-center">
                <h1 aria-label="Xpeak" className="m-0">
                    <XpeakWordmark emphSize={160} restSize={52} />
                </h1>
                <p className="font-mono text-[11px] uppercase tracking-[0.35em] text-white/60">
                    {SUBTITLE}
                </p>
            </header>
            <p className="max-w-sm text-center text-sm text-zinc-400">{NOTE}</p>
            <Link
                href={LOGIN_HREF}
                className="text-xs uppercase tracking-[0.25em] text-white/60 underline-offset-4 transition-colors hover:text-white hover:underline"
            >
                {LOGIN_LABEL}
            </Link>
        </main>
    );
}
