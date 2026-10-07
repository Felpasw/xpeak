const GHOST_COUNT = 3;

export function CheckinListSkeleton() {
    return (
        <div data-testid="checkin-list-skeleton" className="flex flex-col gap-3">
            {Array.from({ length: GHOST_COUNT }).map((_, i) => (
                <div
                    key={i}
                    className="flex items-center gap-4 rounded-2xl border border-zinc-800 bg-zinc-900/40 p-4"
                    aria-hidden="true"
                >
                    <div className="h-16 w-16 shrink-0 animate-pulse rounded-xl bg-zinc-800" />
                    <div className="flex min-w-0 flex-1 flex-col gap-2">
                        <div className="h-3 w-2/3 animate-pulse rounded bg-zinc-800" />
                        <div className="h-2 w-1/2 animate-pulse rounded bg-zinc-800" />
                    </div>
                    <div className="h-6 w-14 shrink-0 animate-pulse rounded-full bg-zinc-800" />
                </div>
            ))}
        </div>
    );
}
