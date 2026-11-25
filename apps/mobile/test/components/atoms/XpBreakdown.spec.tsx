import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { XpBreakdown } from '@/components/atoms/XpBreakdown';

describe('<XpBreakdown />', () => {
    it('renders base, each multiplier row and the total pill', () => {
        render(
            <XpBreakdown
                scoring={{
                    baseXp: 10,
                    multipliers: [
                        { source: 'streak', value: 1.5 },
                        { source: 'categoria', value: 2 },
                    ],
                    total: 30,
                }}
                xpEarned={30}
            />,
        );

        expect(screen.getByText('Base')).toBeInTheDocument();
        expect(screen.getByText('10 XP')).toBeInTheDocument();
        expect(screen.getByText('streak')).toBeInTheDocument();
        expect(screen.getByText('1.5×')).toBeInTheDocument();
        expect(screen.getByText('categoria')).toBeInTheDocument();
        expect(screen.getByText('2×')).toBeInTheDocument();
        expect(screen.getByTestId('xp-breakdown-total')).toHaveTextContent('+30 XP');
    });

    it('hides the multiplier row when there are no multipliers', () => {
        render(
            <XpBreakdown
                scoring={{ baseXp: 10, multipliers: [], total: 10 }}
                xpEarned={10}
            />,
        );

        expect(screen.getByText('Base')).toBeInTheDocument();
        expect(
            screen.queryByTestId('xp-breakdown-multipliers'),
        ).not.toBeInTheDocument();
        expect(screen.getByTestId('xp-breakdown-total')).toHaveTextContent('+10 XP');
    });
});
