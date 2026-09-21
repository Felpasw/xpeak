import { render } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import Page from '@/app/page';

describe('Root page', () => {
  it('renders the XPeak lightning symbol', () => {
    const { container } = render(<Page />);

    expect(container.querySelector('canvas')).toBeInTheDocument();
  });
});
