import type { PropsWithChildren } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';

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

// Minimal IntersectionObserver mock — the organism attaches one to the
// infinite-scroll sentinel. We don't assert the trigger here; the spec
// covers states (skeleton/success/empty/error). Trigger behavior is
// smoke-tested manually in dev.
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
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

beforeEach(() => {
    window.localStorage.clear();
    window.localStorage.setItem('xpeak.auth.token', 'test.jwt');
});

const GLOBAL = '00000000-0000-0000-0000-000000000001';

const wrap = () => {
    const client = new QueryClient({
        defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    return function Wrapper({ children }: PropsWithChildren) {
        return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
    };
};

const row = (id: string, title: string) => ({
    id,
    categoryId: 'cat-1',
    groupId: GLOBAL,
    title,
    xpEarned: 10,
    scoringSnapshot: { baseXp: 10, multipliers: [], total: 10 },
    performedAt: '2026-10-04T12:00:00Z',
    durationMinutes: null,
    notes: null,
    hasMedia: false,
    category: { id: 'cat-1', slug: 'chest', name: 'Chest', iconPublicId: null },
    mediaPreview: null,
});

describe('<CheckinList />', () => {
    it('shows the skeleton on first load', async () => {
        server.use(
            http.get('http://localhost:5000/check_ins', async () => {
                await new Promise((resolve) => setTimeout(resolve, 50));
                return HttpResponse.json({ checkIns: [], nextCursor: null });
            }),
        );
        const { CheckinList } = await import('@/components/organisms/CheckinList');
        render(<CheckinList groupId={GLOBAL} />, { wrapper: wrap() });

        expect(screen.getByTestId('checkin-list-skeleton')).toBeInTheDocument();
    });

    it('renders a card per row on success', async () => {
        server.use(
            http.get('http://localhost:5000/check_ins', () =>
                HttpResponse.json({
                    checkIns: [row('a', 'Treino A'), row('b', 'Treino B')],
                    nextCursor: null,
                }),
            ),
        );
        const { CheckinList } = await import('@/components/organisms/CheckinList');
        render(<CheckinList groupId={GLOBAL} />, { wrapper: wrap() });

        await waitFor(() => expect(screen.getByText('Treino A')).toBeInTheDocument());
        expect(screen.getByText('Treino B')).toBeInTheDocument();
    });

    it('shows an empty state when there are no rows', async () => {
        server.use(
            http.get('http://localhost:5000/check_ins', () =>
                HttpResponse.json({ checkIns: [], nextCursor: null }),
            ),
        );
        const { CheckinList } = await import('@/components/organisms/CheckinList');
        render(<CheckinList groupId={GLOBAL} />, { wrapper: wrap() });

        await waitFor(() => expect(screen.getByTestId('checkin-list-empty')).toBeInTheDocument());
    });

    it('shows an inline retry on error and refetches when clicked', async () => {
        let callCount = 0;
        server.use(
            http.get('http://localhost:5000/check_ins', () => {
                callCount += 1;
                if (callCount === 1) {
                    return new HttpResponse(null, { status: 500 });
                }
                return HttpResponse.json({ checkIns: [row('a', 'Depois do retry')], nextCursor: null });
            }),
        );
        const { CheckinList } = await import('@/components/organisms/CheckinList');
        const user = userEvent.setup();
        render(<CheckinList groupId={GLOBAL} />, { wrapper: wrap() });

        const retry = await screen.findByRole('button', { name: /tentar de novo/i });
        await user.click(retry);

        await waitFor(() => expect(screen.getByText('Depois do retry')).toBeInTheDocument());
    });
});
