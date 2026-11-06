import type { Backgrounds } from '@xpeak/shared';
import type { ComponentType } from 'react';

/**
 * A background is identified by the shared <c>Backgrounds</c> enum
 * (backed by a native Postgres type on the backend) and rendered
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
    slug: Backgrounds;
    name: string;
    description?: string;
    preview?: string;
    Component: ComponentType<BackgroundProps>;
}
