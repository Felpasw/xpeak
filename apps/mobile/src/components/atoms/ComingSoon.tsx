import type { LucideIcon } from 'lucide-react';

interface ComingSoonProps {
    title: string;
    Icon: LucideIcon;
    description?: string;
}

const BADGE = 'Coming soon';

export function ComingSoon({ title, Icon, description }: ComingSoonProps) {
    return (
        <main className="flex flex-1 flex-col items-center justify-center gap-6 px-6 pb-48">
            <div className="flex h-20 w-20 items-center justify-center rounded-full border border-sky-400/40 bg-sky-500/10 text-sky-300 shadow-lg shadow-sky-500/20">
                <Icon className="h-8 w-8" strokeWidth={2} />
            </div>
            <div className="flex flex-col items-center gap-2 text-center">
                <h1 className="text-2xl font-bold tracking-tight text-zinc-100">
                    {title}
                </h1>
                {description ? (
                    <p className="max-w-xs text-sm text-zinc-400">{description}</p>
                ) : null}
            </div>
            <p className="font-mono text-[11px] uppercase tracking-[0.35em] text-sky-400/60">
                {BADGE}
            </p>
        </main>
    );
}
