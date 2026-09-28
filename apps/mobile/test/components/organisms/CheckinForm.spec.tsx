import type { PropsWithChildren } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
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
    window.localStorage.setItem('xpeak.auth.token', 'test.jwt');
});

const GLOBAL = '00000000-0000-0000-0000-000000000001';

const categoriesResponse = {
    categories: [
        {
            id: 'cat-legs',
            groupId: GLOBAL,
            slug: 'legs',
            name: 'Legs',
            iconPublicId: null,
            active: true,
        },
        {
            id: 'cat-chest',
            groupId: GLOBAL,
            slug: 'chest',
            name: 'Chest',
            iconPublicId: null,
            active: true,
        },
    ],
};

const renderForm = async () => {
    const client = new QueryClient({
        defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    const Wrapper = ({ children }: PropsWithChildren) => (
        <QueryClientProvider client={client}>{children}</QueryClientProvider>
    );
    const { CheckinForm } = await import('@/components/organisms/CheckinForm');
    render(<CheckinForm />, { wrapper: Wrapper });
};

describe('<CheckinForm />', () => {
    it('blocks submit until a category is picked (zod schema)', async () => {
        const user = userEvent.setup();
        server.use(
            http.get(`http://localhost:5000/groups/${GLOBAL}/categories`, () =>
                HttpResponse.json(categoriesResponse),
            ),
        );

        await renderForm();
        await waitFor(() => expect(screen.getByRole('radio', { name: 'Legs' })).toBeInTheDocument());

        await user.click(screen.getByRole('button', { name: /registrar check-in/i }));

        await waitFor(() =>
            expect(screen.getByRole('alert')).toHaveTextContent(/escolha uma categoria/i),
        );
    });

    it('submits with the selected category, trimmed notes and parsed duration', async () => {
        const user = userEvent.setup();
        let receivedBody: unknown = null;

        server.use(
            http.get(`http://localhost:5000/groups/${GLOBAL}/categories`, () =>
                HttpResponse.json(categoriesResponse),
            ),
            http.post('http://localhost:5000/check_ins', async ({ request }) => {
                receivedBody = await request.json();
                return HttpResponse.json(
                    {
                        checkIn: {
                            id: 'ci',
                            categoryId: 'cat-chest',
                            groupId: GLOBAL,
                            xpEarned: 14,
                            scoringSnapshot: { baseXp: 12, multipliers: [], total: 14 },
                            performedAt: '2026-09-25T12:00:00Z',
                            durationMinutes: 45,
                            notes: 'hard set',
                        },
                        user: { id: 'u', xp: 14, level: 0, leveledUp: false, levelsGained: 0 },
                        streak: { current: 1, longest: 1, unit: 'day' },
                    },
                    { status: 201 },
                );
            }),
        );

        await renderForm();
        await waitFor(() => expect(screen.getByRole('radio', { name: 'Chest' })).toBeInTheDocument());

        await user.click(screen.getByRole('radio', { name: 'Chest' }));
        await user.type(screen.getByLabelText(/dura(ç|c)ão/i), '45');
        await user.type(screen.getByPlaceholderText(/como foi/i), '  hard set  ');
        await user.click(screen.getByRole('button', { name: /registrar check-in/i }));

        await waitFor(() => expect(routerReplace).toHaveBeenCalledWith('/profile'));
        expect(receivedBody).toEqual({
            categoryId: 'cat-chest',
            durationMinutes: 45,
            notes: 'hard set',
        });
    });

    it('renders the empty state when the group has no categories', async () => {
        server.use(
            http.get(`http://localhost:5000/groups/${GLOBAL}/categories`, () =>
                HttpResponse.json({ categories: [] }),
            ),
        );

        await renderForm();

        await waitFor(() =>
            expect(screen.getByText(/n(ã|a)o h(á|a) categorias/i)).toBeInTheDocument(),
        );
    });

    it('shows the level-up overlay when the response reports leveled_up', async () => {
        const user = userEvent.setup();

        server.use(
            http.get(`http://localhost:5000/groups/${GLOBAL}/categories`, () =>
                HttpResponse.json(categoriesResponse),
            ),
            http.post('http://localhost:5000/check_ins', () =>
                HttpResponse.json(
                    {
                        checkIn: {
                            id: 'ci',
                            categoryId: 'cat-legs',
                            groupId: GLOBAL,
                            xpEarned: 100,
                            scoringSnapshot: { baseXp: 100, multipliers: [], total: 100 },
                            performedAt: '2026-09-25T12:00:00Z',
                            durationMinutes: null,
                            notes: null,
                        },
                        user: { id: 'u', xp: 100, level: 1, leveledUp: true, levelsGained: 1 },
                        streak: { current: 1, longest: 1, unit: 'day' },
                    },
                    { status: 201 },
                ),
            ),
        );

        await renderForm();
        await waitFor(() => expect(screen.getByRole('radio', { name: 'Legs' })).toBeInTheDocument());

        await user.click(screen.getByRole('radio', { name: 'Legs' }));
        await user.click(screen.getByRole('button', { name: /registrar check-in/i }));

        await waitFor(() =>
            expect(screen.getByRole('dialog', { name: /subiu de n(í|i)vel/i })).toBeInTheDocument(),
        );
        expect(routerReplace).not.toHaveBeenCalled();

        await user.click(screen.getByRole('dialog', { name: /subiu de n(í|i)vel/i }));
        await waitFor(() => expect(routerReplace).toHaveBeenCalledWith('/profile'));
    });
});
