import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { AUTH_QUERY_KEYS } from '@/hooks/useAuth';
import checkInService from '@/services/checkin.service';

import type {
    CategoriesQuery,
    CreateCheckInMutation,
} from './interfaces/useCheckIn.interface';

export const CHECK_IN_QUERY_KEYS = {
    categories: (groupId: string) => ['checkins', 'categories', groupId] as const,
    list: (groupId?: string) => ['checkins', 'list', groupId ?? 'all'] as const,
} as const;

export function useCategories(groupId: string): CategoriesQuery {
    return useQuery({
        queryKey: CHECK_IN_QUERY_KEYS.categories(groupId),
        queryFn: () => checkInService.listCategories(groupId),
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
        },
    });
}
