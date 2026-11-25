import type {
    CheckInComment,
    CheckInDetail,
    CheckInReactionsSummary,
    CreateCheckInRequest,
    CreateCheckInResponse,
    CreateCommentRequest,
    ListCheckInCommentsResponse,
    ListCheckInsResponse,
    ReactionsWithUsers,
    ToggleReactionRequest,
} from '@xpeak/shared';

import api from '@/api';

import type {
    ICheckInService,
    ListCheckInsParams,
    ListCommentsParams,
} from './interfaces/checkin.interface';

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

    async detail(id: string) {
        const response = await api.get<CheckInDetail>(`/check_ins/${id}`);
        return response.data;
    }

    async toggleReaction(id: string, payload: ToggleReactionRequest) {
        const response = await api.post<CheckInReactionsSummary>(
            `/check_ins/${id}/reactions`,
            payload,
        );
        return response.data;
    }

    async listReactionUsers(id: string) {
        const response = await api.get<ReactionsWithUsers>(
            `/check_ins/${id}/reactions`,
            { params: { include: 'users' } },
        );
        return response.data;
    }

    async listComments(id: string, params: ListCommentsParams = {}) {
        const response = await api.get<ListCheckInCommentsResponse>(
            `/check_ins/${id}/comments`,
            { params: { limit: params.limit, cursor: params.cursor } },
        );
        return response.data;
    }

    async createComment(id: string, payload: CreateCommentRequest) {
        const response = await api.post<CheckInComment>(
            `/check_ins/${id}/comments`,
            payload,
        );
        return response.data;
    }

    async deleteComment(commentId: string) {
        await api.delete(`/comments/${commentId}`);
    }
}

const checkInService = new CheckInService();

export default checkInService;
