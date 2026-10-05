import { Camera, CameraResultType, CameraSource } from '@capacitor/camera';

import type { SelectedMedia } from '@/lib/media/types';

interface PickOptions {
    limit: number;
}

async function urlToBlob(url: string): Promise<Blob> {
    const response = await fetch(url);
    return response.blob();
}

function randomId() {
    return `media-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
}

export async function pickFromGallery({ limit }: PickOptions): Promise<SelectedMedia[]> {
    const result = await Camera.pickImages({ limit });
    return Promise.all(
        result.photos.map(async (photo) => {
            const previewUrl = photo.webPath;
            const blob = await urlToBlob(previewUrl);
            return {
                id: randomId(),
                kind: 'photo' as const,
                previewUrl,
                blob,
                mimeType: blob.type || `image/${photo.format}`,
            };
        }),
    );
}

export async function pickFromCamera(): Promise<SelectedMedia> {
    const photo = await Camera.getPhoto({
        source: CameraSource.Camera,
        resultType: CameraResultType.Uri,
        quality: 85,
    });
    const previewUrl = photo.webPath ?? '';
    const blob = await urlToBlob(previewUrl);
    return {
        id: randomId(),
        kind: 'photo',
        previewUrl,
        blob,
        mimeType: blob.type || `image/${photo.format}`,
    };
}
