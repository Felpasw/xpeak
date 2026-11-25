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

import { CommentsSection } from '@/components/organisms/CommentsSection';

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

beforeAll(() => {
    class IO {
        observe() { /* noop */ }
        unobserve() { /* noop */ }
        disconnect() { /* noop */ }
        takeRecords() { return []; }
    }
    (globalThis as unknown as { IntersectionObserver: typeof IO }).IntersectionObserver = IO;
});

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

const comment = (id: string, authorId: string, username: string, body: string) => ({
    id,
    checkInId: 'ci-1',
    author: { id: authorId, username, avatarUrl: null },
    body,
    createdAt: '2026-10-04T12:00:00Z',
});

describe('<CommentsSection />', () => {
    it('shows the skeleton on first load', async () => {
        server.use(
            http.get('http://localhost:5000/check_ins/ci-1/comments', async () => {
                await new Promise((resolve) => setTimeout(resolve, 50));
                return HttpResponse.json({ items: [], nextCursor: null });
            }),
        );

        render(
            <CommentsSection
                checkInId="ci-1"
                currentUserId="u1"
                checkInOwnerId="u1"
            />,
            { wrapper: wrap() },
        );

        expect(screen.getByTestId('comments-skeleton')).toBeInTheDocument();
    });

    it('renders the empty state when there are no comments', async () => {
        server.use(
            http.get('http://localhost:5000/check_ins/ci-1/comments', () =>
                HttpResponse.json({ items: [], nextCursor: null }),
            ),
        );

        render(
            <CommentsSection
                checkInId="ci-1"
                currentUserId="u1"
                checkInOwnerId="u1"
            />,
            { wrapper: wrap() },
        );

        await waitFor(() =>
            expect(screen.getByTestId('comments-empty')).toBeInTheDocument(),
        );
    });

    it('renders every comment and the count badge', async () => {
        server.use(
            http.get('http://localhost:5000/check_ins/ci-1/comments', () =>
                HttpResponse.json({
                    items: [
                        comment('c1', 'u1', 'alice', 'gg'),
                        comment('c2', 'u2', 'bob', '+1'),
                    ],
                    nextCursor: null,
                }),
            ),
        );

        render(
            <CommentsSection
                checkInId="ci-1"
                currentUserId="u1"
                checkInOwnerId="u1"
            />,
            { wrapper: wrap() },
        );

        await waitFor(() =>
            expect(screen.getByText('gg')).toBeInTheDocument(),
        );
        expect(screen.getByText('+1')).toBeInTheDocument();
        expect(screen.getByTestId('comments-count')).toHaveTextContent('2');
    });

    it('shows the delete affordance for the comment author and for the check-in owner', async () => {
        server.use(
            http.get('http://localhost:5000/check_ins/ci-1/comments', () =>
                HttpResponse.json({
                    items: [
                        comment('c_mine', 'u_me', 'me', 'author speaking'),
                        comment('c_other', 'u_other', 'other', 'third party'),
                    ],
                    nextCursor: null,
                }),
            ),
        );

        // Case 1: caller is the author of c_mine but NOT the check-in owner.
        const { unmount } = render(
            <CommentsSection
                checkInId="ci-1"
                currentUserId="u_me"
                checkInOwnerId="u_owner"
            />,
            { wrapper: wrap() },
        );

        await waitFor(() =>
            expect(screen.getByText('author speaking')).toBeInTheDocument(),
        );

        const mineRow = screen.getByTestId('comment-c_mine');
        const otherRow = screen.getByTestId('comment-c_other');
        expect(mineRow.querySelector('button[aria-label*="Apagar"]')).not.toBeNull();
        expect(otherRow.querySelector('button[aria-label*="Apagar"]')).toBeNull();

        unmount();

        // Case 2: caller is the check-in owner — can delete every comment.
        render(
            <CommentsSection
                checkInId="ci-1"
                currentUserId="u_owner"
                checkInOwnerId="u_owner"
            />,
            { wrapper: wrap() },
        );

        await waitFor(() =>
            expect(screen.getByText('author speaking')).toBeInTheDocument(),
        );

        expect(
            screen.getByTestId('comment-c_mine').querySelector('button[aria-label*="Apagar"]'),
        ).not.toBeNull();
        expect(
            screen.getByTestId('comment-c_other').querySelector('button[aria-label*="Apagar"]'),
        ).not.toBeNull();
    });

    it('POSTs the comment and refetches the list on submit', async () => {
        let postCount = 0;
        let getCount = 0;
        const added = comment('c_new', 'u_me', 'me', 'novo comentário');

        server.use(
            http.get('http://localhost:5000/check_ins/ci-1/comments', () => {
                getCount += 1;
                return HttpResponse.json({
                    items: postCount === 0 ? [] : [added],
                    nextCursor: null,
                });
            }),
            http.post('http://localhost:5000/check_ins/ci-1/comments', async ({ request }) => {
                const body = (await request.json()) as { body: string };
                postCount += 1;
                return HttpResponse.json(
                    { ...added, body: body.body },
                    { status: 201 },
                );
            }),
        );

        const user = userEvent.setup();
        render(
            <CommentsSection
                checkInId="ci-1"
                currentUserId="u_me"
                checkInOwnerId="u_me"
            />,
            { wrapper: wrap() },
        );

        await waitFor(() =>
            expect(screen.getByTestId('comments-empty')).toBeInTheDocument(),
        );

        const initialGetCount = getCount;
        await user.type(
            screen.getByPlaceholderText(/deixa um comentário/i),
            'novo comentário',
        );
        await user.click(screen.getByRole('button', { name: /enviar/i }));

        await waitFor(() =>
            expect(screen.getByText('novo comentário')).toBeInTheDocument(),
        );
        expect(postCount).toBe(1);
        expect(getCount).toBeGreaterThan(initialGetCount);
    });

    it('disables the send button when the draft is empty or whitespace', async () => {
        server.use(
            http.get('http://localhost:5000/check_ins/ci-1/comments', () =>
                HttpResponse.json({ items: [], nextCursor: null }),
            ),
        );

        const user = userEvent.setup();
        render(
            <CommentsSection
                checkInId="ci-1"
                currentUserId="u_me"
                checkInOwnerId="u_me"
            />,
            { wrapper: wrap() },
        );

        const send = screen.getByRole('button', { name: /enviar/i });
        expect(send).toBeDisabled();

        await user.type(
            screen.getByPlaceholderText(/deixa um comentário/i),
            '   ',
        );
        expect(send).toBeDisabled();
    });

    it('caps typing at 280 chars', async () => {
        server.use(
            http.get('http://localhost:5000/check_ins/ci-1/comments', () =>
                HttpResponse.json({ items: [], nextCursor: null }),
            ),
        );

        const user = userEvent.setup();
        render(
            <CommentsSection
                checkInId="ci-1"
                currentUserId="u_me"
                checkInOwnerId="u_me"
            />,
            { wrapper: wrap() },
        );

        const textarea = screen.getByPlaceholderText(
            /deixa um comentário/i,
        ) as HTMLTextAreaElement;

        await user.type(textarea, 'a'.repeat(300));

        expect(textarea.value).toHaveLength(280);
        expect(screen.getByTestId('comments-counter')).toHaveTextContent('280/280');
    });
});
