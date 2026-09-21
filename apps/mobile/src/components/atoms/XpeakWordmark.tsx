import { cn } from '@/lib/utils';

interface XpeakWordmarkProps {
    emph?: string;
    rest?: string;
    emphSize?: number;
    restSize?: number;
    className?: string;
}

const FONT_FAMILY = '"Orbitron", "Arial Black", sans-serif';
const STROKE_STYLE = {
    color: 'transparent',
    WebkitTextStroke: '1px #ffffff',
} as const;

export function XpeakWordmark({
    emph = 'X',
    rest = 'PEAK',
    emphSize = 96,
    restSize = 32,
    className,
}: XpeakWordmarkProps) {
    return (
        <span
            aria-hidden="true"
            className={cn(
                'relative inline-flex items-center font-bold leading-none tracking-tight',
                className,
            )}
            style={{ fontFamily: FONT_FAMILY }}
        >
            <span style={{ ...STROKE_STYLE, fontSize: `${emphSize}px` }}>{emph}</span>
            <span
                style={{
                    ...STROKE_STYLE,
                    fontSize: `${restSize}px`,
                    marginLeft: `-${emphSize * 0.35}px`,
                }}
            >
                {rest}
            </span>
        </span>
    );
}
