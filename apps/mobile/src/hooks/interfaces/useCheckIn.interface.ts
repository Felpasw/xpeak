import type { UseMutationResult, UseQueryResult } from '@tanstack/react-query';
import type { Category, CreateCheckInRequest, CreateCheckInResponse } from '@xpeak/shared';

export type CategoriesQuery = UseQueryResult<Category[]>;

export type CreateCheckInMutation = UseMutationResult<
    CreateCheckInResponse,
    unknown,
    CreateCheckInRequest
>;
