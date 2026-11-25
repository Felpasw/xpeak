'use client';

import { formatDistanceToNow, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { ArrowLeft } from 'lucide-react';
import Image from 'next/image';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense } from 'react';

import { CheckinMediaHero } from '@/components/atoms/CheckinMediaHero';
import { XpBreakdown } from '@/components/atoms/XpBreakdown';
import { CommentsSection } from '@/components/organisms/CommentsSection';
import { ReactionsStrip } from '@/components/organisms/ReactionsStrip';
import authHooks from '@/hooks/useAuth';
import checkInHooks from '@/hooks/useCheckIn';

const BACK_LABEL = 'Voltar';
const LOADING_COPY = 'Carregando publicação…';
const ERROR_COPY = 'Não achei essa publicação.';

function CheckinDetailBody() {
    const searchParams = useSearchParams();
    const router = useRouter();
    const id = searchParams.get('id');
    const detail = checkInHooks.useDetail(id);
    const { me } = authHooks.use();
    const viewerId = me.data?.id ?? '';

    if (!id) {
        return (
            <main className="flex flex-1 flex-col gap-4 px-6 pt-10 pb-48">
                <p className="text-sm text-zinc-400">{ERROR_COPY}</p>
            </main>
        );
    }

    if (detail.isPending) {
        return (
            <main className="flex flex-1 flex-col gap-4 px-6 pt-10 pb-48">
                <p className="text-xs text-zinc-500">{LOADING_COPY}</p>
                <div className="aspect-square w-full animate-pulse rounded-2xl border border-zinc-800 bg-zinc-900/40" />
            </main>
        );
    }

    if (detail.isError || !detail.data) {
        return (
            <main className="flex flex-1 flex-col gap-4 px-6 pt-10 pb-48">
                <button
                    type="button"
                    onClick={() => router.back()}
                    className="inline-flex w-fit items-center gap-2 text-xs font-semibold text-sky-300"
                >
                    <ArrowLeft className="h-3.5 w-3.5" />
                    {BACK_LABEL}
                </button>
                <p className="text-sm text-zinc-400">{ERROR_COPY}</p>
            </main>
        );
    }

    const checkIn = detail.data;
    const when = formatDistanceToNow(parseISO(checkIn.performedAt), {
        addSuffix: true,
        locale: ptBR,
    });
    const initials = checkIn.author.username.slice(0, 2).toUpperCase();
    const profileHref = `/u?username=${encodeURIComponent(checkIn.author.username)}`;

    return (
        <main className="flex flex-1 flex-col gap-6 px-6 pt-10 pb-48">
            <button
                type="button"
                onClick={() => router.back()}
                className="inline-flex w-fit items-center gap-2 text-xs font-semibold text-sky-300 transition-colors hover:text-sky-200"
            >
                <ArrowLeft className="h-3.5 w-3.5" />
                {BACK_LABEL}
            </button>

            <header className="flex items-center gap-3">
                <Link
                    href={profileHref}
                    aria-label={`Ver perfil de @${checkIn.author.username}`}
                    className="relative flex h-11 w-11 shrink-0 items-center justify-center overflow-hidden rounded-full bg-gradient-to-br from-zinc-800 to-zinc-900 text-sm font-semibold text-sky-100 transition hover:ring-2 hover:ring-sky-400/60"
                >
                    {checkIn.author.avatarUrl ? (
                        <Image
                            src={checkIn.author.avatarUrl}
                            alt=""
                            fill
                            unoptimized
                            sizes="44px"
                            className="object-cover"
                        />
                    ) : (
                        initials
                    )}
                </Link>
                <div className="flex min-w-0 flex-1 flex-col gap-0.5">
                    <Link
                        href={profileHref}
                        className="truncate text-sm font-semibold text-zinc-100 hover:text-sky-200"
                    >
                        @{checkIn.author.username}
                    </Link>
                    <span className="text-[11px] text-zinc-500">{when}</span>
                </div>
            </header>

            <CheckinMediaHero
                mediaPreview={checkIn.mediaPreview}
                title={checkIn.title}
            />

            <section className="flex flex-col gap-2">
                <h1 className="text-xl font-semibold tracking-tight text-zinc-100">
                    {checkIn.title}
                </h1>
                {checkIn.notes && (
                    <p className="whitespace-pre-wrap break-words text-sm text-zinc-300">
                        {checkIn.notes}
                    </p>
                )}
            </section>

            <XpBreakdown
                scoring={checkIn.scoringSnapshot}
                xpEarned={checkIn.xpEarned}
            />

            <ReactionsStrip
                checkInId={checkIn.id}
                reactions={checkIn.reactions}
            />

            <CommentsSection
                checkInId={checkIn.id}
                currentUserId={viewerId}
                checkInOwnerId={checkIn.author.id}
            />
        </main>
    );
}

export default function CheckinDetailPage() {
    return (
        <Suspense fallback={null}>
            <CheckinDetailBody />
        </Suspense>
    );
}
