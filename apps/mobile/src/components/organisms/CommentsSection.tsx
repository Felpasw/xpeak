'use client';

import { formatDistanceToNow, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { Send, Trash2 } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';

import checkInHooks from '@/hooks/useCheckIn';

interface CommentsSectionProps {
    checkInId: string;
    currentUserId: string;
    checkInOwnerId: string;
}

const BODY_MAX_LENGTH = 280;
const EMPTY_COPY = 'Ainda sem comentários. Puxa a conversa.';
const ERROR_COPY = 'Falhou carregar os comentários.';
const SEND_LABEL = 'Enviar';
const RETRY_LABEL = 'Tentar de novo';
const DELETE_LABEL = 'Apagar';

export function CommentsSection({
    checkInId,
    currentUserId,
    checkInOwnerId,
}: CommentsSectionProps) {
    const list = checkInHooks.useListComments(checkInId);
    const create = checkInHooks.useCreateComment();
    const remove = checkInHooks.useDeleteComment();

    const [draft, setDraft] = useState('');
    const trimmed = draft.trim();
    const canSubmit = trimmed.length > 0 && !create.isPending;

    const sentinelRef = useRef<HTMLDivElement>(null);
    const { fetchNextPage, hasNextPage, isFetchingNextPage } = list;

    useEffect(() => {
        const node = sentinelRef.current;
        if (!node || !hasNextPage) {
            return;
        }
        const observer = new IntersectionObserver(([entry]) => {
            if (entry?.isIntersecting && !isFetchingNextPage) {
                fetchNextPage();
            }
        });
        observer.observe(node);
        return () => observer.disconnect();
    }, [fetchNextPage, hasNextPage, isFetchingNextPage]);

    const items = list.data?.pages.flatMap((p) => p.items) ?? [];

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!canSubmit) {
            return;
        }
        await create.mutateAsync({ checkInId, payload: { body: trimmed } });
        setDraft('');
    };

    return (
        <section
            data-testid="comments-section"
            className="flex flex-col gap-4"
        >
            <header className="flex items-baseline justify-between">
                <h2 className="font-mono text-[11px] uppercase tracking-[0.35em] text-white/50">
                    Comentários
                </h2>
                {items.length > 0 && (
                    <span
                        data-testid="comments-count"
                        className="font-mono text-xs text-zinc-500"
                    >
                        {items.length}
                    </span>
                )}
            </header>

            <CommentsBody
                status={list.status}
                itemsCount={items.length}
                onRetry={() => list.refetch()}
            >
                <ul className="flex flex-col gap-3">
                    {items.map((c) => {
                        const isAuthor = c.author.id === currentUserId;
                        const canDelete =
                            isAuthor || currentUserId === checkInOwnerId;
                        return (
                            <li
                                key={c.id}
                                data-testid={`comment-${c.id}`}
                                data-mine={isAuthor || undefined}
                                className="group/comment flex flex-col gap-1 rounded-2xl border border-zinc-800 bg-zinc-900/40 p-3 data-[mine]:border-sky-500/30 data-[mine]:bg-sky-500/5"
                            >
                                <div className="flex items-baseline justify-between gap-2">
                                    <div className="flex items-baseline gap-2">
                                        <span className="text-sm font-semibold text-zinc-100">
                                            @{c.author.username}
                                        </span>
                                        <span className="text-[11px] text-zinc-500">
                                            {formatDistanceToNow(parseISO(c.createdAt), {
                                                addSuffix: true,
                                                locale: ptBR,
                                            })}
                                        </span>
                                    </div>
                                    {canDelete && (
                                        <button
                                            type="button"
                                            aria-label={`${DELETE_LABEL} comentário`}
                                            disabled={remove.isPending}
                                            onClick={() =>
                                                remove.mutate({
                                                    checkInId,
                                                    commentId: c.id,
                                                })
                                            }
                                            data-pending={remove.isPending || undefined}
                                            className="text-zinc-500 transition-colors hover:text-red-400 data-[pending]:opacity-60"
                                        >
                                            <Trash2 className="h-3.5 w-3.5" />
                                        </button>
                                    )}
                                </div>
                                <p className="whitespace-pre-wrap break-words text-sm text-zinc-200">
                                    {c.body}
                                </p>
                            </li>
                        );
                    })}
                </ul>
                <div ref={sentinelRef} aria-hidden="true" className="h-2" />
            </CommentsBody>

            <form
                onSubmit={handleSubmit}
                className="flex flex-col gap-2 rounded-2xl border border-zinc-800 bg-zinc-900/40 p-3"
            >
                <textarea
                    value={draft}
                    onChange={(e) => setDraft(e.target.value.slice(0, BODY_MAX_LENGTH))}
                    maxLength={BODY_MAX_LENGTH}
                    placeholder="Deixa um comentário…"
                    rows={2}
                    className="resize-none bg-transparent text-sm text-zinc-100 placeholder:text-zinc-500 focus:outline-none"
                />
                <div className="flex items-center justify-between text-[11px] text-zinc-500">
                    <span data-testid="comments-counter">
                        {draft.length}/{BODY_MAX_LENGTH}
                    </span>
                    <button
                        type="submit"
                        disabled={!canSubmit}
                        data-pending={create.isPending || undefined}
                        className="inline-flex items-center gap-1.5 rounded-full border border-sky-500/40 bg-sky-500/10 px-3 py-1 text-xs font-semibold text-sky-300 transition-colors hover:border-sky-400/70 disabled:cursor-not-allowed disabled:opacity-40 data-[pending]:opacity-60"
                    >
                        <Send className="h-3 w-3" />
                        {SEND_LABEL}
                    </button>
                </div>
            </form>
        </section>
    );
}

interface CommentsBodyProps {
    status: 'pending' | 'error' | 'success';
    itemsCount: number;
    onRetry: () => void;
    children: React.ReactNode;
}

function CommentsBody({ status, itemsCount, onRetry, children }: CommentsBodyProps) {
    if (status === 'pending') {
        return (
            <div
                data-testid="comments-skeleton"
                className="flex flex-col gap-3"
            >
                <div className="h-16 animate-pulse rounded-2xl border border-zinc-800 bg-zinc-900/40" />
                <div className="h-16 animate-pulse rounded-2xl border border-zinc-800 bg-zinc-900/40" />
            </div>
        );
    }

    if (status === 'error') {
        return (
            <div
                data-testid="comments-error"
                className="flex items-center gap-3 rounded-2xl border border-zinc-800 bg-zinc-900/40 p-4 text-xs text-zinc-300"
            >
                <span>{ERROR_COPY}</span>
                <button
                    type="button"
                    onClick={onRetry}
                    className="rounded-full border border-sky-500/40 bg-sky-500/10 px-3 py-1 text-xs font-semibold text-sky-300"
                >
                    {RETRY_LABEL}
                </button>
            </div>
        );
    }

    if (itemsCount === 0) {
        return (
            <div
                data-testid="comments-empty"
                className="rounded-2xl border border-dashed border-zinc-800 bg-zinc-900/20 p-6 text-center text-sm text-zinc-400"
            >
                {EMPTY_COPY}
            </div>
        );
    }

    return <>{children}</>;
}
