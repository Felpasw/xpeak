import type {
    UseInfiniteQueryResult,
    UseMutationResult,
} from '@tanstack/react-query';
import type {
    CreateCheckInRequest,
    CreateCheckInResponse,
    ListCheckInsResponse,
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
