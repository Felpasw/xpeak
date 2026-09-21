import { LoginForm } from '@/components/organisms/LoginForm';
import { XpeakWordmark } from '@/components/atoms/XpeakWordmark';

const SUBTITLE = 'Welcome back';

export const metadata = {
    title: 'Log in · Xpeak',
};

export default function LoginPage() {
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
            <LoginForm />
        </main>
    );
}
