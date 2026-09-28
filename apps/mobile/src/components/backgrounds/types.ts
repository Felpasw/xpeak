import type { ComponentType } from 'react';

/**
 * A background is identified by a stable `slug` (persisted per user
 * once Phase 10 wires the profile-customization picker) and rendered
 * via `Component`. The component should absolutely fill its parent
 * (`h-full w-full`) and stay `pointer-events-none` so it never eats
 * clicks. Optional `className` prop lets the consumer add extra
 * positioning classes.
 *
 * `preview` is optional — a small still image URL the picker grid
 * can render as a thumbnail. Animated backgrounds may skip it and
 * render a live mini-instance in the picker instead.
 */
export interface BackgroundProps {
    className?: string;
}

export interface BackgroundManifest {
    slug: string;
    name: string;
    description?: string;
    preview?: string;
    Component: ComponentType<BackgroundProps>;
}
