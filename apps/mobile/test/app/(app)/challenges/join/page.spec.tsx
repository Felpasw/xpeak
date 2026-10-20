import type { PropsWithChildren } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';

const replaceMock = vi.fn();
const searchParamsGet = vi.fn<(key: string) => string | null>(() => 'inv-xyz');

vi.mock('next/navigation', () => ({
    useRouter: () => ({ replace: replaceMock, push: vi.fn() }),
    useSearchParams: () => ({ get: searchParamsGet }),
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

vi.mock('sonner', () => ({
    toast: { success: vi.fn(), error: vi.fn() },
}));

const server = setupServer();
beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => {
    server.resetHandlers();
    replaceMock.mockReset();
    searchParamsGet.mockReset();
    searchParamsGet.mockImplementation(() => 'inv-xyz');
});
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

describe('JoinChallengePage', () => {
    it('posts the invitation id and redirects to the detail on success', async () => {
        let posted: Record<string, unknown> | null = null;
        server.use(
            http.post('http://localhost:5000/challenges/join', async ({ request }) => {
                posted = (await request.json()) as Record<string, unknown>;
                return HttpResponse.json({
                    challenge: {
                        id: 'ch-42',
                        ownerId: 'u-9',
                        title: 'Convidado',
                        description: null,
                        iconPublicId: null,
                        visibility: 'private',
                        targetLevel: 5,
                        xpMultiplier: 1.0,
                        startsAt: '2026-10-07T00:00:00Z',
                        endsAt: '2026-11-06T00:00:00Z',
                        memberCap: null,
                        state: 'active',
                        createdAt: '2026-10-07T00:00:00Z',
                    },
                    membership: {
                        id: 'mem-99',
                        challengeId: 'ch-42',
                        userId: 'u-me',
                        role: 'member',
                        joinedAt: '2026-10-20T00:00:00Z',
                        leftAt: null,
                        result: null,
                        progress: { value: 0, target: 1118, achievedAt: null },
                    },
                });
            }),
        );

        const { default: Page } = await import('@/app/(app)/challenges/join/page');
        render(<Page />, { wrapper: wrap() });

        await waitFor(() => expect(posted).not.toBeNull());
        expect(posted!['invitationId']).toBe('inv-xyz');
        await waitFor(() =>
            expect(replaceMock).toHaveBeenCalledWith('/challenges/view?id=ch-42'),
        );
    });

    it('redirects to the challenges list when the invitation is missing', async () => {
        searchParamsGet.mockImplementation(() => null);

        const { default: Page } = await import('@/app/(app)/challenges/join/page');
        render(<Page />, { wrapper: wrap() });

        await waitFor(() => expect(replaceMock).toHaveBeenCalledWith('/challenges'));
    });

    it('redirects to the challenges list when the join call fails', async () => {
        server.use(
            http.post(
                'http://localhost:5000/challenges/join',
                () => new HttpResponse(null, { status: 409 }),
            ),
        );

        const { default: Page } = await import('@/app/(app)/challenges/join/page');
        render(<Page />, { wrapper: wrap() });

        await waitFor(() => expect(replaceMock).toHaveBeenCalledWith('/challenges'));
    });
});
