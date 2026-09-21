import { LightningText } from '@/components/atoms/LightningText';
import { LoginForm } from '@/components/organisms/LoginForm';

const SUBTITLE = 'Welcome back';

export const metadata = {
    title: 'Log in · Xpeak',
};

export default function LoginPage() {
    return (
        <>
            <LightningText emph="X" rest="PEAK" />
            <main className="pointer-events-none fixed left-1/2 top-[calc(50%+130px)] z-10 flex w-full max-w-sm -translate-x-1/2 flex-col items-center gap-6 px-6">
                <p className="font-mono text-[11px] uppercase tracking-[0.35em] text-white/60">
                    {SUBTITLE}
                </p>
                <div className="pointer-events-auto w-full">
                    <LoginForm />
                </div>
            </main>
        </>
    );
}
