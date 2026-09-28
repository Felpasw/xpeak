import type { PropsWithChildren } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
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

describe('useCategories', () => {
    it('fetches categories for the given group', async () => {
        server.use(
            http.get(`http://localhost:5000/groups/${GLOBAL}/categories`, () =>
                HttpResponse.json({
                    categories: [
                        {
                            id: 'cat-1',
                            groupId: GLOBAL,
                            slug: 'chest',
                            name: 'Chest',
                            iconPublicId: null,
                            active: true,
                        },
                    ],
                }),
            ),
        );

        const { useCategories } = await import('@/hooks/useCheckIn');
        const { result } = renderHook(() => useCategories(GLOBAL), { wrapper: wrap() });

        await waitFor(() => expect(result.current.isSuccess).toBe(true));
        expect(result.current.data?.[0]?.slug).toBe('chest');
    });
});

describe('useCreateCheckIn', () => {
    it('invalidates the auth me query so the profile refetches', async () => {
        server.use(
            http.get('http://localhost:5000/me', () =>
                HttpResponse.json({
                    user: {
                        id: 'u',
                        username: 'felipe',
                        email: 'felipe@x.com',
                        avatarUrl: null,
                        level: 0,
                        xp: 0,
                        currentStreakDays: 0,
                        longestStreakDays: 0,
                        createdAt: '2026-01-01T00:00:00Z',
                    },
                }),
            ),
            http.post('http://localhost:5000/check_ins', () =>
                HttpResponse.json(
                    {
                        checkIn: {
                            id: 'ci',
                            categoryId: 'cat-1',
                            groupId: GLOBAL,
                            xpEarned: 10,
                            scoringSnapshot: { baseXp: 10, multipliers: [], total: 10 },
                            performedAt: '2026-09-25T12:00:00Z',
                            durationMinutes: null,
                            notes: null,
                        },
                        user: { id: 'u', xp: 10, level: 0, leveledUp: false, levelsGained: 0 },
                        streak: { current: 1, longest: 1, unit: 'day' },
                    },
                    { status: 201 },
                ),
            ),
        );

        const Wrapper = wrap();
        const authHooksModule = await import('@/hooks/useAuth');
        const { useCreateCheckIn } = await import('@/hooks/useCheckIn');

        // Prime the me query via authHooks; assert cache invalidation after the mutation.
        const { result: authResult } = renderHook(() => authHooksModule.default.use(), {
            wrapper: Wrapper,
        });
        await waitFor(() => expect(authResult.current.me.isSuccess).toBe(true));
        const meStateBefore = authResult.current.me.dataUpdatedAt;

        const { result: mutationResult } = renderHook(() => useCreateCheckIn(), {
            wrapper: Wrapper,
        });

        await mutationResult.current.mutateAsync({ categoryId: 'cat-1' });

        await waitFor(() =>
            expect(authResult.current.me.dataUpdatedAt).toBeGreaterThan(meStateBefore),
        );
    });
});
