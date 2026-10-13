interface ProgressBarProps {
    value: number;
    target: number;
    achieved?: boolean;
}

const clampPercent = (value: number, target: number): number => {
    if (target <= 0) {
        return 0;
    }
    const raw = (value / target) * 100;
    if (raw < 0) {
        return 0;
    }
    if (raw > 100) {
        return 100;
    }
    return raw;
};

export function ProgressBar({ value, target, achieved = false }: ProgressBarProps) {
    const percent = clampPercent(value, target);
    const barColor = achieved ? 'bg-emerald-400' : 'bg-sky-400';
    return (
        <div className="flex flex-col gap-1.5">
            <div className="flex items-baseline justify-between font-mono text-xs text-zinc-400">
                <span>{value.toLocaleString('pt-BR')} XP</span>
                <span>{target.toLocaleString('pt-BR')} XP</span>
            </div>
            <div
                role="progressbar"
                aria-valuenow={value}
                aria-valuemin={0}
                aria-valuemax={target}
                className="relative h-2 w-full overflow-hidden rounded-full bg-zinc-800"
            >
                <div
                    className={`absolute inset-y-0 left-0 ${barColor} transition-[width] duration-300`}
                    style={{ width: `${percent}%` }}
                />
            </div>
            <p className="text-right text-xs font-semibold text-zinc-300">
                {percent.toFixed(0)}%
            </p>
        </div>
    );
}
