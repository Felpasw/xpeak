import type {
    ChallengeProgressResponse,
    CreateChallengeRequest,
    CreateChallengeResponse,
    GetChallengeResponse,
    JoinChallengeRequest,
    JoinChallengeResponse,
    ListChallengeMembersResponse,
    ListChallengesResponse,
    ListChallengesSummaryResponse,
    PresignTierImageResponse,
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

    async summary() {
        const response = await api.get<ListChallengesSummaryResponse>('/challenges/summary');
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

    async members(id: string) {
        const response = await api.get<ListChallengeMembersResponse>(`/challenges/${id}/members`);
        return response.data;
    }

    async create(payload: CreateChallengeRequest) {
        const response = await api.post<CreateChallengeResponse>('/challenges', payload);
        return response.data;
    }

    async delete(id: string) {
        await api.delete(`/challenges/${id}`);
    }

    async join(payload: JoinChallengeRequest) {
        const response = await api.post<JoinChallengeResponse>('/challenges/join', payload);
        return response.data;
    }

    async end(id: string) {
        await api.post(`/challenges/${id}/end`);
    }

    async presignTierImage() {
        const response = await api.post<PresignTierImageResponse>('/challenges/tier-images/presign');
        return response.data;
    }
}

const challengesService = new ChallengesService();

export default challengesService;
