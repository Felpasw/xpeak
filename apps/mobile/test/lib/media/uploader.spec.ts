import {
    MediaKind,
    type ConfirmMediaResponse,
    type PresignMediaResponse,
} from '@xpeak/shared';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { SelectedMedia } from '@/lib/media/types';
import { uploadCheckInMedia } from '@/lib/media/uploader';

vi.mock('@/services/media.service', () => ({
    default: {
        presign: vi.fn(),
        confirm: vi.fn(),
        putToSignedUrl: vi.fn(),
    },
}));

const { default: mediaService } = await import('@/services/media.service');

function buildMedia(id: string): SelectedMedia {
    return {
        id,
        kind: MediaKind.Photo,
        previewUrl: `blob:fake/${id}`,
        blob: new Blob([id], { type: 'image/jpeg' }),
        mimeType: 'image/jpeg',
    };
}

function buildPresign(storageKeys: string[]): PresignMediaResponse {
    return {
        presigned: storageKeys.map((storageKey, index) => ({
            storageKey,
            url: `https://upload.test/${storageKey}`,
            expiresIn: 600 + index,
        })),
    };
}

const confirmResponse: ConfirmMediaResponse = {
    media: [],
    publish: null,
};

describe('uploadCheckInMedia', () => {
    beforeEach(() => {
        vi.mocked(mediaService.presign).mockReset();
        vi.mocked(mediaService.confirm).mockReset();
        vi.mocked(mediaService.putToSignedUrl).mockReset();
    });

    it('presigns, uploads each blob and confirms with correct positions', async () => {
        const items = [buildMedia('a'), buildMedia('b'), buildMedia('c')];
        vi.mocked(mediaService.presign).mockResolvedValue(buildPresign(['s0', 's1', 's2']));
        vi.mocked(mediaService.putToSignedUrl).mockResolvedValue();
        vi.mocked(mediaService.confirm).mockResolvedValue(confirmResponse);

        const result = await uploadCheckInMedia('ck-1', items);

        expect(mediaService.presign).toHaveBeenCalledWith('ck-1', {
            items: [
                { kind: MediaKind.Photo },
                { kind: MediaKind.Photo },
                { kind: MediaKind.Photo },
            ],
        });
        expect(mediaService.putToSignedUrl).toHaveBeenCalledTimes(3);
        const [firstItem] = items;
        expect(firstItem).toBeDefined();
        expect(mediaService.putToSignedUrl).toHaveBeenNthCalledWith(
            1,
            'https://upload.test/s0',
            firstItem!.blob,
            'image/jpeg',
        );
        expect(mediaService.confirm).toHaveBeenCalledWith('ck-1', {
            items: [
                { storageKey: 's0', kind: MediaKind.Photo, position: 0 },
                { storageKey: 's1', kind: MediaKind.Photo, position: 1 },
                { storageKey: 's2', kind: MediaKind.Photo, position: 2 },
            ],
        });
        expect(result).toBe(confirmResponse);
    });

    it('retries a failed PUT and succeeds within the retry budget', async () => {
        const items = [buildMedia('a')];
        vi.mocked(mediaService.presign).mockResolvedValue(buildPresign(['s0']));
        vi.mocked(mediaService.putToSignedUrl)
            .mockRejectedValueOnce(new Error('500'))
            .mockResolvedValueOnce();
        vi.mocked(mediaService.confirm).mockResolvedValue(confirmResponse);

        await uploadCheckInMedia('ck-1', items, { retries: 1 });

        expect(mediaService.putToSignedUrl).toHaveBeenCalledTimes(2);
        expect(mediaService.confirm).toHaveBeenCalledOnce();
    });

    it('throws if a PUT keeps failing past the retry budget', async () => {
        const items = [buildMedia('a')];
        vi.mocked(mediaService.presign).mockResolvedValue(buildPresign(['s0']));
        vi.mocked(mediaService.putToSignedUrl).mockRejectedValue(new Error('500'));

        await expect(uploadCheckInMedia('ck-1', items, { retries: 2 })).rejects.toThrow('500');

        expect(mediaService.putToSignedUrl).toHaveBeenCalledTimes(3);
        expect(mediaService.confirm).not.toHaveBeenCalled();
    });
});
