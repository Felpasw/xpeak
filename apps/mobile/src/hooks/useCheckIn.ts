import {
    useInfiniteQuery,
    useMutation,
    useQuery,
    useQueryClient,
} from '@tanstack/react-query';

import { AUTH_QUERY_KEYS } from '@/hooks/useAuth';
import checkInService from '@/services/checkin.service';

import type {
    CategoriesQuery,
    CheckInsListQuery,
    CreateCheckInMutation,
    UseCheckInsListParams,
} from './interfaces/useCheckIn.interface';

const LIST_QUERY_PREFIX = ['checkins', 'list'] as const;
const DEFAULT_LIST_LIMIT = 20;

export const CHECK_IN_QUERY_KEYS = {
    categories: (groupId: string) => ['checkins', 'categories', groupId] as const,
    list: (groupId?: string) => [...LIST_QUERY_PREFIX, groupId ?? 'all'] as const,
    listPrefix: LIST_QUERY_PREFIX,
} as const;

export function useCategories(groupId: string): CategoriesQuery {
    return useQuery({
        queryKey: CHECK_IN_QUERY_KEYS.categories(groupId),
        queryFn: () => checkInService.listCategories(groupId),
    });
}

export function useCheckInsList(params: UseCheckInsListParams = {}): CheckInsListQuery {
    const limit = params.limit ?? DEFAULT_LIST_LIMIT;
    return useInfiniteQuery({
        queryKey: CHECK_IN_QUERY_KEYS.list(params.groupId),
        queryFn: ({ pageParam }) =>
            checkInService.list({ groupId: params.groupId, limit, cursor: pageParam ?? undefined }),
        initialPageParam: null as string | null,
        getNextPageParam: (last) => last.nextCursor,
    });
}

export function useCreateCheckIn(): CreateCheckInMutation {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (payload) => checkInService.create(payload),
        onSuccess: () => {
            // Bumps XP + level on the user record; profile page reads
            // `useMe` so invalidating it forces a refetch on the next
            // render.
            queryClient.invalidateQueries({ queryKey: AUTH_QUERY_KEYS.me });
            // History listing (all variants of groupId) needs to pick
            // up the new row without a manual pull-to-refresh.
            queryClient.invalidateQueries({ queryKey: CHECK_IN_QUERY_KEYS.listPrefix });
        },
    });
}
