import type {
    CreateCheckInRequest,
    CreateCheckInResponse,
    ListCheckInsResponse,
} from '@xpeak/shared';

export interface ListCheckInsParams {
    limit?: number;
    cursor?: string;
}

export interface ICheckInService {
    create(payload: CreateCheckInRequest): Promise<CreateCheckInResponse>;
    list(params?: ListCheckInsParams): Promise<ListCheckInsResponse>;
}
