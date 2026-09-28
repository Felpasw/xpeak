import type {
    CreateCheckInRequest,
    CreateCheckInResponse,
    ListCategoriesResponse,
    ListCheckInsResponse,
} from '@xpeak/shared';

import api from '@/api';

import type { ICheckInService, ListCheckInsParams } from './interfaces/checkin.interface';

class CheckInService implements ICheckInService {
    async listCategories(groupId: string) {
        const response = await api.get<ListCategoriesResponse>(
            `/groups/${groupId}/categories`,
        );
        return response.data.categories;
    }

    async create(payload: CreateCheckInRequest) {
        const response = await api.post<CreateCheckInResponse>('/check_ins', payload);
        return response.data;
    }

    async list(params: ListCheckInsParams = {}) {
        const response = await api.get<ListCheckInsResponse>('/check_ins', {
            params: {
                limit: params.limit,
                cursor: params.cursor,
                group_id: params.groupId,
            },
        });
        return response.data;
    }
}

const checkInService = new CheckInService();

export default checkInService;
