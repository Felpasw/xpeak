import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MediaKind } from '@xpeak/shared';
import { useState } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { MediaPicker } from '@/components/atoms/MediaPicker';
import type { SelectedMedia } from '@/lib/media/types';

vi.mock('@/lib/media/picker', () => ({
    pickFromGallery: vi.fn(),
    pickFromCamera: vi.fn(),
}));

const { pickFromGallery, pickFromCamera } = await import('@/lib/media/picker');

function buildMedia(id: string): SelectedMedia {
    return {
        id,
        kind: MediaKind.Photo,
        previewUrl: `blob:fake/${id}`,
        blob: new Blob(['x']),
        mimeType: 'image/jpeg',
    };
}

function Host({ initial = [] as SelectedMedia[], max = 10 }) {
    const [value, setValue] = useState<SelectedMedia[]>(initial);
    return <MediaPicker value={value} onChange={setValue} max={max} />;
}

describe('<MediaPicker />', () => {
    beforeEach(() => {
        vi.mocked(pickFromGallery).mockReset();
        vi.mocked(pickFromCamera).mockReset();
    });

    it('appends picked items from the gallery to value', async () => {
        const user = userEvent.setup();
        const picked = [buildMedia('a'), buildMedia('b')];
        vi.mocked(pickFromGallery).mockResolvedValue(picked);

        render(<Host max={5} />);
        await user.click(screen.getByRole('button', { name: /galeria/i }));

        expect(pickFromGallery).toHaveBeenCalledWith({ limit: 5 });
        expect(await screen.findByAltText(/mídia 1/i)).toBeInTheDocument();
        expect(screen.getByAltText(/mídia 2/i)).toBeInTheDocument();
    });

    it('appends a camera-captured photo to value', async () => {
        const user = userEvent.setup();
        vi.mocked(pickFromCamera).mockResolvedValue(buildMedia('cam'));

        render(<Host />);
        await user.click(screen.getByRole('button', { name: /câmera/i }));

        expect(pickFromCamera).toHaveBeenCalled();
        expect(await screen.findByAltText(/mídia 1/i)).toBeInTheDocument();
    });

    it('removes an item when its remove button is clicked', async () => {
        const user = userEvent.setup();
        render(<Host initial={[buildMedia('a'), buildMedia('b')]} />);

        await user.click(screen.getByRole('button', { name: /remover mídia 1/i }));

        expect(screen.queryByAltText(/mídia 2/i)).toBeNull();
        expect(screen.getByAltText(/mídia 1/i)).toBeInTheDocument();
    });

    it('disables add buttons when value reaches max', () => {
        const items = Array.from({ length: 3 }, (_, i) => buildMedia(`m${i}`));
        render(<Host initial={items} max={3} />);

        expect(screen.getByRole('button', { name: /galeria/i })).toBeDisabled();
        expect(screen.getByRole('button', { name: /câmera/i })).toBeDisabled();
    });
});
