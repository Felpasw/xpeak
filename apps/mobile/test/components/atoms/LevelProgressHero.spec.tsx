import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { LevelProgressHero } from '@/components/atoms/LevelProgressHero';

const baseUser = {
    username: 'felipe',
    level: 3,
    xp: 1250,
    currentStreakDays: 7,
    levelProgress: { xpIntoLevel: 100, xpToNextLevel: 280, percent: 100 / 280 },
};

describe('<LevelProgressHero />', () => {
    it('renders the username, level and XP line', () => {
        render(<LevelProgressHero user={baseUser} />);

        expect(screen.getByText('@felipe')).toBeInTheDocument();
        expect(screen.getByText(/Level 3/i)).toBeInTheDocument();
        expect(screen.getByText(/1250 XP/i)).toBeInTheDocument();
    });

    it('renders the next-level copy with the remaining XP', () => {
        render(<LevelProgressHero user={baseUser} />);

        expect(
            screen.getByText(/faltam 180 XP pra Level 4/i),
        ).toBeInTheDocument();
    });

    it('sets the progress bar width to the percent value', () => {
        render(<LevelProgressHero user={baseUser} />);

        const bar = screen.getByTestId('level-progress-fill');
        expect(bar.style.width).toBe(`${(100 / 280) * 100}%`);
    });

    it('renders the streak chip with the fire glyph when streak > 0', () => {
        render(<LevelProgressHero user={baseUser} />);

        expect(screen.getByTestId('streak-chip')).toHaveTextContent('7d');
    });

    it('hides the streak chip when the streak is 0', () => {
        render(
            <LevelProgressHero
                user={{ ...baseUser, currentStreakDays: 0 }}
            />,
        );

        expect(screen.queryByTestId('streak-chip')).not.toBeInTheDocument();
    });

    it('renders a maxed bar and a "nivel máximo" copy at the ceiling', () => {
        render(
            <LevelProgressHero
                user={{
                    ...baseUser,
                    levelProgress: { xpIntoLevel: 0, xpToNextLevel: 0, percent: 1 },
                }}
            />,
        );

        expect(screen.getByText(/nível máximo/i)).toBeInTheDocument();
        expect(screen.getByTestId('level-progress-fill').style.width).toBe('100%');
    });
});
