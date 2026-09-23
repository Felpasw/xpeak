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

const routerReplace = vi.fn();

vi.mock('next/navigation', () => ({
    useRouter: () => ({ replace: routerReplace, push: routerReplace }),
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
    routerReplace.mockReset();
});
afterAll(() => server.close());

beforeEach(() => {
    window.localStorage.clear();
});

const sampleUser = {
    id: '00000000-0000-0000-0000-000000000000',
    username: 'felipe',
    email: 'felipe@x.com',
    avatarUrl: null,
    level: 3,
    xp: 1250,
    currentStreakDays: 7,
    longestStreakDays: 21,
    createdAt: '2026-09-17T12:00:00Z',
};

const renderPage = async () => {
    server.use(
        http.get('http://localhost:5000/me', () => HttpResponse.json({ user: sampleUser })),
    );
    const client = new QueryClient({
        defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    client.setQueryData(['auth', 'me'], sampleUser);
    const Wrapper = ({ children }: PropsWithChildren) => (
        <QueryClientProvider client={client}>{children}</QueryClientProvider>
    );
    const { default: ProfilePage } = await import('@/app/(app)/profile/page');
    render(<ProfilePage />, { wrapper: Wrapper });
};

describe('ProfilePage', () => {
    it('renders @username, email, initials avatar and stats tab by default', async () => {
        await renderPage();

        expect(screen.getByText('@felipe')).toBeInTheDocument();
        expect(screen.getByText('felipe@x.com')).toBeInTheDocument();
        expect(screen.getByLabelText('Default avatar')).toHaveTextContent('FE');
        expect(screen.getByText(/^level$/i)).toBeInTheDocument();
        expect(screen.getByText('3')).toBeInTheDocument();
        expect(screen.getByText(/^xp$/i)).toBeInTheDocument();
        expect(screen.getByText('1250')).toBeInTheDocument();
    });

    it('reveals current and longest streaks when the streaks tab is selected', async () => {
        const user = userEvent.setup();
        await renderPage();

        await user.click(screen.getByRole('button', { name: /streaks/i }));

        expect(screen.getByText(/current streak/i)).toBeInTheDocument();
        expect(screen.getByText('7')).toBeInTheDocument();
        expect(screen.getByText(/longest streak/i)).toBeInTheDocument();
        expect(screen.getByText('21')).toBeInTheDocument();
    });
});
