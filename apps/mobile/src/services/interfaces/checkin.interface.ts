import type {
    Category,
    CreateCheckInRequest,
    CreateCheckInResponse,
    ListCheckInsResponse,
} from '@xpeak/shared';

export interface ListCheckInsParams {
    limit?: number;
    cursor?: string;
    groupId?: string;
}

export interface ICheckInService {
    listCategories(groupId: string): Promise<Category[]>;
    create(payload: CreateCheckInRequest): Promise<CreateCheckInResponse>;
    list(params?: ListCheckInsParams): Promise<ListCheckInsResponse>;
}
