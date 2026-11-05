import type { Backgrounds } from '@xpeak/shared';

import { getBackground } from '@/components/backgrounds/registry';

/**
 * Renders the manifest Component for the given slug as a positioned
 * layer. The caller owns the slug — this is the profile owner's
 * background, not the authenticated viewer's. Null/undefined falls
 * back to <c>DEFAULT_BACKGROUND</c>. Meant to be dropped inside a
 * `relative` container that defines its own height.
 */
export function ProfileBackground({
    background,
    className,
}: {
    background: Backgrounds | null | undefined;
    className?: string;
}) {
    const { Component } = getBackground(background);
    return (
        <Component
            className={`pointer-events-none absolute inset-0 -z-10 ${className ?? ''}`.trim()}
        />
    );
}
