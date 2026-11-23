import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import { CheckinMediaHero } from '@/components/atoms/CheckinMediaHero';

describe('<CheckinMediaHero />', () => {
    it('renders the image when mediaPreview is set', () => {
        render(
            <CheckinMediaHero
                mediaPreview={{ kind: 'photo', thumbUrl: 'https://cdn/x.jpg' }}
                title="Leg day"
            />,
        );

        expect(screen.getByTestId('checkin-media-hero')).toBeInTheDocument();
        expect(screen.getByAltText(/mídia do check-in Leg day/i)).toBeInTheDocument();
    });

    it('falls back to the placeholder when mediaPreview is null', () => {
        render(<CheckinMediaHero mediaPreview={null} title="No media" />);

        expect(
            screen.getByTestId('checkin-media-hero-placeholder'),
        ).toBeInTheDocument();
        expect(screen.queryByTestId('checkin-media-hero')).not.toBeInTheDocument();
    });
});
