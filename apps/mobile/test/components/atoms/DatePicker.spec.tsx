import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useState } from 'react';
import { describe, expect, it, vi } from 'vitest';

import { DatePicker } from '@/components/atoms/DatePicker';

function Host({
    initial = '2026-06-15',
    min,
    max,
    onChange,
}: {
    initial?: string;
    min?: string;
    max?: string;
    onChange?: (value: string) => void;
}) {
    const [value, setValue] = useState(initial);
    return (
        <DatePicker
            label="Dia do treino"
            value={value}
            onChange={(next) => {
                setValue(next);
                onChange?.(next);
            }}
            min={min}
            max={max}
        />
    );
}

describe('<DatePicker />', () => {
    it('opens the popover and selects a day inside the current month', async () => {
        const user = userEvent.setup();
        const handleChange = vi.fn();
        render(<Host initial="2026-06-15" onChange={handleChange} />);

        await user.click(screen.getByRole('button', { name: /dia do treino/i }));

        const grid = await screen.findByRole('grid', { name: /junho 2026/i });
        await user.click(within(grid).getByRole('gridcell', { name: '20' }));

        expect(handleChange).toHaveBeenCalledWith('2026-06-20');
        expect(screen.getByRole('button', { name: /dia do treino/i })).toHaveTextContent(
            '20/06/2026',
        );
    });

    it('disables days outside the min/max range', async () => {
        const user = userEvent.setup();
        render(<Host initial="2026-06-15" min="2026-06-10" max="2026-06-20" />);

        await user.click(screen.getByRole('button', { name: /dia do treino/i }));

        const grid = await screen.findByRole('grid', { name: /junho 2026/i });
        expect(within(grid).getByRole('gridcell', { name: '5' })).toBeDisabled();
        expect(within(grid).getByRole('gridcell', { name: '25' })).toBeDisabled();
        expect(within(grid).getByRole('gridcell', { name: '15' })).toBeEnabled();
    });

    it('closes the popover on Escape', async () => {
        const user = userEvent.setup();
        render(<Host />);

        await user.click(screen.getByRole('button', { name: /dia do treino/i }));
        expect(screen.getByRole('dialog')).toBeInTheDocument();

        await user.keyboard('{Escape}');

        await waitFor(() =>
            expect(screen.queryByRole('dialog')).not.toBeInTheDocument(),
        );
    });
});
