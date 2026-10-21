import type { PropsWithChildren } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';

const replaceMock = vi.fn();
const searchParamsGet = vi.fn<(key: string) => string | null>(() => 'ch-1');

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

const server = setupServer();
beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => {
    server.resetHandlers();
    replaceMock.mockReset();
    searchParamsGet.mockReset();
    searchParamsGet.mockImplementation(() => 'ch-1');
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
    it('renders header, state badge, progress bar and share button for an owner', async () => {
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
                    shareLink: {
                        id: 'inv-1',
                        challengeId: 'ch-1',
                        invitedByUserId: 'u-1',
                        state: 'pending',
                        createdAt: '2026-10-07T00:00:00Z',
                    },
                    tiers: [],
                    currentTier: null,
                }),
            ),
            http.get('http://localhost:5000/challenges/ch-1/progress', () =>
                HttpResponse.json({
                    progress: { value: 420, target: 3162, achievedAt: null },
                }),
            ),
        );

        const { default: Page } = await import('@/app/(app)/challenges/view/page');
        render(<Page />, { wrapper: wrap() });

        await waitFor(() => expect(screen.getByText('Rumo ao level 10')).toBeInTheDocument());
        expect(screen.getByText('Ativo')).toBeInTheDocument();

        const bar = await screen.findByRole('progressbar');
        expect(bar.getAttribute('aria-valuenow')).toBe('420');
        expect(bar.getAttribute('aria-valuemax')).toBe('3162');

        expect(screen.getByRole('button', { name: /deletar/i })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: /copiar link/i })).toBeInTheDocument();
    });

    it('shows the not-found fallback when the id is missing from the url', async () => {
        searchParamsGet.mockImplementation(() => null);

        const { default: Page } = await import('@/app/(app)/challenges/view/page');
        render(<Page />, { wrapper: wrap() });

        expect(screen.getByTestId('challenge-detail-not-found')).toBeInTheDocument();
    });

    it('shows the not-found fallback when the API returns 404', async () => {
        server.use(
            http.get('http://localhost:5000/challenges/ch-1', () => new HttpResponse(null, { status: 404 })),
        );

        const { default: Page } = await import('@/app/(app)/challenges/view/page');
        render(<Page />, { wrapper: wrap() });

        await waitFor(() =>
            expect(screen.getByTestId('challenge-detail-not-found')).toBeInTheDocument(),
        );
    });

    it('renders the tier ladder for an owner with tiers and highlights the current one', async () => {
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
                        progress: { value: 30, target: 3162, achievedAt: null },
                    },
                    shareLink: {
                        id: 'inv-1',
                        challengeId: 'ch-1',
                        invitedByUserId: 'u-1',
                        state: 'pending',
                        createdAt: '2026-10-07T00:00:00Z',
                    },
                    tiers: [
                        { id: 't-1', minLevel: 10, title: 'Aprendiz' },
                        { id: 't-2', minLevel: 30, title: 'Mago' },
                        { id: 't-3', minLevel: 60, title: 'ArchMago' },
                    ],
                    currentTier: { id: 't-2', minLevel: 30, title: 'Mago' },
                }),
            ),
            http.get('http://localhost:5000/challenges/ch-1/progress', () =>
                HttpResponse.json({
                    progress: { value: 30, target: 3162, achievedAt: null },
                }),
            ),
        );

        const { default: Page } = await import('@/app/(app)/challenges/view/page');
        render(<Page />, { wrapper: wrap() });

        await waitFor(() => expect(screen.getByTestId('challenge-detail-tiers')).toBeInTheDocument());
        expect(screen.getByTestId('challenge-tier-0')).toHaveAttribute('data-status', 'reached');
        expect(screen.getByTestId('challenge-tier-1')).toHaveAttribute('data-status', 'current');
        expect(screen.getByTestId('challenge-tier-2')).toHaveAttribute('data-status', 'next');
    });

    it('renders the ladder for a member below the first tier with the first row as "Próximo"', async () => {
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
                        progress: { value: 5, target: 3162, achievedAt: null },
                    },
                    shareLink: null,
                    tiers: [
                        { id: 't-1', minLevel: 10, title: 'Aprendiz' },
                        { id: 't-2', minLevel: 30, title: 'Mago' },
                    ],
                    currentTier: null,
                }),
            ),
            http.get('http://localhost:5000/challenges/ch-1/progress', () =>
                HttpResponse.json({
                    progress: { value: 5, target: 3162, achievedAt: null },
                }),
            ),
        );

        const { default: Page } = await import('@/app/(app)/challenges/view/page');
        render(<Page />, { wrapper: wrap() });

        await waitFor(() => expect(screen.getByTestId('challenge-detail-tiers')).toBeInTheDocument());
        expect(screen.getByTestId('challenge-tier-0')).toHaveAttribute('data-status', 'next');
        expect(screen.getByTestId('challenge-tier-1')).toHaveAttribute('data-status', 'upcoming');
        expect(screen.queryByText('Atual')).not.toBeInTheDocument();
    });

    it('does not render the tiers section when the challenge has no tiers', async () => {
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
                        progress: { value: 0, target: 3162, achievedAt: null },
                    },
                    shareLink: {
                        id: 'inv-1',
                        challengeId: 'ch-1',
                        invitedByUserId: 'u-1',
                        state: 'pending',
                        createdAt: '2026-10-07T00:00:00Z',
                    },
                    tiers: [],
                    currentTier: null,
                }),
            ),
            http.get('http://localhost:5000/challenges/ch-1/progress', () =>
                HttpResponse.json({
                    progress: { value: 0, target: 3162, achievedAt: null },
                }),
            ),
        );

        const { default: Page } = await import('@/app/(app)/challenges/view/page');
        render(<Page />, { wrapper: wrap() });

        await waitFor(() => expect(screen.getByText('Rumo ao level 10')).toBeInTheDocument());
        expect(screen.queryByTestId('challenge-detail-tiers')).not.toBeInTheDocument();
    });

    it('hides the delete and share buttons for non-owner members', async () => {
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
                    shareLink: null,
                    tiers: [],
                    currentTier: null,
                }),
            ),
            http.get('http://localhost:5000/challenges/ch-1/progress', () =>
                HttpResponse.json({
                    progress: { value: 0, target: 3162, achievedAt: null },
                }),
            ),
        );

        const { default: Page } = await import('@/app/(app)/challenges/view/page');
        render(<Page />, { wrapper: wrap() });

        await waitFor(() => expect(screen.getByText('Rumo ao level 10')).toBeInTheDocument());
        expect(screen.queryByRole('button', { name: /deletar/i })).not.toBeInTheDocument();
        expect(screen.queryByRole('button', { name: /copiar link/i })).not.toBeInTheDocument();
    });
});
