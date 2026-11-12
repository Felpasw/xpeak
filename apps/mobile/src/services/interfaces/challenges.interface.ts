import type {
    ChallengeProgressResponse,
    ChallengeState,
    CreateChallengeRequest,
    CreateChallengeResponse,
    GetChallengeResponse,
    JoinChallengeRequest,
    JoinChallengeResponse,
    ListChallengeCheckInsResponse,
    ListChallengeMembersResponse,
    ListChallengesResponse,
    ListChallengesSummaryResponse,
    PresignTierImageResponse,
} from '@xpeak/shared';

export interface ListChallengesParams {
    state?: ChallengeState;
}

export interface ListChallengeCheckInsParams {
    cursor?: string;
    limit?: number;
}

export interface IChallengesService {
    list(params?: ListChallengesParams): Promise<ListChallengesResponse>;
    summary(): Promise<ListChallengesSummaryResponse>;
    get(id: string): Promise<GetChallengeResponse>;
    progress(id: string): Promise<ChallengeProgressResponse>;
    members(id: string): Promise<ListChallengeMembersResponse>;
    checkIns(
        id: string,
        params?: ListChallengeCheckInsParams,
    ): Promise<ListChallengeCheckInsResponse>;
    create(payload: CreateChallengeRequest): Promise<CreateChallengeResponse>;
    delete(id: string): Promise<void>;
    join(payload: JoinChallengeRequest): Promise<JoinChallengeResponse>;
    end(id: string): Promise<void>;
    presignTierImage(): Promise<PresignTierImageResponse>;
}
