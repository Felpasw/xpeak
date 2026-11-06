import type { Backgrounds } from '@xpeak/shared';

import { getBackground } from '@/components/backgrounds/registry';

/**
 * Renders the manifest Component for the given slug as a full-bleed
 * layer that obscures any ambient chrome the layout paints (the
 * default starfield). Caller owns the slug — this is the profile
 * owner's background, not the authenticated viewer's. Null or
 * undefined falls back to <c>DEFAULT_BACKGROUND</c>. Must be dropped
 * inside a `relative` container so the `absolute inset-0`
 * positioning resolves against that box, with the page content
 * placed on a `relative z-10` sibling so it renders above the layer.
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
            className={`pointer-events-none absolute inset-0 z-0 ${className ?? ''}`.trim()}
        />
    );
}
