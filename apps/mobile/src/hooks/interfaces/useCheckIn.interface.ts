import type {
    UseInfiniteQueryResult,
    UseMutationResult,
    UseQueryResult,
} from '@tanstack/react-query';
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
} from '@xpeak/shared';

export type CreateCheckInMutation = UseMutationResult<
    CreateCheckInResponse,
    unknown,
    CreateCheckInRequest
>;

export interface UseCheckInsListParams {
    limit?: number;
}

export type CheckInsListQuery = UseInfiniteQueryResult<
    { pages: ListCheckInsResponse[]; pageParams: Array<string | null> },
    unknown
>;

export type CheckInDetailQuery = UseQueryResult<CheckInDetail>;

export interface ToggleReactionVariables {
    checkInId: string;
    emoji: string;
}

export type ToggleReactionMutation = UseMutationResult<
    CheckInReactionsSummary,
    unknown,
    ToggleReactionVariables
>;

export type ReactionUsersQuery = UseQueryResult<ReactionsWithUsers>;

export type CheckInCommentsQuery = UseInfiniteQueryResult<
    { pages: ListCheckInCommentsResponse[]; pageParams: Array<string | null> },
    unknown
>;

export interface CreateCommentVariables {
    checkInId: string;
    payload: CreateCommentRequest;
}

export type CreateCommentMutation = UseMutationResult<
    CheckInComment,
    unknown,
    CreateCommentVariables
>;

export interface DeleteCommentVariables {
    checkInId: string;
    commentId: string;
}

export type DeleteCommentMutation = UseMutationResult<
    void,
    unknown,
    DeleteCommentVariables
>;

export interface ICheckInHooks {
    useList(params?: UseCheckInsListParams): CheckInsListQuery;
    useCreate(): CreateCheckInMutation;
    useDetail(id: string | null | undefined): CheckInDetailQuery;
    useToggleReaction(): ToggleReactionMutation;
    useReactionUsers(
        id: string | null | undefined,
        enabled?: boolean,
    ): ReactionUsersQuery;
    useListComments(id: string | null | undefined): CheckInCommentsQuery;
    useCreateComment(): CreateCommentMutation;
    useDeleteComment(): DeleteCommentMutation;
}
