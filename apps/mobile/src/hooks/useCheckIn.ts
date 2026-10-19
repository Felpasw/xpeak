import {
    useInfiniteQuery,
    useMutation,
    useQueryClient,
} from '@tanstack/react-query';

import { AUTH_QUERY_KEYS } from '@/hooks/useAuth';
import checkInService from '@/services/checkin.service';

import type {
    CheckInsListQuery,
    CreateCheckInMutation,
    UseCheckInsListParams,
} from './interfaces/useCheckIn.interface';

const LIST_QUERY_PREFIX = ['checkins', 'list'] as const;
const DEFAULT_LIST_LIMIT = 20;

export const CHECK_IN_QUERY_KEYS = {
    list: () => LIST_QUERY_PREFIX,
    listPrefix: LIST_QUERY_PREFIX,
} as const;

export function useCheckInsList(params: UseCheckInsListParams = {}): CheckInsListQuery {
    const limit = params.limit ?? DEFAULT_LIST_LIMIT;
    return useInfiniteQuery({
        queryKey: CHECK_IN_QUERY_KEYS.list(),
        queryFn: ({ pageParam }) =>
            checkInService.list({ limit, cursor: pageParam ?? undefined }),
        initialPageParam: null as string | null,
        getNextPageParam: (last) => last.nextCursor,
    });
}

export function useCreateCheckIn(): CreateCheckInMutation {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (payload) => checkInService.create(payload),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: AUTH_QUERY_KEYS.me });
            queryClient.invalidateQueries({ queryKey: CHECK_IN_QUERY_KEYS.listPrefix });
        },
    });
}
