/* eslint-disable react-hooks/rules-of-hooks --
 * ESLint flags hooks called inside a class body (assumes React class component),
 * but this is a plain TS class — not a React component. Every method is invoked
 * during a function component's render in stable order, so the runtime Rules
 * of Hooks contract is respected. Do NOT add conditionals or loops around the
 * hook calls below.
 */

import { useQuery } from '@tanstack/react-query';

import usersService from '@/services/users.service';

import type { IUsersHooks } from './interfaces/useUsers.interface';

export const USERS_QUERY_KEYS = {
    one: (username: string) => ['users', 'one', username] as const,
} as const;

class UsersHooks implements IUsersHooks {
    useOne(username: string | null | undefined) {
        return useQuery({
            queryKey: username
                ? USERS_QUERY_KEYS.one(username)
                : (['users', 'one', '_disabled'] as const),
            queryFn: () => usersService.get(username!),
            enabled: Boolean(username),
        });
    }
}

const usersHooks = new UsersHooks();

export default usersHooks;
