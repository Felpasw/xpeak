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
    title: 'Treino de perna',
    description: null,
    iconPublicId: null,
    visibility: 'private',
    targetLevel: 10,
    xpMultiplier: 1.0,
    startsAt: '2026-10-07T00:00:00Z',
    endsAt: '2026-11-06T00:00:00Z',
    memberCap: null,
    state: 'active',
    categoryIds: ['cat-1'],
    createdAt: '2026-10-07T00:00:00Z',
    ...overrides,
});

describe('ChallengesPage', () => {
    it('renders the header, the "Novo" link and the active challenges by default', async () => {
        server.use(
            http.get('http://localhost:5000/challenges', ({ request }) => {
                expect(new URL(request.url).searchParams.get('state')).toBe('active');
                return HttpResponse.json({ challenges: [challenge()] });
            }),
        );

        const { default: ChallengesPage } = await import('@/app/(app)/challenges/page');
        render(<ChallengesPage />, { wrapper: wrap() });

        expect(screen.getByText('Challenges')).toBeInTheDocument();
        expect(screen.getByRole('link', { name: /criar novo challenge/i })).toHaveAttribute(
            'href',
            '/challenges/new',
        );

        await waitFor(() => expect(screen.getByText('Treino de perna')).toBeInTheDocument());
    });

    it('shows the empty state when there are no challenges', async () => {
        server.use(
            http.get('http://localhost:5000/challenges', () =>
                HttpResponse.json({ challenges: [] }),
            ),
        );

        const { default: ChallengesPage } = await import('@/app/(app)/challenges/page');
        render(<ChallengesPage />, { wrapper: wrap() });

        await waitFor(() =>
            expect(screen.getByTestId('challenge-list-empty')).toBeInTheDocument(),
        );
    });
});
