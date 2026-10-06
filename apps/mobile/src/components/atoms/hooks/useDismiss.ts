'use client';

import { useEffect, type RefObject } from 'react';

interface UseDismissArgs {
    open: boolean;
    scope: RefObject<HTMLElement | null>;
    onDismiss: () => void;
}

export function useDismiss({ open, scope, onDismiss }: UseDismissArgs) {
    useEffect(() => {
        if (!open) return;

        const onPointer = (event: MouseEvent) => {
            if (scope.current && !scope.current.contains(event.target as Node)) {
                onDismiss();
            }
        };
        const onKey = (event: KeyboardEvent) => {
            if (event.key === 'Escape') onDismiss();
        };

        document.addEventListener('mousedown', onPointer);
        document.addEventListener('keydown', onKey);
        return () => {
            document.removeEventListener('mousedown', onPointer);
            document.removeEventListener('keydown', onKey);
        };
    }, [open, scope, onDismiss]);
}
