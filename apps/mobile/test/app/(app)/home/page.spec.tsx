import type { PropsWithChildren } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import {
    afterAll,
    afterEach,
    beforeAll,
    beforeEach,
    describe,
    expect,
    it,
    vi,
} from 'vitest';

const routerReplace = vi.fn();

vi.mock('next/navigation', () => ({
    useRouter: () => ({ replace: routerReplace, push: routerReplace }),
}));

vi.mock('@capacitor/core', () => ({
    Capacitor: { isNativePlatform: () => false },
}));
vi.mock('@capacitor/preferences', () => ({
    Preferences: {
        get: vi.fn(async () => ({ value: null })),
        set: vi.fn(async () => undefined),
        remove: vi.fn(async () => undefined),
    },
}));

beforeAll(() => {
    class IO {
        observe() { /* noop */ }
        unobserve() { /* noop */ }
        disconnect() { /* noop */ }
        takeRecords() { return []; }
    }
    (globalThis as unknown as { IntersectionObserver: typeof IO }).IntersectionObserver = IO;
});

const server = setupServer();
beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => {
    server.resetHandlers();
    routerReplace.mockReset();
});
afterAll(() => server.close());

beforeEach(() => {
    window.localStorage.clear();
    window.localStorage.setItem('xpeak.auth.token', 'test.jwt');
});

const sampleUser = {
    id: '00000000-0000-0000-0000-000000000000',
    username: 'felipe',
    email: 'felipe@x.com',
    avatarUrl: null,
    level: 3,
    xp: 1250,
    currentStreakDays: 7,
    longestStreakDays: 21,
    createdAt: '2026-09-17T12:00:00Z',
    levelProgress: { xpIntoLevel: 730, xpToNextLevel: 280, percent: 1 },
};

const row = (id: string, title: string) => ({
    id,
    title,
    xpEarned: 10,
    scoringSnapshot: { baseXp: 10, multipliers: [], total: 10 },
    performedAt: '2026-10-04T12:00:00Z',
    durationMinutes: null,
    notes: null,
    hasMedia: false,
    mediaPreview: null,
});

const renderPage = async () => {
    const client = new QueryClient({
        defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    client.setQueryData(['auth', 'me'], sampleUser);
    const Wrapper = ({ children }: PropsWithChildren) => (
        <QueryClientProvider client={client}>{children}</QueryClientProvider>
    );
    const { default: HomePage } = await import('@/app/(app)/home/page');
    render(<HomePage />, { wrapper: Wrapper });
};

describe('HomePage', () => {
    it('renders the welcome section with the level hero', async () => {
        server.use(
            http.get('http://localhost:5000/me', () =>
                HttpResponse.json({ user: sampleUser }),
            ),
            http.get('http://localhost:5000/check_ins', () =>
                HttpResponse.json({ checkIns: [], nextCursor: null }),
            ),
        );
        await renderPage();

        expect(screen.getByText('@felipe')).toBeInTheDocument();
        expect(screen.getByText(/Level 3/i)).toBeInTheDocument();
        expect(screen.getByText(/1250 XP/i)).toBeInTheDocument();
        expect(screen.getByTestId('streak-chip')).toHaveTextContent('7d');
    });

    it('renders a Check-in agora CTA linking to /checkin', async () => {
        server.use(
            http.get('http://localhost:5000/me', () =>
                HttpResponse.json({ user: sampleUser }),
            ),
            http.get('http://localhost:5000/check_ins', () =>
                HttpResponse.json({ checkIns: [], nextCursor: null }),
            ),
        );
        await renderPage();

        const cta = screen.getByRole('link', { name: /check-?in agora/i });
        expect(cta).toHaveAttribute('href', '/checkin');
    });

    it('renders the check-in list fed by GET /check_ins', async () => {
        server.use(
            http.get('http://localhost:5000/me', () =>
                HttpResponse.json({ user: sampleUser }),
            ),
            http.get('http://localhost:5000/check_ins', () =>
                HttpResponse.json({
                    checkIns: [row('a', 'Treino A'), row('b', 'Treino B')],
                    nextCursor: null,
                }),
            ),
        );
        await renderPage();

        await waitFor(() => expect(screen.getByText('Treino A')).toBeInTheDocument());
        expect(screen.getByText('Treino B')).toBeInTheDocument();
    });

    it('does not render the coming-soon placeholder anymore', async () => {
        server.use(
            http.get('http://localhost:5000/me', () =>
                HttpResponse.json({ user: sampleUser }),
            ),
            http.get('http://localhost:5000/check_ins', () =>
                HttpResponse.json({ checkIns: [], nextCursor: null }),
            ),
        );
        await renderPage();

        expect(
            screen.queryByText(/your workout log lives here soon/i),
        ).not.toBeInTheDocument();
    });
});
