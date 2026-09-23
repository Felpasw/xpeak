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
const toastSuccess = vi.fn();
const toastError = vi.fn();

vi.mock('next/navigation', () => ({
    useRouter: () => ({ replace: routerReplace, push: routerReplace }),
}));

vi.mock('sonner', () => ({
    toast: { success: toastSuccess, error: toastError },
    Toaster: () => null,
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
    toastSuccess.mockReset();
    toastError.mockReset();
});
afterAll(() => server.close());

beforeEach(() => {
    window.localStorage.clear();
    server.use(
        http.get('http://localhost:5000/me', () => HttpResponse.json({}, { status: 401 })),
    );
});

const wrap = () => {
    const client = new QueryClient({
        defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    return function Wrapper({ children }: PropsWithChildren) {
        return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
    };
};

const renderForm = async () => {
    const { LoginForm } = await import('@/components/organisms/LoginForm');
    render(<LoginForm />, { wrapper: wrap() });
};

const sampleUser = {
    id: '00000000-0000-0000-0000-000000000000',
    username: 'felipe',
    email: 'felipe@x.com',
    avatarUrl: null,
    level: 0,
    xp: 0,
    currentStreakDays: 0,
    longestStreakDays: 0,
    createdAt: '2026-09-17T12:00:00Z',
};

describe('<LoginForm />', () => {
    it('accepts email OR username in the identifier field', async () => {
        const user = userEvent.setup();
        const seen: unknown[] = [];
        server.use(
            http.post('http://localhost:5000/auth/login', async ({ request }) => {
                seen.push(await request.json());
                return HttpResponse.json(
                    { user: sampleUser },
                    { headers: { Authorization: 'Bearer login.jwt' } },
                );
            }),
        );

        await renderForm();
        await user.type(screen.getByLabelText(/email or username/i), 'felipe');
        await user.type(screen.getByLabelText('Password'), 'hunter22!');
        await user.click(screen.getByRole('button', { name: /^log in$/i }));

        await vi.waitFor(() => expect(routerReplace).toHaveBeenCalledWith('/home'));
        expect(seen[0]).toEqual({ identifier: 'felipe', password: 'hunter22!' });
    });

    it('shows an invalid-credentials toast on 401', async () => {
        const user = userEvent.setup();
        server.use(
            http.post('http://localhost:5000/auth/login', () =>
                HttpResponse.json({}, { status: 401 }),
            ),
        );

        await renderForm();
        await user.type(screen.getByLabelText(/email or username/i), 'felipe@x.com');
        await user.type(screen.getByLabelText('Password'), 'wrong-password');
        await user.click(screen.getByRole('button', { name: /^log in$/i }));

        await vi.waitFor(() =>
            expect(toastError).toHaveBeenCalledWith(expect.stringMatching(/invalid credentials/i)),
        );
        expect(routerReplace).not.toHaveBeenCalled();
    });

});
