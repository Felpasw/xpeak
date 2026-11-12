import type { ChallengeCheckInFeedEntry } from '@xpeak/shared';
import Link from 'next/link';

const DATE_FORMATTER = new Intl.DateTimeFormat('pt-BR', {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
});

interface ChallengeActivityRowProps {
    entry: ChallengeCheckInFeedEntry;
}

export function ChallengeActivityRow({ entry }: ChallengeActivityRowProps) {
    const { checkIn, author } = entry;
    const initials = author.username.slice(0, 2).toUpperCase();
    const when = DATE_FORMATTER.format(new Date(checkIn.performedAt));
    return (
        <li className="flex items-center gap-3 rounded-xl border border-zinc-800 bg-zinc-950/60 px-3 py-2">
            <Link
                href={`/u?username=${encodeURIComponent(author.username)}`}
                aria-label={`Ver perfil de @${author.username}`}
                className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-zinc-800 to-zinc-900 text-sm font-semibold text-sky-100 transition hover:ring-2 hover:ring-sky-400/60"
            >
                {initials}
            </Link>
            <div className="flex min-w-0 flex-1 flex-col gap-0.5">
                <div className="flex items-baseline gap-2">
                    <Link
                        href={`/u?username=${encodeURIComponent(author.username)}`}
                        className="truncate text-sm font-medium text-zinc-100 hover:text-sky-200"
                    >
                        @{author.username}
                    </Link>
                    <span className="font-mono text-[10px] text-zinc-500">{when}</span>
                </div>
                <span className="truncate text-sm text-zinc-300">{checkIn.title}</span>
            </div>
            <span className="shrink-0 rounded-full border border-sky-500/30 bg-sky-500/10 px-2 py-0.5 font-mono text-[11px] font-semibold text-sky-200">
                +{checkIn.xpEarned} XP
            </span>
        </li>
    );
}
