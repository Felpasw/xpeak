'use client';

import type { ReactNode } from 'react';

import { getBackground } from '@/components/backgrounds/registry';
import authHooks from '@/hooks/useAuth';

/**
 * Resolves the authenticated user's picked background through the
 * shared `me` query cache and renders its manifest Component as a
 * positioned layer behind `children`. Falls back to
 * <c>DEFAULT_BACKGROUND</c> while `me` loads or for anonymous users
 * so the cold-open stays pixel-stable with the previous layout.
 */
export function UserBackground({ children }: { children: ReactNode }) {
    const { me } = authHooks.use();
    const { Component } = getBackground(me.data?.background);

    return (
        <div className="relative flex flex-1 flex-col overflow-hidden">
            <Component className="pointer-events-none absolute inset-0 -z-10" />
            {children}
        </div>
    );
}
