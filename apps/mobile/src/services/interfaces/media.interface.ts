import type {
    ConfirmMediaRequest,
    ConfirmMediaResponse,
    PresignMediaRequest,
    PresignMediaResponse,
} from '@xpeak/shared';

export interface IMediaService {
    presign(checkInId: string, payload: PresignMediaRequest): Promise<PresignMediaResponse>;
    confirm(checkInId: string, payload: ConfirmMediaRequest): Promise<ConfirmMediaResponse>;
    putToSignedUrl(url: string, blob: Blob, contentType: string): Promise<void>;
}
