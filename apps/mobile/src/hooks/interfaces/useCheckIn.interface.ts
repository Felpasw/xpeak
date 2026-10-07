import type {
    UseInfiniteQueryResult,
    UseMutationResult,
    UseQueryResult,
} from '@tanstack/react-query';
import type {
    Category,
    CreateCheckInRequest,
    CreateCheckInResponse,
    ListCheckInsResponse,
} from '@xpeak/shared';

export type CategoriesQuery = UseQueryResult<Category[]>;

export type CreateCheckInMutation = UseMutationResult<
    CreateCheckInResponse,
    unknown,
    CreateCheckInRequest
>;

export interface UseCheckInsListParams {
    groupId?: string;
    limit?: number;
}

export type CheckInsListQuery = UseInfiniteQueryResult<
    { pages: ListCheckInsResponse[]; pageParams: Array<string | null> },
    unknown
>;
