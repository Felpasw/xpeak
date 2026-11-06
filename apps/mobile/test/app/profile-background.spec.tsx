import type { PropsWithChildren } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
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

const routerPush = vi.fn();

vi.mock('next/navigation', () => ({
    useRouter: () => ({ push: routerPush, back: routerPush, replace: routerPush }),
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
    routerPush.mockReset();
});
afterAll(() => server.close());

const makeUser = (background: string | null) => ({
    id: '00000000-0000-0000-0000-000000000000',
    username: 'felipe',
    email: 'felipe@x.com',
    avatarUrl: null,
    level: 0,
    xp: 0,
    currentStreakDays: 0,
    longestStreakDays: 0,
    background,
    createdAt: '2026-09-17T12:00:00Z',
});

const renderPage = async (background: string | null) => {
    server.use(
        http.get('http://localhost:5000/me', () =>
            HttpResponse.json({ user: makeUser(background) }),
        ),
    );
    const client = new QueryClient({
        defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    const { default: Page } = await import('@/app/(app)/profile/background/page');
    const Wrapper = ({ children }: PropsWithChildren) => (
        <QueryClientProvider client={client}>{children}</QueryClientProvider>
    );
    render(<Page />, { wrapper: Wrapper });
};

beforeEach(() => {
    window.localStorage.clear();
});

describe('ProfileBackgroundPage', () => {
    it('marks the current background and saves a new pick', async () => {
        const user = userEvent.setup();
        const seen: Array<{ background: string }> = [];
        server.use(
            http.patch('http://localhost:5000/me/background', async ({ request }) => {
                seen.push((await request.json()) as { background: string });
                return HttpResponse.json({ user: makeUser('lightning') });
            }),
        );

        await renderPage('stars');
        await screen.findByText(/Atual/);

        expect(screen.getByRole('radio', { name: /Estrelas/ })).toHaveAttribute(
            'aria-checked',
            'true',
        );

        await user.click(screen.getByRole('radio', { name: /Raios/ }));
        await user.click(screen.getByRole('button', { name: /Salvar/ }));

        await vi.waitFor(() =>
            expect(seen).toEqual([{ background: 'lightning' }]),
        );
        await vi.waitFor(() => expect(routerPush).toHaveBeenCalledWith('/profile'));
    });

    it('disables Save until a different option is picked', async () => {
        await renderPage('lightning');
        await screen.findByRole('radio', { name: /Raios/ });

        expect(screen.getByRole('button', { name: /Salvar/ })).toBeDisabled();
    });

    it('cancel navigates back to profile without hitting the API', async () => {
        const user = userEvent.setup();
        const seen = vi.fn();
        server.use(
            http.patch('http://localhost:5000/me/background', () => {
                seen();
                return HttpResponse.json({});
            }),
        );

        await renderPage('stars');
        await screen.findByRole('radio', { name: /Estrelas/ });

        await user.click(screen.getByRole('button', { name: /Cancelar/ }));

        expect(routerPush).toHaveBeenCalledWith('/profile');
        expect(seen).not.toHaveBeenCalled();
    });
});
