'use client';

import { formatDistanceToNow } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { Dumbbell, Heart, MessageCircle } from 'lucide-react';
import { motion } from 'motion/react';
import Image from 'next/image';
import Link from 'next/link';
import type { CheckIn } from '@xpeak/shared';

import checkInHooks from '@/hooks/useCheckIn';

interface CheckinCardProps {
    checkIn: CheckIn;
}

const QUICK_EMOJI = '🔥';

export function CheckinCard({ checkIn }: CheckinCardProps) {
    const when = formatDistanceToNow(new Date(checkIn.performedAt), {
        addSuffix: true,
        locale: ptBR,
    });
    const reactionCount = Object.values(checkIn.reactions.counts).reduce(
        (acc, n) => acc + n,
        0,
    );
    const quickLiked = checkIn.reactions.mine.includes(QUICK_EMOJI);
    const detailHref = `/checkin/view?id=${checkIn.id}`;

    const toggle = checkInHooks.useToggleReaction();

    const handleQuickReact = (e: React.MouseEvent) => {
        e.preventDefault();
        e.stopPropagation();
        toggle.mutate({ checkInId: checkIn.id, emoji: QUICK_EMOJI });
    };

    return (
        <article className="relative flex items-center gap-4 rounded-2xl border border-zinc-800 bg-zinc-900/40 p-4 transition-colors hover:border-zinc-700">
            <Link
                href={detailHref}
                aria-label={`Abrir detalhes do check-in ${checkIn.title}`}
                className="absolute inset-0 z-0 rounded-2xl"
            />

            {checkIn.mediaPreview ? (
                <div className="relative z-10 h-16 w-16 shrink-0 overflow-hidden rounded-xl border border-zinc-800">
                    <Image
                        src={checkIn.mediaPreview.thumbUrl}
                        alt={`Mídia do check-in ${checkIn.title}`}
                        fill
                        unoptimized
                        sizes="64px"
                        className="pointer-events-none object-cover"
                    />
                </div>
            ) : (
                <div
                    data-testid="checkin-card-placeholder"
                    className="relative z-10 flex h-16 w-16 shrink-0 items-center justify-center rounded-xl border border-zinc-800 bg-zinc-900/60 text-zinc-500"
                    aria-hidden="true"
                >
                    <Dumbbell className="h-6 w-6" />
                </div>
            )}

            <div className="relative z-10 flex min-w-0 flex-1 flex-col gap-1">
                <h3 className="truncate text-sm font-semibold text-zinc-100">
                    {checkIn.title}
                </h3>
                <p className="truncate text-xs text-zinc-400">{when}</p>
                {checkIn.notes && (
                    <p className="truncate text-xs text-zinc-500">{checkIn.notes}</p>
                )}
                <div className="mt-1 flex items-center gap-3 text-xs text-zinc-400">
                    <motion.button
                        type="button"
                        onClick={handleQuickReact}
                        disabled={toggle.isPending}
                        aria-label={`Reagir com ${QUICK_EMOJI}`}
                        aria-pressed={quickLiked}
                        data-testid="checkin-card-react"
                        data-mine={quickLiked || undefined}
                        data-pending={toggle.isPending || undefined}
                        whileTap={{ scale: 1.3 }}
                        transition={{ type: 'spring', stiffness: 500, damping: 15 }}
                        className="inline-flex items-center gap-1 rounded-full px-1 transition-colors data-[mine]:text-sky-300 data-[pending]:opacity-60"
                    >
                        <Heart className="h-3.5 w-3.5 data-[mine]:fill-current group-data-[mine]/react:fill-current" />
                        {reactionCount}
                    </motion.button>
                    <Link
                        href={detailHref}
                        onClick={(e) => e.stopPropagation()}
                        aria-label="Ver comentários"
                        data-testid="checkin-card-comments"
                        className="relative z-10 inline-flex items-center gap-1 rounded-full px-1 transition-colors hover:text-zinc-200"
                    >
                        <MessageCircle className="h-3.5 w-3.5" />
                        {checkIn.commentsCount}
                    </Link>
                </div>
            </div>

            <span className="relative z-10 shrink-0 rounded-full border border-sky-500/40 bg-sky-500/10 px-3 py-1 font-mono text-xs font-semibold text-sky-300">
                +{checkIn.xpEarned} XP
            </span>
        </article>
    );
}
