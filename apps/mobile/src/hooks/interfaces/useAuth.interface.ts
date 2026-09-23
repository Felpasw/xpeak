import type { UseMutationResult, UseQueryResult } from '@tanstack/react-query';
import type { User } from '@xpeak/shared';

import type { LoginPayload } from '@/services/interfaces/auth.interface';

export interface AuthMutationResult {
    user: User;
    token: string | null;
}

export interface AuthHooksResult {
    me: UseQueryResult<User | null>;
    login: UseMutationResult<AuthMutationResult, unknown, LoginPayload>;
    logout: UseMutationResult<void, unknown, void>;
}

export interface IAuthHooks {
    use(): AuthHooksResult;
}
