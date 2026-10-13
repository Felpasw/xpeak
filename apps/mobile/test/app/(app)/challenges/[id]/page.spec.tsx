import type { PropsWithChildren } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';

const replaceMock = vi.fn();
vi.mock('next/navigation', () => ({
    useRouter: () => ({ replace: replaceMock, push: vi.fn() }),
    useParams: () => ({ id: 'ch-1' }),
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

const server = setupServer();
beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => {
    server.resetHandlers();
    replaceMock.mockReset();
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

const challenge = () => ({
    id: 'ch-1',
    ownerId: 'u-1',
    title: 'Rumo ao level 10',
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
});

describe('ChallengeDetailPage', () => {
    it('renders header, state badge and progress bar for an owner', async () => {
        server.use(
            http.get('http://localhost:5000/challenges/ch-1', () =>
                HttpResponse.json({
                    challenge: challenge(),
                    membership: {
                        id: 'mem-1',
                        challengeId: 'ch-1',
                        userId: 'u-1',
                        role: 'owner',
                        joinedAt: '2026-10-07T00:00:00Z',
                        leftAt: null,
                        result: null,
                        progress: { value: 420, target: 3162, achievedAt: null },
                    },
                }),
            ),
            http.get('http://localhost:5000/challenges/ch-1/progress', () =>
                HttpResponse.json({
                    progress: { value: 420, target: 3162, achievedAt: null },
                }),
            ),
        );

        const { default: Page } = await import('@/app/(app)/challenges/[id]/page');
        render(<Page />, { wrapper: wrap() });

        await waitFor(() => expect(screen.getByText('Rumo ao level 10')).toBeInTheDocument());
        expect(screen.getByText('Ativo')).toBeInTheDocument();

        const bar = await screen.findByRole('progressbar');
        expect(bar.getAttribute('aria-valuenow')).toBe('420');
        expect(bar.getAttribute('aria-valuemax')).toBe('3162');

        expect(screen.getByRole('button', { name: /deletar/i })).toBeInTheDocument();
    });

    it('shows the not-found fallback when the API returns 404', async () => {
        server.use(
            http.get('http://localhost:5000/challenges/ch-1', () => new HttpResponse(null, { status: 404 })),
        );

        const { default: Page } = await import('@/app/(app)/challenges/[id]/page');
        render(<Page />, { wrapper: wrap() });

        await waitFor(() =>
            expect(screen.getByTestId('challenge-detail-not-found')).toBeInTheDocument(),
        );
    });

    it('hides the delete button for non-owner members', async () => {
        server.use(
            http.get('http://localhost:5000/challenges/ch-1', () =>
                HttpResponse.json({
                    challenge: challenge(),
                    membership: {
                        id: 'mem-2',
                        challengeId: 'ch-1',
                        userId: 'u-2',
                        role: 'member',
                        joinedAt: '2026-10-07T00:00:00Z',
                        leftAt: null,
                        result: null,
                        progress: { value: 0, target: 3162, achievedAt: null },
                    },
                }),
            ),
            http.get('http://localhost:5000/challenges/ch-1/progress', () =>
                HttpResponse.json({
                    progress: { value: 0, target: 3162, achievedAt: null },
                }),
            ),
        );

        const { default: Page } = await import('@/app/(app)/challenges/[id]/page');
        render(<Page />, { wrapper: wrap() });

        await waitFor(() => expect(screen.getByText('Rumo ao level 10')).toBeInTheDocument());
        expect(screen.queryByRole('button', { name: /deletar/i })).not.toBeInTheDocument();
    });
});
