import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import challengesService from '@/services/challenges.service';

import type {
    ChallengeDetailQuery,
    ChallengeProgressQuery,
    ChallengesListQuery,
    CreateChallengeMutation,
    DeleteChallengeMutation,
    UseChallengesListParams,
} from './interfaces/useChallenges.interface';

const LIST_PREFIX = ['challenges', 'list'] as const;

export const CHALLENGES_QUERY_KEYS = {
    list: (state?: string) => [...LIST_PREFIX, state ?? 'all'] as const,
    listPrefix: LIST_PREFIX,
    detail: (id: string) => ['challenges', 'detail', id] as const,
    progress: (id: string) => ['challenges', 'progress', id] as const,
} as const;

export function useChallengesList(params: UseChallengesListParams = {}): ChallengesListQuery {
    return useQuery({
        queryKey: CHALLENGES_QUERY_KEYS.list(params.state),
        queryFn: async () => {
            const response = await challengesService.list({ state: params.state });
            return response.challenges;
        },
    });
}

export function useChallenge(id: string): ChallengeDetailQuery {
    return useQuery({
        queryKey: CHALLENGES_QUERY_KEYS.detail(id),
        queryFn: () => challengesService.get(id),
        enabled: Boolean(id),
    });
}

export function useChallengeProgress(id: string): ChallengeProgressQuery {
    return useQuery({
        queryKey: CHALLENGES_QUERY_KEYS.progress(id),
        queryFn: () => challengesService.progress(id),
        enabled: Boolean(id),
    });
}

export function useCreateChallenge(): CreateChallengeMutation {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (payload) => challengesService.create(payload),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: CHALLENGES_QUERY_KEYS.listPrefix });
        },
    });
}

export function useDeleteChallenge(): DeleteChallengeMutation {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: (id) => challengesService.delete(id),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: CHALLENGES_QUERY_KEYS.listPrefix });
        },
    });
}
