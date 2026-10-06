import type { PropsWithChildren } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
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

describe('GroupsPage', () => {
    it('renders the page header and lists the Global group check-ins', async () => {
        let seenGroupId: string | null = null;
        server.use(
            http.get('http://localhost:5000/check_ins', ({ request }) => {
                seenGroupId = new URL(request.url).searchParams.get('group_id');
                return HttpResponse.json({
                    checkIns: [
                        {
                            id: 'ci-1',
                            categoryId: 'cat-1',
                            groupId: GLOBAL,
                            title: 'Treino inicial',
                            xpEarned: 10,
                            scoringSnapshot: { baseXp: 10, multipliers: [], total: 10 },
                            performedAt: '2026-10-04T12:00:00Z',
                            durationMinutes: null,
                            notes: null,
                            hasMedia: false,
                            category: {
                                id: 'cat-1',
                                slug: 'chest',
                                name: 'Peito',
                                iconPublicId: null,
                            },
                            mediaPreview: null,
                        },
                    ],
                    nextCursor: null,
                });
            }),
        );

        const { default: GroupsPage } = await import('@/app/(app)/groups/page');
        render(<GroupsPage />, { wrapper: wrap() });

        await waitFor(() => expect(screen.getByText('Treino inicial')).toBeInTheDocument());
        expect(seenGroupId).toBe(GLOBAL);
        expect(screen.queryByText(/em breve/i)).not.toBeInTheDocument();
    });
});
