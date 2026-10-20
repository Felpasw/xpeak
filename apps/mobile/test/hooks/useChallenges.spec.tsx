import type { PropsWithChildren } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { ChallengeState, ChallengeVisibility } from '@xpeak/shared';

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

const wrap = () => {
    const client = new QueryClient({
        defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    return function Wrapper({ children }: PropsWithChildren) {
        return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
    };
};

const challenge = (overrides: Partial<Record<string, unknown>> = {}) => ({
    id: 'ch-1',
    ownerId: 'u-1',
    title: 'Challenge A',
    description: null,
    iconPublicId: null,
    visibility: 'private',
    targetLevel: 10,
    xpMultiplier: 1.0,
    startsAt: '2026-10-07T00:00:00Z',
    endsAt: '2026-11-06T00:00:00Z',
    memberCap: null,
    state: 'active',
    createdAt: '2026-10-07T00:00:00Z',
    ...overrides,
});

describe('challengesHooks.useList', () => {
    it('fetches challenges and unwraps the array', async () => {
        server.use(
            http.get('http://localhost:5000/challenges', ({ request }) => {
                expect(new URL(request.url).searchParams.get('state')).toBeNull();
                return HttpResponse.json({ challenges: [challenge()] });
            }),
        );

        const { default: challengesHooks } = await import('@/hooks/useChallenges');
        const { result } = renderHook(() => challengesHooks.useList(), { wrapper: wrap() });

        await waitFor(() => expect(result.current.isSuccess).toBe(true));
        expect(result.current.data).toHaveLength(1);
        expect(result.current.data?.[0]?.title).toBe('Challenge A');
    });

    it('forwards state filter to the backend', async () => {
        let seen: string | null = null;
        server.use(
            http.get('http://localhost:5000/challenges', ({ request }) => {
                seen = new URL(request.url).searchParams.get('state');
                return HttpResponse.json({ challenges: [] });
            }),
        );

        const { default: challengesHooks } = await import('@/hooks/useChallenges');
        const { result } = renderHook(
            () => challengesHooks.useList({ state: ChallengeState.Completed }),
            { wrapper: wrap() },
        );

        await waitFor(() => expect(result.current.isSuccess).toBe(true));
        expect(seen).toBe('completed');
    });
});

describe('challengesHooks.useDetail', () => {
    it('fetches a challenge detail by id', async () => {
        server.use(
            http.get('http://localhost:5000/challenges/ch-9', () =>
                HttpResponse.json({
                    challenge: challenge({ id: 'ch-9', title: 'Detail View' }),
                    membership: null,
                    shareLink: null,
                }),
            ),
        );

        const { default: challengesHooks } = await import('@/hooks/useChallenges');
        const { result } = renderHook(() => challengesHooks.useDetail('ch-9'), { wrapper: wrap() });

        await waitFor(() => expect(result.current.isSuccess).toBe(true));
        expect(result.current.data?.challenge.title).toBe('Detail View');
    });
});

describe('challengesHooks.useProgress', () => {
    it('fetches progress for a challenge', async () => {
        server.use(
            http.get('http://localhost:5000/challenges/ch-9/progress', () =>
                HttpResponse.json({
                    progress: { value: 420, target: 1000, achievedAt: null },
                }),
            ),
        );

        const { default: challengesHooks } = await import('@/hooks/useChallenges');
        const { result } = renderHook(() => challengesHooks.useProgress('ch-9'), {
            wrapper: wrap(),
        });

        await waitFor(() => expect(result.current.isSuccess).toBe(true));
        expect(result.current.data?.progress.value).toBe(420);
    });
});

describe('challengesHooks.useCreate', () => {
    it('posts the payload and invalidates the list prefix', async () => {
        let listFetchCount = 0;
        server.use(
            http.get('http://localhost:5000/challenges', () => {
                listFetchCount += 1;
                return HttpResponse.json({ challenges: [] });
            }),
            http.post('http://localhost:5000/challenges', () =>
                HttpResponse.json(
                    {
                        challenge: challenge({ id: 'created' }),
                        ownerMembership: {
                            id: 'mem-1',
                            challengeId: 'created',
                            userId: 'u-1',
                            role: 'owner',
                            joinedAt: '2026-10-07T00:00:00Z',
                            leftAt: null,
                            result: null,
                            progress: { value: 0, target: 3162, achievedAt: null },
                        },
                        shareLink: {
                            id: 'inv-1',
                            challengeId: 'created',
                            invitedByUserId: 'u-1',
                            state: 'pending',
                            createdAt: '2026-10-07T00:00:00Z',
                        },
                    },
                    { status: 201 },
                ),
            ),
        );

        const Wrapper = wrap();
        const { default: challengesHooks } = await import('@/hooks/useChallenges');

        const { result: listResult } = renderHook(() => challengesHooks.useList(), {
            wrapper: Wrapper,
        });
        await waitFor(() => expect(listResult.current.isSuccess).toBe(true));
        expect(listFetchCount).toBe(1);

        const { result: mutation } = renderHook(() => challengesHooks.useCreate(), {
            wrapper: Wrapper,
        });
        await mutation.current.mutateAsync({
            title: 'New',
            visibility: ChallengeVisibility.Private,
            targetLevel: 5,
            xpMultiplier: 1.0,
            startsAt: '2026-10-07T00:00:00Z',
            endsAt: '2026-11-06T00:00:00Z',
        });

        await waitFor(() => expect(listFetchCount).toBeGreaterThan(1));
    });
});

describe('challengesHooks.useDelete', () => {
    it('deletes and invalidates the list prefix', async () => {
        let listFetchCount = 0;
        server.use(
            http.get('http://localhost:5000/challenges', () => {
                listFetchCount += 1;
                return HttpResponse.json({ challenges: [] });
            }),
            http.delete('http://localhost:5000/challenges/ch-x', () => new HttpResponse(null, { status: 204 })),
        );

        const Wrapper = wrap();
        const { default: challengesHooks } = await import('@/hooks/useChallenges');

        const { result: listResult } = renderHook(() => challengesHooks.useList(), {
            wrapper: Wrapper,
        });
        await waitFor(() => expect(listResult.current.isSuccess).toBe(true));
        expect(listFetchCount).toBe(1);

        const { result: mutation } = renderHook(() => challengesHooks.useDelete(), {
            wrapper: Wrapper,
        });
        await mutation.current.mutateAsync('ch-x');

        await waitFor(() => expect(listFetchCount).toBeGreaterThan(1));
    });
});
