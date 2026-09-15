import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import Page from '../page';

describe('Root page', () => {
  it('renders the Hello XPeak headline', () => {
    render(<Page />);

    expect(
      screen.getByRole('heading', { level: 1, name: /hello xpeak/i }),
    ).toBeInTheDocument();
  });

  it('renders a mobile-first caption teasing progression', () => {
    render(<Page />);

    expect(screen.getByText(/level up here soon/i)).toBeInTheDocument();
  });
});
