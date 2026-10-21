import { render, screen } from '@testing-library/react';
import type { ChallengeTier } from '@xpeak/shared';
import { describe, expect, it } from 'vitest';

import { ChallengeDetailTiers } from '@/components/organisms/ChallengeDetailTiers';

const tier = (overrides: Partial<ChallengeTier>): ChallengeTier => ({
    id: overrides.id ?? crypto.randomUUID(),
    minLevel: overrides.minLevel ?? 0,
    title: overrides.title ?? 'Tier',
});

const APRENDIZ = tier({ id: 't-1', minLevel: 10, title: 'Aprendiz' });
const MAGO = tier({ id: 't-2', minLevel: 30, title: 'Mago' });
const ARCHMAGO = tier({ id: 't-3', minLevel: 60, title: 'ArchMago' });
const LADDER = [APRENDIZ, MAGO, ARCHMAGO];

describe('<ChallengeDetailTiers />', () => {
    it('renders nothing when the tier list is empty', () => {
        const { container } = render(
            <ChallengeDetailTiers tiers={[]} currentTier={null} />,
        );

        expect(container.firstChild).toBeNull();
    });

    it('highlights the middle tier as current and marks the next one as "Próximo"', () => {
        render(<ChallengeDetailTiers tiers={LADDER} currentTier={MAGO} />);

        expect(screen.getByTestId('challenge-tier-0')).toHaveAttribute('data-status', 'reached');
        expect(screen.getByTestId('challenge-tier-1')).toHaveAttribute('data-status', 'current');
        expect(screen.getByTestId('challenge-tier-2')).toHaveAttribute('data-status', 'next');

        expect(screen.getByText('Atual')).toBeInTheDocument();
        expect(screen.getByText('Próximo')).toBeInTheDocument();
        expect(screen.getByText('Alcançado')).toBeInTheDocument();
    });

    it('marks the first tier as "Próximo" when there is no current tier yet', () => {
        render(<ChallengeDetailTiers tiers={LADDER} currentTier={null} />);

        expect(screen.getByTestId('challenge-tier-0')).toHaveAttribute('data-status', 'next');
        expect(screen.getByTestId('challenge-tier-1')).toHaveAttribute('data-status', 'upcoming');
        expect(screen.getByTestId('challenge-tier-2')).toHaveAttribute('data-status', 'upcoming');

        expect(screen.queryByText('Atual')).not.toBeInTheDocument();
    });

    it('marks the last tier as current when the member has passed every floor', () => {
        render(<ChallengeDetailTiers tiers={LADDER} currentTier={ARCHMAGO} />);

        expect(screen.getByTestId('challenge-tier-0')).toHaveAttribute('data-status', 'reached');
        expect(screen.getByTestId('challenge-tier-1')).toHaveAttribute('data-status', 'reached');
        expect(screen.getByTestId('challenge-tier-2')).toHaveAttribute('data-status', 'current');

        expect(screen.queryByText('Próximo')).not.toBeInTheDocument();
    });

    it('renders every tier title and level', () => {
        render(<ChallengeDetailTiers tiers={LADDER} currentTier={MAGO} />);

        expect(screen.getByText('Aprendiz')).toBeInTheDocument();
        expect(screen.getByText('Mago')).toBeInTheDocument();
        expect(screen.getByText('ArchMago')).toBeInTheDocument();
        expect(screen.getByText('Lv 10')).toBeInTheDocument();
        expect(screen.getByText('Lv 30')).toBeInTheDocument();
        expect(screen.getByText('Lv 60')).toBeInTheDocument();
    });
});
