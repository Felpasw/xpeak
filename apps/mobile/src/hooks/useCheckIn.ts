/* eslint-disable react-hooks/rules-of-hooks --
 * ESLint flags hooks called inside a class body (assumes React class component),
 * but this is a plain TS class — not a React component. Every method is invoked
 * during a function component's render in stable order, so the runtime Rules
 * of Hooks contract is respected. Do NOT add conditionals or loops around the
 * hook calls below.
 */

import {
    useInfiniteQuery,
    useMutation,
    useQuery,
    useQueryClient,
} from '@tanstack/react-query';

import { AUTH_QUERY_KEYS } from '@/hooks/useAuth';
import checkInService from '@/services/checkin.service';

import type {
    CheckInCommentsQuery,
    CheckInDetailQuery,
    CheckInsListQuery,
    CreateCheckInMutation,
    CreateCommentMutation,
    DeleteCommentMutation,
    ICheckInHooks,
    ReactionUsersQuery,
    ToggleReactionMutation,
    UseCheckInsListParams,
} from './interfaces/useCheckIn.interface';

const LIST_QUERY_PREFIX = ['checkins', 'list'] as const;
const DETAIL_PREFIX = ['checkins', 'detail'] as const;
const REACTION_USERS_PREFIX = ['checkins', 'reactions', 'users'] as const;
const COMMENTS_PREFIX = ['checkins', 'comments'] as const;
const DEFAULT_LIST_LIMIT = 20;
const DEFAULT_COMMENTS_LIMIT = 20;

export const CHECK_IN_QUERY_KEYS = {
    list: () => LIST_QUERY_PREFIX,
    listPrefix: LIST_QUERY_PREFIX,
    detail: (id: string) => [...DETAIL_PREFIX, id] as const,
    detailPrefix: DETAIL_PREFIX,
    reactionUsers: (id: string) => [...REACTION_USERS_PREFIX, id] as const,
    comments: (id: string) => [...COMMENTS_PREFIX, id] as const,
    commentsPrefix: COMMENTS_PREFIX,
} as const;

class CheckInHooks implements ICheckInHooks {
    useList(params: UseCheckInsListParams = {}): CheckInsListQuery {
        const limit = params.limit ?? DEFAULT_LIST_LIMIT;
        return useInfiniteQuery({
            queryKey: CHECK_IN_QUERY_KEYS.list(),
            queryFn: ({ pageParam }) =>
                checkInService.list({ limit, cursor: pageParam ?? undefined }),
            initialPageParam: null as string | null,
            getNextPageParam: (last) => last.nextCursor,
        });
    }

    useCreate(): CreateCheckInMutation {
        const queryClient = useQueryClient();
        return useMutation({
            mutationFn: (payload) => checkInService.create(payload),
            onSuccess: () => {
                queryClient.invalidateQueries({ queryKey: AUTH_QUERY_KEYS.me });
                queryClient.invalidateQueries({ queryKey: CHECK_IN_QUERY_KEYS.listPrefix });
            },
        });
    }

    useDetail(id: string | null | undefined): CheckInDetailQuery {
        return useQuery({
            queryKey: CHECK_IN_QUERY_KEYS.detail(id ?? ''),
            queryFn: () => checkInService.detail(id as string),
            enabled: Boolean(id),
        });
    }

    useToggleReaction(): ToggleReactionMutation {
        const queryClient = useQueryClient();
        return useMutation({
            mutationFn: ({ checkInId, emoji }) =>
                checkInService.toggleReaction(checkInId, { emoji }),
            onSuccess: (_, { checkInId }) => {
                queryClient.invalidateQueries({
                    queryKey: CHECK_IN_QUERY_KEYS.detail(checkInId),
                });
                queryClient.invalidateQueries({
                    queryKey: CHECK_IN_QUERY_KEYS.reactionUsers(checkInId),
                });
                queryClient.invalidateQueries({
                    queryKey: CHECK_IN_QUERY_KEYS.listPrefix,
                });
            },
        });
    }

    useReactionUsers(
        id: string | null | undefined,
        enabled = true,
    ): ReactionUsersQuery {
        return useQuery({
            queryKey: CHECK_IN_QUERY_KEYS.reactionUsers(id ?? ''),
            queryFn: () => checkInService.listReactionUsers(id as string),
            enabled: Boolean(id) && enabled,
        });
    }

    useListComments(id: string | null | undefined): CheckInCommentsQuery {
        return useInfiniteQuery({
            queryKey: CHECK_IN_QUERY_KEYS.comments(id ?? ''),
            queryFn: ({ pageParam }) =>
                checkInService.listComments(id as string, {
                    limit: DEFAULT_COMMENTS_LIMIT,
                    cursor: pageParam ?? undefined,
                }),
            initialPageParam: null as string | null,
            getNextPageParam: (last) => last.nextCursor,
            enabled: Boolean(id),
        });
    }

    useCreateComment(): CreateCommentMutation {
        const queryClient = useQueryClient();
        return useMutation({
            mutationFn: ({ checkInId, payload }) =>
                checkInService.createComment(checkInId, payload),
            onSuccess: (_, { checkInId }) => {
                queryClient.invalidateQueries({
                    queryKey: CHECK_IN_QUERY_KEYS.comments(checkInId),
                });
                queryClient.invalidateQueries({
                    queryKey: CHECK_IN_QUERY_KEYS.detail(checkInId),
                });
                queryClient.invalidateQueries({
                    queryKey: CHECK_IN_QUERY_KEYS.listPrefix,
                });
            },
        });
    }

    useDeleteComment(): DeleteCommentMutation {
        const queryClient = useQueryClient();
        return useMutation({
            mutationFn: ({ commentId }) => checkInService.deleteComment(commentId),
            onSuccess: (_, { checkInId }) => {
                queryClient.invalidateQueries({
                    queryKey: CHECK_IN_QUERY_KEYS.comments(checkInId),
                });
                queryClient.invalidateQueries({
                    queryKey: CHECK_IN_QUERY_KEYS.detail(checkInId),
                });
                queryClient.invalidateQueries({
                    queryKey: CHECK_IN_QUERY_KEYS.listPrefix,
                });
            },
        });
    }
}

const checkInHooks = new CheckInHooks();

export default checkInHooks;
