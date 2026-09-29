import type { LucideIcon } from 'lucide-react';

interface ComingSoonProps {
    Icon: LucideIcon;
}

const BADGE = 'Em breve';

export function ComingSoon({ Icon }: ComingSoonProps) {
    return (
        <div className="flex flex-1 flex-col items-center justify-center gap-6">
            <div className="flex h-20 w-20 items-center justify-center rounded-full border border-sky-400/40 bg-sky-500/10 text-sky-300 shadow-lg shadow-sky-500/20">
                <Icon className="h-8 w-8" strokeWidth={2} />
            </div>
            <p className="font-mono text-[11px] uppercase tracking-[0.35em] text-sky-400/60">
                {BADGE}
            </p>
        </div>
    );
}
