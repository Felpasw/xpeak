import type { PublicUser } from '@xpeak/shared';

export interface IUsersService {
    get(username: string): Promise<PublicUser | null>;
}
