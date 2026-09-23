import type { Metadata, Viewport } from 'next';
import { Geist, Geist_Mono } from 'next/font/google';

import { Providers } from '@/components/atoms/Providers';
import { StarsBackground } from '@/components/atoms/StarsBackground';
import './globals.css';

const geistSans = Geist({
    variable: '--font-geist-sans',
    subsets: ['latin'],
});

const geistMono = Geist_Mono({
    variable: '--font-geist-mono',
    subsets: ['latin'],
});

const PORTFOLIO_URL = 'https://felipeclacerda.com';
const CREDIT_PREFIX = 'Powered by ';
const CREDIT_HANDLE = 'felpasw';

export const metadata: Metadata = {
    title: 'XPeak',
    description: 'Gym app with an RPG progression layer.',
};

export const viewport: Viewport = {
    width: 'device-width',
    initialScale: 1,
    viewportFit: 'cover',
    themeColor: '#0a0a0a',
};

export default function RootLayout({ children }: LayoutProps<'/'>) {
    return (
        <html
            lang="en"
            className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
        >
            <body
                className="min-h-full flex flex-col bg-zinc-950 text-zinc-100"
                style={{
                    paddingTop: 'env(safe-area-inset-top)',
                    paddingBottom: 'env(safe-area-inset-bottom)',
                    paddingLeft: 'env(safe-area-inset-left)',
                    paddingRight: 'env(safe-area-inset-right)',
                }}
            >
                <Providers>
                    <StarsBackground
                        className="flex flex-1 flex-col"
                        starColor="#38bdf8"
                    >
                        {children}
                    </StarsBackground>
                    <footer className="pointer-events-none fixed bottom-6 left-0 right-0 z-10 text-center text-sm text-zinc-400">
                        <span>{CREDIT_PREFIX}</span>
                        <a
                            href={PORTFOLIO_URL}
                            target="_blank"
                            rel="noreferrer"
                            className="pointer-events-auto font-bold text-zinc-100 underline-offset-4 hover:text-white hover:underline"
                        >
                            {CREDIT_HANDLE}
                        </a>
                    </footer>
                </Providers>
            </body>
        </html>
    );
}
