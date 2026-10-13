import type {
    ChallengeProgressResponse,
    CreateChallengeRequest,
    CreateChallengeResponse,
    GetChallengeResponse,
    ListChallengesResponse,
} from '@xpeak/shared';

import api from '@/api';

import type { IChallengesService, ListChallengesParams } from './interfaces/challenges.interface';

class ChallengesService implements IChallengesService {
    async list(params: ListChallengesParams = {}) {
        const response = await api.get<ListChallengesResponse>('/challenges', {
            params: { state: params.state },
        });
        return response.data;
    }

    async get(id: string) {
        const response = await api.get<GetChallengeResponse>(`/challenges/${id}`);
        return response.data;
    }

    async progress(id: string) {
        const response = await api.get<ChallengeProgressResponse>(`/challenges/${id}/progress`);
        return response.data;
    }

    async create(payload: CreateChallengeRequest) {
        const response = await api.post<CreateChallengeResponse>('/challenges', payload);
        return response.data;
    }

    async delete(id: string) {
        await api.delete(`/challenges/${id}`);
    }
}

const challengesService = new ChallengesService();

export default challengesService;
