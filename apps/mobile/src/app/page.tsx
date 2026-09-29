import Link from 'next/link';

import { AnimatedBorderButton } from '@/components/atoms/AnimatedBorderButton';
import { LightningText } from '@/components/atoms/LightningText';

const LOGIN_HREF = '/login';
const REGISTER_HREF = '/register';
const SUBTITLE = 'Level up your training';
const LOGIN_LABEL = 'Sign in';
const REGISTER_LABEL = 'Sign up';

const EMPH_SIZE = 180;
const REST_SIZE = 64;
const CANVAS_WIDTH = 320;
const CANVAS_HEIGHT = 280;

export default function Page() {
    return (
        <main className="flex min-h-screen flex-col items-center justify-center gap-6 px-6">
            <LightningText
                emph="X"
                rest="PEAK"
                emphSize={EMPH_SIZE}
                restSize={REST_SIZE}
                width={CANVAS_WIDTH}
                height={CANVAS_HEIGHT}
            />
            <p className="whitespace-nowrap text-center font-mono text-[11px] uppercase tracking-[0.35em] text-white/60">
                {SUBTITLE}
            </p>
            <div className="flex flex-col items-center gap-4">
                <AnimatedBorderButton href={LOGIN_HREF} ariaLabel={LOGIN_LABEL}>
                    {LOGIN_LABEL}
                </AnimatedBorderButton>
                <Link
                    href={REGISTER_HREF}
                    className="text-xs uppercase tracking-[0.25em] text-white/60 underline-offset-4 transition-colors hover:text-white hover:underline"
                >
                    {REGISTER_LABEL}
                </Link>
            </div>
        </main>
    );
}
