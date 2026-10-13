import type { PropsWithChildren } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';

const replaceMock = vi.fn();
vi.mock('next/navigation', () => ({
    useRouter: () => ({ replace: replaceMock, push: vi.fn() }),
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
    toast: { success: vi.fn(), error: vi.fn() },
}));

const server = setupServer();
beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => {
    server.resetHandlers();
    replaceMock.mockReset();
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

describe('<CreateChallengeForm />', () => {
    it('posts the expected payload and navigates to /challenges on success', async () => {
        let posted: Record<string, unknown> | null = null;
        server.use(
            http.post('http://localhost:5000/challenges', async ({ request }) => {
                posted = (await request.json()) as Record<string, unknown>;
                return HttpResponse.json(
                    {
                        challenge: {
                            id: 'ch-new',
                            ownerId: 'u-1',
                            title: posted['title'],
                            description: null,
                            iconPublicId: null,
                            visibility: posted['visibility'],
                            targetLevel: posted['targetLevel'],
                            xpMultiplier: posted['xpMultiplier'],
                            startsAt: posted['startsAt'],
                            endsAt: posted['endsAt'],
                            memberCap: null,
                            state: 'active',
                            createdAt: '2026-10-07T00:00:00Z',
                        },
                        ownerMembership: {
                            id: 'mem-1',
                            challengeId: 'ch-new',
                            userId: 'u-1',
                            role: 'owner',
                            joinedAt: '2026-10-07T00:00:00Z',
                            leftAt: null,
                            result: null,
                            progress: { value: 0, target: 1118, achievedAt: null },
                        },
                    },
                    { status: 201 },
                );
            }),
        );

        const { CreateChallengeForm } = await import(
            '@/components/organisms/CreateChallengeForm'
        );
        const user = userEvent.setup();
        render(<CreateChallengeForm />, { wrapper: wrap() });

        await user.type(screen.getByLabelText('Título'), 'Perna Épica');

        await user.click(screen.getByRole('button', { name: /criar challenge/i }));

        await waitFor(() => expect(posted).not.toBeNull());
        expect(posted!['title']).toBe('Perna Épica');
        expect(posted!['visibility']).toBe('private');
        expect(posted!['targetLevel']).toBe(5);
        expect(posted!['xpMultiplier']).toBe(1);
        await waitFor(() => expect(replaceMock).toHaveBeenCalledWith('/challenges'));
    });
});
