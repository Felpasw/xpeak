import type {
    CreateCheckInRequest,
    CreateCheckInResponse,
    ListCheckInsResponse,
} from '@xpeak/shared';

import api from '@/api';

import type { ICheckInService, ListCheckInsParams } from './interfaces/checkin.interface';

class CheckInService implements ICheckInService {
    async create(payload: CreateCheckInRequest) {
        const response = await api.post<CreateCheckInResponse>('/check_ins', payload);
        return response.data;
    }

    async list(params: ListCheckInsParams = {}) {
        const response = await api.get<ListCheckInsResponse>('/check_ins', {
            params: {
                limit: params.limit,
                cursor: params.cursor,
            },
        });
        return response.data;
    }
}

const checkInService = new CheckInService();

export default checkInService;
