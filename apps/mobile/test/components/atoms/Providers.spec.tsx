import { render } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';

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

vi.mock('sonner', () => ({
    Toaster: () => null,
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

describe('<Providers />', () => {
    it('wires the axios unauthorized handler to redirect to /login on 401', async () => {
        server.use(
            http.get('http://localhost:5000/me', () => HttpResponse.json({}, { status: 401 })),
        );

        const { Providers } = await import('@/components/atoms/Providers');
        const { default: api } = await import('@/api');

        render(<Providers>ready</Providers>);

        await expect(api.get('/me')).rejects.toMatchObject({ response: { status: 401 } });
        await vi.waitFor(() => expect(routerReplace).toHaveBeenCalledWith('/login'));
    });
});
