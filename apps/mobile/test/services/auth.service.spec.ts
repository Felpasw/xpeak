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

describe('authService', () => {
    it('login stores the Bearer token from the Authorization header', async () => {
        server.use(
            http.post('http://localhost:5000/auth/login', () =>
                HttpResponse.json(
                    { user: sampleUser },
                    { headers: { Authorization: 'Bearer login.jwt' } },
                ),
            ),
        );
        const { default: authService } = await import('@/services/auth.service');

        const result = await authService.login({
            identifier: 'felipe@x.com',
            password: 'hunter22!',
        });

        expect(result.token).toBe('login.jwt');
        expect(window.localStorage.getItem('xpeak.auth.token')).toBe('login.jwt');
    });

    it('logout clears the local token even if the request fails', async () => {
        window.localStorage.setItem('xpeak.auth.token', 'live.jwt');
        server.use(
            http.post('http://localhost:5000/auth/logout', () =>
                HttpResponse.json({}, { status: 500 }),
            ),
        );
        const { default: authService } = await import('@/services/auth.service');

        await expect(authService.logout()).rejects.toBeDefined();
        expect(window.localStorage.getItem('xpeak.auth.token')).toBeNull();
    });

    it('me returns the user on 200 and null on 401', async () => {
        const { default: authService } = await import('@/services/auth.service');

        server.use(
            http.get('http://localhost:5000/me', () => HttpResponse.json({ user: sampleUser })),
        );
        await expect(authService.me()).resolves.toEqual(sampleUser);

        server.use(
            http.get('http://localhost:5000/me', () => HttpResponse.json({}, { status: 401 })),
        );
        await expect(authService.me()).resolves.toBeNull();
    });
});
