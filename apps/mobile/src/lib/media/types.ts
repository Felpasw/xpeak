import type { MediaKind } from '@xpeak/shared';

export interface SelectedMedia {
    id: string;
    kind: MediaKind;
    previewUrl: string;
    blob: Blob;
    mimeType: string;
}

export const MEDIA_MAX_PER_CHECKIN = 10;
