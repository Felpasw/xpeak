import type { ChallengeMemberRanking } from '@xpeak/shared';
import Link from 'next/link';

import { cn } from '@/lib/utils';
import { clampPercent } from '@/lib/progress';

interface ChallengeMemberRowProps {
    member: ChallengeMemberRanking;
    rank: number;
    isViewer: boolean;
}

export function ChallengeMemberRow({ member, rank, isViewer }: ChallengeMemberRowProps) {
    const { user, progress, tier } = member;
    const initials = user.username.slice(0, 2).toUpperCase();
    const percent = clampPercent(progress.value, progress.target);
    return (
        <li>
            <Link
                href={`/u?username=${encodeURIComponent(user.username)}`}
                data-viewer={isViewer}
                className={cn(
                    'flex items-center gap-3 rounded-xl border border-zinc-800 bg-zinc-950/60 px-3 py-2 transition hover:border-sky-500/50 hover:bg-zinc-900/60',
                    'data-[viewer=true]:border-sky-400/60 data-[viewer=true]:bg-sky-500/10',
                )}
            >
                <span className="w-6 text-right font-mono text-xs text-zinc-500">
                    {rank}
                </span>
                <span className="flex h-10 w-10 items-center justify-center rounded-full bg-gradient-to-br from-zinc-800 to-zinc-900 text-sm font-semibold text-sky-100">
                    {initials}
                </span>
                <div className="flex min-w-0 flex-1 flex-col gap-1">
                    <div className="flex items-center justify-between gap-2">
                        <span className="truncate text-sm font-medium text-zinc-100">
                            @{user.username}
                        </span>
                        {tier && (
                            <span className="rounded-full border border-sky-500/30 bg-sky-500/10 px-2 py-0.5 text-[10px] uppercase tracking-wider text-sky-200">
                                {tier.title}
                            </span>
                        )}
                    </div>
                    <div className="flex items-center gap-2">
                        <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-zinc-800">
                            <div
                                className="h-full bg-sky-400"
                                style={{ width: `${percent}%` }}
                            />
                        </div>
                        <span className="font-mono text-[10px] text-zinc-400">
                            {progress.value.toLocaleString('pt-BR')} /{' '}
                            {progress.target.toLocaleString('pt-BR')}
                        </span>
                    </div>
                </div>
            </Link>
        </li>
    );
}
