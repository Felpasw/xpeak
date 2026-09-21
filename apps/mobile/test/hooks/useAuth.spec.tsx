import type { PropsWithChildren } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
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

const wrap = () => {
    const client = new QueryClient({
        defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    return function Wrapper({ children }: PropsWithChildren) {
        return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
    };
};

describe('authHooks.use()', () => {
    it('me query returns the user when the server responds 200', async () => {
        server.use(
            http.get('http://localhost:5000/me', () => HttpResponse.json({ user: sampleUser })),
        );
        const { default: authHooks } = await import('@/hooks/useAuth');
        const { result } = renderHook(() => authHooks.use(), { wrapper: wrap() });

        await waitFor(() => expect(result.current.me.isSuccess).toBe(true));
        expect(result.current.me.data).toEqual(sampleUser);
    });

    it('me query resolves to null on 401', async () => {
        server.use(
            http.get('http://localhost:5000/me', () => HttpResponse.json({}, { status: 401 })),
        );
        const { default: authHooks } = await import('@/hooks/useAuth');
        const { result } = renderHook(() => authHooks.use(), { wrapper: wrap() });

        await waitFor(() => expect(result.current.me.isSuccess).toBe(true));
        expect(result.current.me.data).toBeNull();
    });

    it('login mutation stores the token and warms the me cache', async () => {
        server.use(
            http.get('http://localhost:5000/me', () => HttpResponse.json({}, { status: 401 })),
            http.post('http://localhost:5000/auth/login', () =>
                HttpResponse.json(
                    { user: sampleUser },
                    { headers: { Authorization: 'Bearer login.jwt' } },
                ),
            ),
        );
        const { default: authHooks } = await import('@/hooks/useAuth');
        const wrapper = wrap();
        const { result } = renderHook(() => authHooks.use(), { wrapper });

        await result.current.login.mutateAsync({
            identifier: 'felipe@x.com',
            password: 'hunter22!',
        });

        await waitFor(() => expect(result.current.me.data).toEqual(sampleUser));
        expect(window.localStorage.getItem('xpeak.auth.token')).toBe('login.jwt');
    });

    it('logout mutation clears the token and empties the me cache', async () => {
        window.localStorage.setItem('xpeak.auth.token', 'live.jwt');
        server.use(
            http.get('http://localhost:5000/me', () => HttpResponse.json({ user: sampleUser })),
            http.post('http://localhost:5000/auth/logout', () => new HttpResponse(null, { status: 204 })),
        );
        const { default: authHooks } = await import('@/hooks/useAuth');
        const { result } = renderHook(() => authHooks.use(), { wrapper: wrap() });

        await waitFor(() => expect(result.current.me.data).toEqual(sampleUser));
        await result.current.logout.mutateAsync();

        expect(window.localStorage.getItem('xpeak.auth.token')).toBeNull();
        await waitFor(() => expect(result.current.me.data).toBeNull());
    });
});
