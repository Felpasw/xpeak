/* eslint-disable react-hooks/rules-of-hooks --
 * ESLint flags hooks called inside a class body (assumes React class component),
 * but this is a plain TS class — not a React component. `authHooks.use()` is
 * invoked during render in stable order, so the runtime Rules of Hooks contract
 * is respected. Do NOT add conditionals or loops around the hook calls below.
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { User } from '@xpeak/shared';

import authService from '@/services/auth.service';
import type { LoginPayload } from '@/services/interfaces/auth.interface';

import type {
    AuthHooksResult,
    AuthMutationResult,
    IAuthHooks,
} from './interfaces/useAuth.interface';

export const AUTH_QUERY_KEYS = {
    me: ['auth', 'me'] as const,
} as const;

class AuthHooks implements IAuthHooks {
    use(): AuthHooksResult {
        const queryClient = useQueryClient();

        const me = useQuery<User | null>({
            queryKey: AUTH_QUERY_KEYS.me,
            queryFn: () => authService.me(),
        });

        const login = useMutation<AuthMutationResult, unknown, LoginPayload>({
            mutationFn: (payload) => authService.login(payload),
            onSuccess: ({ user }) => {
                queryClient.setQueryData(AUTH_QUERY_KEYS.me, user);
            },
        });

        const logout = useMutation<void, unknown, void>({
            mutationFn: () => authService.logout(),
            onSuccess: () => {
                queryClient.setQueryData(AUTH_QUERY_KEYS.me, null);
            },
        });

        return { me, login, logout };
    }
}

const authHooks = new AuthHooks();

export default authHooks;
