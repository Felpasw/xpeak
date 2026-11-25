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

import { ReactionsStrip } from '@/components/organisms/ReactionsStrip';

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

describe('<ReactionsStrip />', () => {
    it('renders all 12 palette chips and shows the count only when > 0', () => {
        render(
            <ReactionsStrip
                checkInId="ci-1"
                reactions={{ counts: { '🔥': 3 }, mine: [] }}
            />,
            { wrapper: wrap() },
        );

        const chips = screen.getAllByRole('button').filter((b) =>
            (b.getAttribute('aria-label') ?? '').match(/^.+ \(\d+\)$/),
        );
        expect(chips).toHaveLength(12);

        expect(
            screen.getByRole('button', { name: /^🔥 \(3\)$/ }),
        ).toBeInTheDocument();
        // zero-count chips still render the emoji but no number
        expect(
            screen.getByRole('button', { name: /^👏 \(0\)$/ }),
        ).toBeInTheDocument();
    });

    it("highlights the caller's reactions via aria-pressed", () => {
        render(
            <ReactionsStrip
                checkInId="ci-1"
                reactions={{ counts: { '🔥': 1 }, mine: ['🔥'] }}
            />,
            { wrapper: wrap() },
        );

        const fire = screen.getByRole('button', { name: /^🔥 \(1\)$/ });
        expect(fire).toHaveAttribute('aria-pressed', 'true');

        const strong = screen.getByRole('button', { name: /^💪 \(0\)$/ });
        expect(strong).toHaveAttribute('aria-pressed', 'false');
    });

    it('POSTs the toggle when a chip is tapped', async () => {
        let bodyEmoji: string | null = null;
        server.use(
            http.post('http://localhost:5000/check_ins/ci-1/reactions', async ({ request }) => {
                const payload = (await request.json()) as { emoji: string };
                bodyEmoji = payload.emoji;
                return HttpResponse.json({ counts: { [payload.emoji]: 1 }, mine: [payload.emoji] });
            }),
        );

        const user = userEvent.setup();
        render(
            <ReactionsStrip
                checkInId="ci-1"
                reactions={{ counts: {}, mine: [] }}
            />,
            { wrapper: wrap() },
        );

        await user.click(screen.getByRole('button', { name: /^🔥 \(0\)$/ }));

        await waitFor(() => expect(bodyEmoji).toBe('🔥'));
    });

    it('hides "Ver quem reagiu" when there are no reactions', () => {
        render(
            <ReactionsStrip
                checkInId="ci-1"
                reactions={{ counts: {}, mine: [] }}
            />,
            { wrapper: wrap() },
        );

        expect(
            screen.queryByRole('button', { name: /reações/i }),
        ).not.toBeInTheDocument();
    });

    it('expands the panel and fetches the users on demand', async () => {
        let hit = 0;
        server.use(
            http.get('http://localhost:5000/check_ins/ci-1/reactions', ({ request }) => {
                const url = new URL(request.url);
                expect(url.searchParams.get('include')).toBe('users');
                hit += 1;
                return HttpResponse.json({
                    counts: { '🔥': 3 },
                    mine: [],
                    users: {
                        '🔥': [
                            { id: 'u1', username: 'alice', avatarUrl: null },
                            { id: 'u2', username: 'bob', avatarUrl: null },
                        ],
                    },
                });
            }),
        );

        const user = userEvent.setup();
        render(
            <ReactionsStrip
                checkInId="ci-1"
                reactions={{ counts: { '🔥': 3 }, mine: [] }}
            />,
            { wrapper: wrap() },
        );

        expect(hit).toBe(0);
        await user.click(screen.getByRole('button', { name: /reações/i }));

        await waitFor(() => expect(screen.getByTestId('who-reacted-panel')).toBeInTheDocument());
        expect(hit).toBe(1);
        expect(screen.getByText('@alice')).toBeInTheDocument();
        expect(screen.getByText('@bob')).toBeInTheDocument();

        // Overflow: count 3, listed 2 → "+1"
        expect(screen.getByTestId('who-reacted-🔥-overflow')).toHaveTextContent('+1');
    });
});
