import type {
    UseInfiniteQueryResult,
    UseMutationResult,
    UseQueryResult,
} from '@tanstack/react-query';
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
    ListChallengeCheckInsResponse,
} from '@xpeak/shared';

export interface UseChallengesListParams {
    state?: ChallengeState;
}

export type ChallengesListQuery = UseQueryResult<Challenge[]>;

export type ChallengesSummaryQuery = UseQueryResult<ChallengeSummary[]>;

export type ChallengeDetailQuery = UseQueryResult<GetChallengeResponse>;

export type ChallengeProgressQuery = UseQueryResult<ChallengeProgressResponse>;

export type ChallengeMembersQuery = UseQueryResult<ChallengeMemberRanking[]>;

export type ChallengeCheckInsQuery = UseInfiniteQueryResult<{
    pages: ListChallengeCheckInsResponse[];
    pageParams: Array<string | null>;
}>;

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
    useCheckIns(id: string | null | undefined): ChallengeCheckInsQuery;
    useCreate(): CreateChallengeMutation;
    useDelete(): DeleteChallengeMutation;
    useJoin(): JoinChallengeMutation;
    useEnd(): EndChallengeMutation;
}
