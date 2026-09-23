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
});

describe('api', () => {
    it('injects Authorization: Bearer <token> when one is stored', async () => {
        window.localStorage.setItem('xpeak.auth.token', 'stored.jwt');
        const { default: api } = await import('@/api');

        let seenHeader: string | null = null;
        server.use(
            http.get('http://localhost:5000/me', ({ request }) => {
                seenHeader = request.headers.get('Authorization');
                return HttpResponse.json({ ok: true });
            }),
        );

        await api.get('/me');

        expect(seenHeader).toBe('Bearer stored.jwt');
    });

    it('omits the Authorization header when there is no token', async () => {
        const { default: api } = await import('@/api');

        let seenHeader: string | null = 'unset';
        server.use(
            http.get('http://localhost:5000/public', ({ request }) => {
                seenHeader = request.headers.get('Authorization');
                return HttpResponse.json({ ok: true });
            }),
        );

        await api.get('/public');

        expect(seenHeader).toBeNull();
    });

    it('clears the stored token and fires the unauthorized handler on 401', async () => {
        window.localStorage.setItem('xpeak.auth.token', 'expired.jwt');
        const { default: api, onUnauthorized } = await import('@/api');
        const handler = vi.fn();
        onUnauthorized(handler);

        server.use(
            http.get('http://localhost:5000/me', () => HttpResponse.json({}, { status: 401 })),
        );

        await expect(api.get('/me')).rejects.toMatchObject({ response: { status: 401 } });

        expect(window.localStorage.getItem('xpeak.auth.token')).toBeNull();
        expect(handler).toHaveBeenCalledOnce();

        onUnauthorized(null);
    });
});
