import Link from 'next/link';

import { AnimatedBorderButton } from '@/components/atoms/AnimatedBorderButton';
import { LightningText } from '@/components/atoms/LightningText';

const LOGIN_HREF = '/login';
const REGISTER_HREF = '/register';
const SUBTITLE = 'Level up your training';
const LOGIN_LABEL = 'Sign in';
const REGISTER_LABEL = 'Sign up';

export default function Page() {
    return (
        <>
            <LightningText emph="X" rest="PEAK" />
            <main className="pointer-events-none fixed left-1/2 top-[calc(50%+130px)] z-10 flex -translate-x-1/2 flex-col items-center gap-6">
                <p className="whitespace-nowrap px-4 text-center font-mono text-[11px] uppercase tracking-[0.35em] text-white/60">
                    {SUBTITLE}
                </p>
                <div className="flex flex-col items-center gap-4">
                    <AnimatedBorderButton
                        href={LOGIN_HREF}
                        ariaLabel={LOGIN_LABEL}
                        className="pointer-events-auto"
                    >
                        {LOGIN_LABEL}
                    </AnimatedBorderButton>
                    <Link
                        href={REGISTER_HREF}
                        className="pointer-events-auto text-xs uppercase tracking-[0.25em] text-white/60 underline-offset-4 transition-colors hover:text-white hover:underline"
                    >
                        {REGISTER_LABEL}
                    </Link>
                </div>
            </main>
        </>
    );
}
