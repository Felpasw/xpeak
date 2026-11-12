export function ChallengeActivitySkeleton() {
    return (
        <div data-testid="challenge-activity-skeleton" className="flex flex-col gap-2">
            <div className="h-14 animate-pulse rounded-xl bg-zinc-800/60" />
            <div className="h-14 animate-pulse rounded-xl bg-zinc-800/60" />
        </div>
    );
}
