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

export interface ListCheckInsParams {
    limit?: number;
    cursor?: string;
}

export interface ListCommentsParams {
    limit?: number;
    cursor?: string;
}

export interface ICheckInService {
    create(payload: CreateCheckInRequest): Promise<CreateCheckInResponse>;
    list(params?: ListCheckInsParams): Promise<ListCheckInsResponse>;
    detail(id: string): Promise<CheckInDetail>;
    toggleReaction(
        id: string,
        payload: ToggleReactionRequest,
    ): Promise<CheckInReactionsSummary>;
    listReactionUsers(id: string): Promise<ReactionsWithUsers>;
    listComments(
        id: string,
        params?: ListCommentsParams,
    ): Promise<ListCheckInCommentsResponse>;
    createComment(
        id: string,
        payload: CreateCommentRequest,
    ): Promise<CheckInComment>;
    deleteComment(commentId: string): Promise<void>;
}
