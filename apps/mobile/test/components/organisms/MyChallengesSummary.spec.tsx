import type { PropsWithChildren } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
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

const summary = (id: string, title: string, value: number, target: number) => ({
    id,
    title,
    state: 'active',
    progress: { value, target, achievedAt: null },
});

describe('<MyChallengesSummary />', () => {
    it('shows a skeleton on the first load', async () => {
        server.use(
            http.get('http://localhost:5000/challenges/summary', async () => {
                await new Promise((resolve) => setTimeout(resolve, 50));
                return HttpResponse.json({ summaries: [] });
            }),
        );
        const { MyChallengesSummary } = await import(
            '@/components/organisms/MyChallengesSummary'
        );
        render(<MyChallengesSummary />, { wrapper: wrap() });

        expect(screen.getByTestId('my-challenges-skeleton')).toBeInTheDocument();
    });

    it('renders a row per active challenge with title and progress bar', async () => {
        server.use(
            http.get('http://localhost:5000/challenges/summary', () =>
                HttpResponse.json({
                    summaries: [
                        summary('a', 'Treino A', 50, 300),
                        summary('b', 'Treino B', 200, 300),
                    ],
                }),
            ),
        );
        const { MyChallengesSummary } = await import(
            '@/components/organisms/MyChallengesSummary'
        );
        render(<MyChallengesSummary />, { wrapper: wrap() });

        await waitFor(() => expect(screen.getByText('Treino A')).toBeInTheDocument());
        expect(screen.getByText('Treino B')).toBeInTheDocument();

        const bars = screen.getAllByRole('progressbar');
        expect(bars[0]).toHaveAttribute('aria-valuenow', '50');
        expect(bars[0]).toHaveAttribute('aria-valuemax', '300');
        expect(bars[1]).toHaveAttribute('aria-valuenow', '200');
    });

    it('renders nothing when the user has zero active challenges', async () => {
        server.use(
            http.get('http://localhost:5000/challenges/summary', () =>
                HttpResponse.json({ summaries: [] }),
            ),
        );
        const { MyChallengesSummary } = await import(
            '@/components/organisms/MyChallengesSummary'
        );
        const { container } = render(<MyChallengesSummary />, { wrapper: wrap() });

        await waitFor(() =>
            expect(screen.queryByTestId('my-challenges-skeleton')).not.toBeInTheDocument(),
        );
        expect(container).toBeEmptyDOMElement();
    });

    it('renders an inline retry on error', async () => {
        server.use(
            http.get('http://localhost:5000/challenges/summary', () =>
                new HttpResponse(null, { status: 500 }),
            ),
        );
        const { MyChallengesSummary } = await import(
            '@/components/organisms/MyChallengesSummary'
        );
        render(<MyChallengesSummary />, { wrapper: wrap() });

        await waitFor(() =>
            expect(
                screen.getByRole('button', { name: /tentar de novo/i }),
            ).toBeInTheDocument(),
        );
    });
});
