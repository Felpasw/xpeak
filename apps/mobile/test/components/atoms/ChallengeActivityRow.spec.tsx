import type { PropsWithChildren } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import type { ChallengeCheckInFeedEntry } from '@xpeak/shared';
import { describe, expect, it } from 'vitest';

import { ChallengeActivityRow } from '@/components/atoms/ChallengeActivityRow';

const wrap = () => {
    const client = new QueryClient({
        defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    return function Wrapper({ children }: PropsWithChildren) {
        return <QueryClientProvider client={client}>{children}</QueryClientProvider>;
    };
};

const entry = (
    overrides: Partial<ChallengeCheckInFeedEntry['checkIn']> = {},
    authorOverrides: Partial<ChallengeCheckInFeedEntry['author']> = {},
): ChallengeCheckInFeedEntry => ({
    checkIn: {
        id: 'ci-1',
        title: 'Treino de perna',
        xpEarned: 45,
        scoringSnapshot: {
            baseXp: 30,
            multipliers: [{ source: 'streak', value: 1.5 }],
            total: 45,
        },
        performedAt: '2026-10-04T12:00:00Z',
        durationMinutes: 55,
        notes: null,
        hasMedia: true,
        mediaPreview: { kind: 'photo', thumbUrl: 'https://cdn/x.jpg' },
        reactions: { counts: {}, mine: [] },
        commentsCount: 0,
        ...overrides,
    },
    author: {
        id: 'u-1',
        username: 'felipe',
        avatarUrl: null,
        level: 12,
        xp: 2500,
        currentStreakDays: 7,
        longestStreakDays: 21,
        background: null,
        createdAt: '2026-09-17T12:00:00Z',
        ...authorOverrides,
    },
});

describe('<ChallengeActivityRow />', () => {
    it('renders the header with username and level pill', () => {
        render(<ChallengeActivityRow entry={entry()} />, { wrapper: wrap() });

        expect(screen.getByText('@felipe')).toBeInTheDocument();
        expect(screen.getByText(/level 12/i)).toBeInTheDocument();
    });

    it('renders the hero media when mediaPreview is set', () => {
        render(<ChallengeActivityRow entry={entry()} />, { wrapper: wrap() });

        expect(screen.getByTestId('challenge-activity-media')).toBeInTheDocument();
        expect(
            screen.getByAltText(/mídia do check-in Treino de perna/i),
        ).toBeInTheDocument();
    });

    it('falls back to the placeholder when mediaPreview is null', () => {
        render(
            <ChallengeActivityRow
                entry={entry({ mediaPreview: null, hasMedia: false })}
            />,
            { wrapper: wrap() },
        );

        expect(
            screen.getByTestId('challenge-activity-media-placeholder'),
        ).toBeInTheDocument();
        expect(
            screen.queryByTestId('challenge-activity-media'),
        ).not.toBeInTheDocument();
    });

    it('renders the title + xp pill + multiplier breakdown', () => {
        render(<ChallengeActivityRow entry={entry()} />, { wrapper: wrap() });

        expect(screen.getByText('Treino de perna')).toBeInTheDocument();
        expect(screen.getByText('+45 XP')).toBeInTheDocument();

        const breakdown = screen.getByTestId('challenge-activity-breakdown');
        expect(breakdown).toHaveTextContent('30 base');
        expect(breakdown).toHaveTextContent('1.5×');
        expect(breakdown).toHaveTextContent('streak');
    });

    it('hides the breakdown when scoring has no multipliers', () => {
        render(
            <ChallengeActivityRow
                entry={entry({
                    scoringSnapshot: { baseXp: 10, multipliers: [], total: 10 },
                    xpEarned: 10,
                })}
            />,
            { wrapper: wrap() },
        );

        expect(
            screen.queryByTestId('challenge-activity-breakdown'),
        ).not.toBeInTheDocument();
    });
});
