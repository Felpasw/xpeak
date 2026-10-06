'use client';

import { useCallback, useMemo, useRef, useState } from 'react';

import { useDismiss } from '@/components/atoms/hooks/useDismiss';
import { useMonthView } from '@/components/atoms/hooks/useMonthView';
import { isOutOfRange, todayIso } from '@/components/atoms/utils/date';

interface UseDatePickerPopoverArgs {
    value: string;
    min?: string;
    max?: string;
    disabled?: boolean;
    onChange: (next: string) => void;
}

export function useDatePickerPopover({
    value,
    min,
    max,
    disabled = false,
    onChange,
}: UseDatePickerPopoverArgs) {
    const [open, setOpen] = useState(false);
    const containerRef = useRef<HTMLDivElement>(null);
    const triggerRef = useRef<HTMLButtonElement>(null);

    const today = useMemo(() => todayIso(), []);
    const anchor = value || today;

    const month = useMonthView({ initialIso: anchor, min, max });

    const close = useCallback(() => {
        setOpen(false);
        triggerRef.current?.focus();
    }, []);

    useDismiss({ open, scope: containerRef, onDismiss: close });

    const toggle = useCallback(() => {
        if (disabled) return;
        setOpen((previous) => {
            if (!previous) month.jumpTo(value || today);
            return !previous;
        });
    }, [disabled, month, value, today]);

    const commit = useCallback(
        (iso: string) => {
            onChange(iso);
            close();
        },
        [onChange, close],
    );

    const pickToday = useCallback(() => {
        if (isOutOfRange(today, min, max)) return;
        month.jumpTo(today);
        commit(today);
    }, [today, min, max, month, commit]);

    return {
        open,
        containerRef,
        triggerRef,
        today,
        todayDisabled: isOutOfRange(today, min, max),
        month,
        toggle,
        commit,
        pickToday,
    };
}
