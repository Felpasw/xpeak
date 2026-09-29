import { LiquidWordmark } from '@/components/atoms/LiquidWordmark';

interface PageHeaderProps {
    title: string;
    description?: string;
    width?: number;
}

const GLYPH_PIXEL_WIDTH = 24;

export function PageHeader({ title, description, width }: PageHeaderProps) {
    const renderedWidth = width ?? title.length * GLYPH_PIXEL_WIDTH;

    return (
        <header className="flex flex-col gap-2">
            <LiquidWordmark text={title} align="start" width={renderedWidth} />
            {description ? <p className="text-sm text-zinc-400">{description}</p> : null}
        </header>
    );
}
