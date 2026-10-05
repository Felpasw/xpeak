import type {
    ConfirmMediaRequest,
    ConfirmMediaResponse,
    PresignMediaRequest,
    PresignMediaResponse,
} from '@xpeak/shared';
import axios from 'axios';

import api from '@/api';

import type { IMediaService } from './interfaces/media.interface';

class MediaService implements IMediaService {
    async presign(checkInId: string, payload: PresignMediaRequest) {
        const response = await api.post<PresignMediaResponse>(
            `/check_ins/${checkInId}/media/presign`,
            payload,
        );
        return response.data;
    }

    async confirm(checkInId: string, payload: ConfirmMediaRequest) {
        const response = await api.post<ConfirmMediaResponse>(
            `/check_ins/${checkInId}/media`,
            payload,
        );
        return response.data;
    }

    async putToSignedUrl(url: string, blob: Blob, contentType: string) {
        await axios.put(url, blob, {
            headers: { 'Content-Type': contentType },
            transformRequest: (data) => data,
        });
    }
}

const mediaService: IMediaService = new MediaService();

export default mediaService;
