import type { PropsWithChildren } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
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

vi.mock('next/navigation', () => ({
    useRouter: () => ({ push: vi.fn(), back: vi.fn(), replace: vi.fn() }),
    useSearchParams: () => new URLSearchParams('username=felipe'),
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
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

beforeEach(() => {
    window.localStorage.clear();
});

const renderPage = async () => {
    const client = new QueryClient({
        defaultOptions: { queries: { retry: false } },
    });
    const { default: Page } = await import('@/app/(app)/u/page');
    const Wrapper = ({ children }: PropsWithChildren) => (
        <QueryClientProvider client={client}>{children}</QueryClientProvider>
    );
    render(<Page />, { wrapper: Wrapper });
};

describe('PublicProfilePage', () => {
    it('renders the owner background and public card when the user exists', async () => {
        server.use(
            http.get('http://localhost:5000/users/felipe', () =>
                HttpResponse.json({
                    id: '00000000-0000-0000-0000-000000000000',
                    username: 'felipe',
                    avatarUrl: null,
                    level: 7,
                    xp: 1420,
                    background: 'etched_accretion',
                    createdAt: '2026-09-17T12:00:00Z',
                }),
            ),
        );

        await renderPage();

        await screen.findByText('@felipe');
        expect(screen.getByText(/Level 7/)).toBeInTheDocument();
        expect(screen.getByText(/1420/)).toBeInTheDocument();
        expect(screen.getByTestId('profile-background')).toHaveAttribute(
            'data-slug',
            'etched_accretion',
        );
    });

    it('shows a not-found state when the user does not exist', async () => {
        server.use(
            http.get('http://localhost:5000/users/felipe', () =>
                HttpResponse.json({}, { status: 404 }),
            ),
        );

        await renderPage();

        await screen.findByText(/não encontrado/i);
    });
});
