'use client';

import { formatDistanceToNow, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { Dumbbell, Heart, MessageCircle } from 'lucide-react';
import { motion } from 'motion/react';
import Image from 'next/image';
import Link from 'next/link';

import type { ChallengeCheckInFeedEntry } from '@xpeak/shared';

import checkInHooks from '@/hooks/useCheckIn';

const QUICK_EMOJI = '🔥';

interface ChallengeActivityRowProps {
    entry: ChallengeCheckInFeedEntry;
}

export function ChallengeActivityRow({ entry }: ChallengeActivityRowProps) {
    const { checkIn, author } = entry;
    const initials = author.username.slice(0, 2).toUpperCase();
    const when = formatDistanceToNow(parseISO(checkIn.performedAt), {
        addSuffix: true,
        locale: ptBR,
    });
    const multipliers = checkIn.scoringSnapshot.multipliers;
    const hasMultipliers = multipliers.length > 0;
    const profileHref = `/u?username=${encodeURIComponent(author.username)}`;
    const detailHref = `/checkin/view?id=${checkIn.id}`;
    const reactionCount = Object.values(checkIn.reactions.counts).reduce(
        (acc, n) => acc + n,
        0,
    );
    const quickLiked = checkIn.reactions.mine.includes(QUICK_EMOJI);

    const toggle = checkInHooks.useToggleReaction();

    const handleQuickReact = (e: React.MouseEvent) => {
        e.preventDefault();
        e.stopPropagation();
        toggle.mutate({ checkInId: checkIn.id, emoji: QUICK_EMOJI });
    };

    return (
        <li className="flex flex-col overflow-hidden rounded-2xl border border-zinc-800 bg-zinc-900/40">
            <header className="flex items-center gap-3 px-4 pt-4">
                <Link
                    href={profileHref}
                    aria-label={`Ver perfil de @${author.username}`}
                    className="relative flex h-10 w-10 shrink-0 items-center justify-center overflow-hidden rounded-full bg-gradient-to-br from-zinc-800 to-zinc-900 text-sm font-semibold text-sky-100 transition hover:ring-2 hover:ring-sky-400/60"
                >
                    {author.avatarUrl ? (
                        <Image
                            src={author.avatarUrl}
                            alt=""
                            fill
                            unoptimized
                            sizes="40px"
                            className="object-cover"
                        />
                    ) : (
                        initials
                    )}
                </Link>
                <div className="flex min-w-0 flex-1 flex-col gap-0.5">
                    <div className="flex items-center gap-2">
                        <Link
                            href={profileHref}
                            className="truncate text-sm font-semibold text-zinc-100 hover:text-sky-200"
                        >
                            @{author.username}
                        </Link>
                        <span className="shrink-0 rounded-full border border-sky-500/40 bg-sky-500/10 px-2 py-0.5 font-mono text-[10px] font-semibold text-sky-200">
                            Level {author.level}
                        </span>
                    </div>
                    <span className="truncate text-[11px] text-zinc-500">{when}</span>
                </div>
            </header>

            {checkIn.mediaPreview ? (
                <div
                    data-testid="challenge-activity-media"
                    className="relative mt-3 aspect-square w-full overflow-hidden border-y border-zinc-800 bg-black"
                >
                    <Image
                        src={checkIn.mediaPreview.thumbUrl}
                        alt={`Mídia do check-in ${checkIn.title}`}
                        fill
                        unoptimized
                        sizes="(max-width: 480px) 100vw, 480px"
                        className="object-cover"
                    />
                </div>
            ) : (
                <div
                    data-testid="challenge-activity-media-placeholder"
                    className="mt-3 flex aspect-square w-full items-center justify-center border-y border-zinc-800 bg-zinc-900/60 text-zinc-600"
                    aria-hidden="true"
                >
                    <Dumbbell className="h-16 w-16" />
                </div>
            )}

            <footer className="flex flex-col gap-2 px-4 py-3">
                <div className="flex items-start justify-between gap-3">
                    <h3 className="min-w-0 flex-1 truncate text-sm font-semibold text-zinc-100">
                        {checkIn.title}
                    </h3>
                    <span className="shrink-0 rounded-full border border-sky-500/40 bg-sky-500/10 px-3 py-1 font-mono text-xs font-semibold text-sky-200">
                        +{checkIn.xpEarned} XP
                    </span>
                </div>
                <div className="flex items-center gap-4 text-xs text-zinc-400">
                    <motion.button
                        type="button"
                        onClick={handleQuickReact}
                        disabled={toggle.isPending}
                        aria-label={`Reagir com ${QUICK_EMOJI}`}
                        aria-pressed={quickLiked}
                        data-testid="challenge-activity-reactions"
                        data-mine={quickLiked || undefined}
                        data-pending={toggle.isPending || undefined}
                        whileTap={{ scale: 1.3 }}
                        transition={{ type: 'spring', stiffness: 500, damping: 15 }}
                        className="inline-flex items-center gap-1 rounded-full px-1 transition-colors data-[mine]:text-sky-300 data-[pending]:opacity-60"
                    >
                        <Heart className="h-4 w-4" />
                        {reactionCount}
                    </motion.button>
                    <Link
                        href={detailHref}
                        aria-label="Ver comentários"
                        data-testid="challenge-activity-comments"
                        className="inline-flex items-center gap-1 rounded-full px-1 transition-colors hover:text-zinc-200"
                    >
                        <MessageCircle className="h-4 w-4" />
                        {checkIn.commentsCount}
                    </Link>
                </div>
                {hasMultipliers && (
                    <p
                        data-testid="challenge-activity-breakdown"
                        className="font-mono text-[11px] text-zinc-500"
                    >
                        <span className="text-zinc-400">{checkIn.scoringSnapshot.baseXp} base</span>
                        {multipliers.map((m) => (
                            <span key={`${m.source}-${m.value}`}>
                                {' · '}
                                <span className="text-sky-300">
                                    {Number.isInteger(m.value)
                                        ? `${m.value}×`
                                        : `${m.value.toFixed(2).replace(/0+$/, '').replace(/\.$/, '')}×`}
                                </span>{' '}
                                {m.source}
                            </span>
                        ))}
                    </p>
                )}
                {checkIn.notes && (
                    <p className="whitespace-pre-wrap break-words text-xs text-zinc-400">
                        {checkIn.notes}
                    </p>
                )}
            </footer>
        </li>
    );
}
