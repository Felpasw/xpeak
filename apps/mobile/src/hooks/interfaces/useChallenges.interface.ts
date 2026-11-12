import type { UseMutationResult, UseQueryResult } from '@tanstack/react-query';
import type {
    Challenge,
    ChallengeMemberRanking,
    ChallengeProgressResponse,
    ChallengeState,
    ChallengeSummary,
    CreateChallengeRequest,
    CreateChallengeResponse,
    GetChallengeResponse,
    JoinChallengeRequest,
    JoinChallengeResponse,
} from '@xpeak/shared';

export interface UseChallengesListParams {
    state?: ChallengeState;
}

export type ChallengesListQuery = UseQueryResult<Challenge[]>;

export type ChallengesSummaryQuery = UseQueryResult<ChallengeSummary[]>;

export type ChallengeDetailQuery = UseQueryResult<GetChallengeResponse>;

export type ChallengeProgressQuery = UseQueryResult<ChallengeProgressResponse>;

export type ChallengeMembersQuery = UseQueryResult<ChallengeMemberRanking[]>;

export type CreateChallengeMutation = UseMutationResult<
    CreateChallengeResponse,
    unknown,
    CreateChallengeRequest
>;

export type DeleteChallengeMutation = UseMutationResult<void, unknown, string>;

export type JoinChallengeMutation = UseMutationResult<
    JoinChallengeResponse,
    unknown,
    JoinChallengeRequest
>;

export type EndChallengeMutation = UseMutationResult<void, unknown, string>;

export interface IChallengesHooks {
    useList(params?: UseChallengesListParams): ChallengesListQuery;
    useSummary(): ChallengesSummaryQuery;
    useDetail(id: string): ChallengeDetailQuery;
    useProgress(id: string): ChallengeProgressQuery;
    useMembers(id: string | null | undefined): ChallengeMembersQuery;
    useCreate(): CreateChallengeMutation;
    useDelete(): DeleteChallengeMutation;
    useJoin(): JoinChallengeMutation;
    useEnd(): EndChallengeMutation;
}
