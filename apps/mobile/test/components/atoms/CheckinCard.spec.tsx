import { render, screen } from '@testing-library/react';
import type { CheckIn } from '@xpeak/shared';
import { describe, expect, it } from 'vitest';

import { CheckinCard } from '@/components/atoms/CheckinCard';

const base: CheckIn = {
    id: 'ci-1',
    categoryId: 'cat-1',
    groupId: 'g-1',
    title: 'Treino de perna',
    xpEarned: 14,
    scoringSnapshot: { baseXp: 10, multipliers: [], total: 14 },
    performedAt: '2026-10-04T12:00:00Z',
    durationMinutes: 55,
    notes: 'Agachamento pesado',
    hasMedia: false,
    category: { id: 'cat-1', slug: 'perna', name: 'Perna', iconPublicId: null },
    mediaPreview: null,
};

describe('<CheckinCard />', () => {
    it('renders title, category name and the XP pill', () => {
        render(<CheckinCard checkIn={base} />);

        expect(screen.getByText('Treino de perna')).toBeInTheDocument();
        expect(screen.getByText(/Perna/)).toBeInTheDocument();
        expect(screen.getByText(/\+14 XP/)).toBeInTheDocument();
    });

    it('renders the thumbnail when mediaPreview is set', () => {
        const withMedia: CheckIn = {
            ...base,
            hasMedia: true,
            mediaPreview: {
                kind: 'photo',
                thumbUrl: 'https://fake.local/thumb/key?kind=photo',
            },
        };

        render(<CheckinCard checkIn={withMedia} />);

        const img = screen.getByAltText('Mídia do check-in Treino de perna');
        expect(img.getAttribute('src')).toContain('fake.local');
    });

    it('falls back to a placeholder icon when no media', () => {
        render(<CheckinCard checkIn={base} />);
        expect(screen.getByTestId('checkin-card-placeholder')).toBeInTheDocument();
    });

    it('renders truncated notes when present', () => {
        render(<CheckinCard checkIn={base} />);
        expect(screen.getByText('Agachamento pesado')).toBeInTheDocument();
    });

    it('omits the notes line when notes is null', () => {
        const noNotes: CheckIn = { ...base, notes: null };
        render(<CheckinCard checkIn={noNotes} />);
        expect(screen.queryByText('Agachamento pesado')).not.toBeInTheDocument();
    });
});
