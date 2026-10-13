import type { UseMutationResult, UseQueryResult } from '@tanstack/react-query';
import type {
    Challenge,
    ChallengeProgressResponse,
    ChallengeState,
    CreateChallengeRequest,
    CreateChallengeResponse,
    GetChallengeResponse,
} from '@xpeak/shared';

export interface UseChallengesListParams {
    state?: ChallengeState;
}

export type ChallengesListQuery = UseQueryResult<Challenge[]>;

export type ChallengeDetailQuery = UseQueryResult<GetChallengeResponse>;

export type ChallengeProgressQuery = UseQueryResult<ChallengeProgressResponse>;

export type CreateChallengeMutation = UseMutationResult<
    CreateChallengeResponse,
    unknown,
    CreateChallengeRequest
>;

export type DeleteChallengeMutation = UseMutationResult<void, unknown, string>;
