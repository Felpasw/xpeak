import type { UseQueryResult } from '@tanstack/react-query';
import type { PublicUser } from '@xpeak/shared';

export interface IUsersHooks {
    useOne(username: string | null | undefined): UseQueryResult<PublicUser | null>;
}
