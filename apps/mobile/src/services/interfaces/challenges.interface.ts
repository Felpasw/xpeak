import type {
    ChallengeProgressResponse,
    ChallengeState,
    CreateChallengeRequest,
    CreateChallengeResponse,
    GetChallengeResponse,
    JoinChallengeRequest,
    JoinChallengeResponse,
    ListChallengesResponse,
    PresignTierImageResponse,
} from '@xpeak/shared';

export interface ListChallengesParams {
    state?: ChallengeState;
}

export interface IChallengesService {
    list(params?: ListChallengesParams): Promise<ListChallengesResponse>;
    get(id: string): Promise<GetChallengeResponse>;
    progress(id: string): Promise<ChallengeProgressResponse>;
    create(payload: CreateChallengeRequest): Promise<CreateChallengeResponse>;
    delete(id: string): Promise<void>;
    join(payload: JoinChallengeRequest): Promise<JoinChallengeResponse>;
    end(id: string): Promise<void>;
    presignTierImage(): Promise<PresignTierImageResponse>;
}
