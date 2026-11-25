'use client';

import { ChevronDown, Users } from 'lucide-react';
import { useState } from 'react';

import type { CheckInReactionsSummary } from '@xpeak/shared';

import { REACTION_PALETTE } from '@/lib/reactionPalette';
import checkInHooks from '@/hooks/useCheckIn';

interface ReactionsStripProps {
    checkInId: string;
    reactions: CheckInReactionsSummary;
}

const SHOW_USERS_LABEL = 'Reações';
const USERS_LOADING = 'Carregando reações…';
const USERS_ERROR = 'Falhou carregar as reações.';
const MORE_SUFFIX = (n: number) => `+${n}`;

const CHIP_CLASSES =
    'group/chip inline-flex items-center gap-1.5 rounded-full border px-3 py-1.5 text-sm transition-colors ' +
    'border-zinc-800 bg-zinc-900/40 text-zinc-300 hover:border-zinc-700 ' +
    'data-[mine]:border-sky-500/60 data-[mine]:bg-sky-500/15 data-[mine]:text-sky-200 ' +
    'data-[pending]:opacity-60';

export function ReactionsStrip({ checkInId, reactions }: ReactionsStripProps) {
    const [expanded, setExpanded] = useState(false);
    const toggle = checkInHooks.useToggleReaction();
    const users = checkInHooks.useReactionUsers(checkInId, expanded);

    const totalReactions = Object.values(reactions.counts).reduce(
        (acc, n) => acc + n,
        0,
    );

    return (
        <section
            data-testid="reactions-strip"
            className="flex flex-col gap-3"
        >
            <div className="flex flex-wrap gap-2">
                {REACTION_PALETTE.map((emoji) => {
                    const count = reactions.counts[emoji] ?? 0;
                    const mine = reactions.mine.includes(emoji);
                    return (
                        <button
                            key={emoji}
                            type="button"
                            disabled={toggle.isPending}
                            onClick={() => toggle.mutate({ checkInId, emoji })}
                            aria-pressed={mine}
                            aria-label={`${emoji} (${count})`}
                            data-mine={mine || undefined}
                            data-pending={toggle.isPending || undefined}
                            className={CHIP_CLASSES}
                        >
                            <span className="text-base leading-none">{emoji}</span>
                            {count > 0 && (
                                <span className="font-mono text-xs text-zinc-300">
                                    {count}
                                </span>
                            )}
                        </button>
                    );
                })}
            </div>

            {totalReactions > 0 && (
                <button
                    type="button"
                    onClick={() => setExpanded((v) => !v)}
                    aria-expanded={expanded}
                    data-expanded={expanded || undefined}
                    className="group/disclose inline-flex w-fit items-center gap-1.5 text-xs text-zinc-400 transition-colors hover:text-zinc-200"
                >
                    <Users className="h-3.5 w-3.5" />
                    {SHOW_USERS_LABEL}
                    <ChevronDown className="h-3 w-3 transition-transform group-data-[expanded]/disclose:rotate-180" />
                </button>
            )}

            {expanded && (
                <WhoReactedPanel
                    status={users.status}
                    data={users.data ?? null}
                    onRetry={() => users.refetch()}
                />
            )}
        </section>
    );
}

interface WhoReactedPanelProps {
    status: 'pending' | 'error' | 'success';
    data: {
        counts: Record<string, number>;
        users: Record<string, Array<{ id: string; username: string; avatarUrl: string | null }>>;
    } | null;
    onRetry: () => void;
}

const USERS_PER_EMOJI_CAP = 20;

function WhoReactedPanel({ status, data, onRetry }: WhoReactedPanelProps) {
    if (status === 'pending') {
        return (
            <p
                data-testid="who-reacted-loading"
                className="text-xs text-zinc-500"
            >
                {USERS_LOADING}
            </p>
        );
    }

    if (status === 'error' || !data) {
        return (
            <div
                data-testid="who-reacted-error"
                className="flex items-center gap-3 rounded-2xl border border-zinc-800 bg-zinc-900/40 p-3 text-xs text-zinc-300"
            >
                <span>{USERS_ERROR}</span>
                <button
                    type="button"
                    onClick={onRetry}
                    className="rounded-full border border-sky-500/40 bg-sky-500/10 px-3 py-1 text-xs font-semibold text-sky-300"
                >
                    Tentar de novo
                </button>
            </div>
        );
    }

    const emojisWithReactions = Object.keys(data.counts).filter(
        (emoji) => (data.counts[emoji] ?? 0) > 0,
    );

    return (
        <div
            data-testid="who-reacted-panel"
            className="flex flex-col gap-3 rounded-2xl border border-zinc-800 bg-zinc-900/40 p-4"
        >
            {emojisWithReactions.map((emoji) => {
                const users = data.users[emoji] ?? [];
                const overflow = Math.max(
                    0,
                    (data.counts[emoji] ?? 0) - Math.min(users.length, USERS_PER_EMOJI_CAP),
                );
                return (
                    <div
                        key={emoji}
                        className="flex flex-col gap-2"
                        data-testid={`who-reacted-${emoji}`}
                    >
                        <div className="flex items-center gap-2 text-xs font-semibold text-zinc-300">
                            <span className="text-base">{emoji}</span>
                            <span className="font-mono text-zinc-500">
                                {data.counts[emoji]}
                            </span>
                        </div>
                        <div className="flex flex-wrap gap-1.5">
                            {users.map((u) => (
                                <span
                                    key={u.id}
                                    className="rounded-full border border-zinc-800 bg-zinc-900/60 px-2 py-0.5 text-[11px] text-zinc-300"
                                >
                                    @{u.username}
                                </span>
                            ))}
                            {overflow > 0 && (
                                <span
                                    className="rounded-full border border-dashed border-zinc-700 px-2 py-0.5 text-[11px] text-zinc-500"
                                    data-testid={`who-reacted-${emoji}-overflow`}
                                >
                                    {MORE_SUFFIX(overflow)}
                                </span>
                            )}
                        </div>
                    </div>
                );
            })}
        </div>
    );
}
