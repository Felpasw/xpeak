import type { ConfirmMediaResponse, PresignMediaSlot } from '@xpeak/shared';

import type { SelectedMedia } from '@/lib/media/types';
import mediaService from '@/services/media.service';

interface UploadOptions {
    retries?: number;
}

const DEFAULT_RETRIES = 2;

async function putWithRetry(
    slot: PresignMediaSlot,
    item: SelectedMedia,
    retries: number,
): Promise<void> {
    let lastError: unknown;
    for (let attempt = 0; attempt <= retries; attempt += 1) {
        try {
            await mediaService.putToSignedUrl(slot.url, item.blob, item.mimeType);
            return;
        } catch (error) {
            lastError = error;
        }
    }
    throw lastError;
}

export async function uploadCheckInMedia(
    checkInId: string,
    items: SelectedMedia[],
    options: UploadOptions = {},
): Promise<ConfirmMediaResponse> {
    const retries = options.retries ?? DEFAULT_RETRIES;

    const presign = await mediaService.presign(checkInId, {
        items: items.map((item) => ({ kind: item.kind })),
    });

    const pairs = items.map((item, index) => {
        const slot = presign.presigned[index];
        if (!slot) {
            throw new Error('Presign returned fewer slots than requested items');
        }
        return { item, slot };
    });

    await Promise.all(pairs.map(({ item, slot }) => putWithRetry(slot, item, retries)));

    return mediaService.confirm(checkInId, {
        items: pairs.map(({ item, slot }, index) => ({
            storageKey: slot.storageKey,
            kind: item.kind,
            position: index,
        })),
    });
}
