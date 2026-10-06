import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MediaKind } from '@xpeak/shared';
import { describe, expect, it } from 'vitest';

import { MediaGallery, type GalleryItem } from '@/components/atoms/MediaGallery';

const items: GalleryItem[] = [
    { id: 'a', kind: MediaKind.Photo, url: 'https://cdn.test/a.jpg' },
    { id: 'b', kind: MediaKind.Video, url: 'https://cdn.test/b.mp4' },
];

describe('<MediaGallery />', () => {
    it('opens the fullscreen viewer when a thumbnail is tapped', async () => {
        const user = userEvent.setup();
        render(<MediaGallery items={items} />);

        expect(screen.queryByRole('dialog', { name: /visualizar m(í|i)dia/i })).toBeNull();

        await user.click(screen.getByRole('button', { name: /abrir m(í|i)dia 1/i }));

        const dialog = await screen.findByRole('dialog', { name: /visualizar m(í|i)dia/i });
        expect(within(dialog).getByAltText(/m(í|i)dia 1/i)).toBeInTheDocument();
    });

    it('renders a <video> element when the active item is a video', async () => {
        const user = userEvent.setup();
        render(<MediaGallery items={items} />);

        await user.click(screen.getByRole('button', { name: /abrir m(í|i)dia 2/i }));

        const dialog = await screen.findByRole('dialog', { name: /visualizar m(í|i)dia/i });
        const video = dialog.querySelector('video');
        expect(video).not.toBeNull();
        expect(video?.querySelector('source')?.getAttribute('src')).toBe('https://cdn.test/b.mp4');
    });

    it('closes the viewer when the close button is clicked', async () => {
        const user = userEvent.setup();
        render(<MediaGallery items={items} />);

        await user.click(screen.getByRole('button', { name: /abrir m(í|i)dia 1/i }));
        await screen.findByRole('dialog', { name: /visualizar m(í|i)dia/i });

        await user.click(screen.getByRole('button', { name: /fechar/i }));

        await waitFor(() =>
            expect(screen.queryByRole('dialog', { name: /visualizar m(í|i)dia/i })).toBeNull(),
        );
    });
});

