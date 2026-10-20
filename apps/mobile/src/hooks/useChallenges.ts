/* eslint-disable react-hooks/rules-of-hooks --
 * ESLint flags hooks called inside a class body (assumes React class component),
 * but this is a plain TS class — not a React component. Every method is invoked
 * during a function component's render in stable order, so the runtime Rules
 * of Hooks contract is respected. Do NOT add conditionals or loops around the
 * hook calls below.
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import challengesService from '@/services/challenges.service';

import type {
    ChallengeDetailQuery,
    ChallengeProgressQuery,
    ChallengesListQuery,
    CreateChallengeMutation,
    DeleteChallengeMutation,
    EndChallengeMutation,
    IChallengesHooks,
    JoinChallengeMutation,
    UseChallengesListParams,
} from './interfaces/useChallenges.interface';

const LIST_PREFIX = ['challenges', 'list'] as const;

export const CHALLENGES_QUERY_KEYS = {
    list: (state?: string) => [...LIST_PREFIX, state ?? 'all'] as const,
    listPrefix: LIST_PREFIX,
    detail: (id: string) => ['challenges', 'detail', id] as const,
    progress: (id: string) => ['challenges', 'progress', id] as const,
} as const;

class ChallengesHooks implements IChallengesHooks {
    useList(params: UseChallengesListParams = {}): ChallengesListQuery {
        return useQuery({
            queryKey: CHALLENGES_QUERY_KEYS.list(params.state),
            queryFn: async () => {
                const response = await challengesService.list({ state: params.state });
                return response.challenges;
            },
        });
    }

    useDetail(id: string): ChallengeDetailQuery {
        return useQuery({
            queryKey: CHALLENGES_QUERY_KEYS.detail(id),
            queryFn: () => challengesService.get(id),
            enabled: Boolean(id),
        });
    }

    useProgress(id: string): ChallengeProgressQuery {
        return useQuery({
            queryKey: CHALLENGES_QUERY_KEYS.progress(id),
            queryFn: () => challengesService.progress(id),
            enabled: Boolean(id),
        });
    }

    useCreate(): CreateChallengeMutation {
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: (payload) => challengesService.create(payload),
            onSuccess: () => {
                queryClient.invalidateQueries({ queryKey: CHALLENGES_QUERY_KEYS.listPrefix });
            },
        });
    }

    useDelete(): DeleteChallengeMutation {
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: (id) => challengesService.delete(id),
            onSuccess: () => {
                queryClient.invalidateQueries({ queryKey: CHALLENGES_QUERY_KEYS.listPrefix });
            },
        });
    }

    useJoin(): JoinChallengeMutation {
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: (payload) => challengesService.join(payload),
            onSuccess: () => {
                queryClient.invalidateQueries({ queryKey: CHALLENGES_QUERY_KEYS.listPrefix });
            },
        });
    }

    useEnd(): EndChallengeMutation {
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: (id) => challengesService.end(id),
            onSuccess: (_, id) => {
                queryClient.invalidateQueries({ queryKey: CHALLENGES_QUERY_KEYS.listPrefix });
                queryClient.invalidateQueries({ queryKey: CHALLENGES_QUERY_KEYS.detail(id) });
            },
        });
    }
}

const challengesHooks = new ChallengesHooks();

export default challengesHooks;
