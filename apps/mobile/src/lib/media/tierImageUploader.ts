import challengesService from '@/services/challenges.service';

export interface UploadedTierImage {
    storageKey: string;
}

/**
 * Uploads a file to Cloudinary (or the dev fake backend) using the
 * presign flow:
 *
 * 1. POST to the API — get `{ storageKey, uploadUrl }`
 * 2. POST FormData with `file` field to `uploadUrl` (Cloudinary
 *    expects multipart/form-data, not raw PUT)
 * 3. Return `storageKey` so the caller can embed it in the tier
 *    payload of `POST /challenges`
 */
export async function uploadTierImage(file: File): Promise<UploadedTierImage> {
    const presign = await challengesService.presignTierImage();

    const form = new FormData();
    form.append('file', file);

    const response = await fetch(presign.uploadUrl, {
        method: 'POST',
        body: form,
    });

    if (!response.ok) {
        throw new Error(`tier image upload failed: ${response.status}`);
    }

    return { storageKey: presign.storageKey };
}
