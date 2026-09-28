import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

import { LevelUpOverlay } from '@/components/atoms/LevelUpOverlay';

describe('<LevelUpOverlay />', () => {
    it('renders when open', () => {
        render(<LevelUpOverlay open level={3} onDismiss={() => {}} />);

        expect(screen.getByRole('dialog', { name: /subiu de n(í|i)vel/i })).toBeInTheDocument();
        expect(screen.getByText(/agora voc(ê|e) (é|e) n(í|i)vel 3/i)).toBeInTheDocument();
    });

    it('does not render when closed', () => {
        render(<LevelUpOverlay open={false} level={3} onDismiss={() => {}} />);

        expect(screen.queryByRole('dialog', { name: /subiu de n(í|i)vel/i })).toBeNull();
    });

    it('calls onDismiss when tapped', async () => {
        const user = userEvent.setup();
        const onDismiss = vi.fn();

        render(<LevelUpOverlay open level={2} onDismiss={onDismiss} />);
        await user.click(screen.getByRole('dialog', { name: /subiu de n(í|i)vel/i }));

        expect(onDismiss).toHaveBeenCalledOnce();
    });
});
