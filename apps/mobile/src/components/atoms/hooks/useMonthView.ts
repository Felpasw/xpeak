'use client';

import { useCallback, useMemo, useState } from 'react';

import {
    buildMonthCells,
    isMonthOutOfRange,
    parseIso,
    shiftMonth,
    type DayCell,
    type MonthView,
} from '@/components/atoms/utils/date';

interface UseMonthViewArgs {
    initialIso: string;
    min?: string;
    max?: string;
}

interface UseMonthViewResult {
    view: MonthView;
    cells: DayCell[];
    prevDisabled: boolean;
    nextDisabled: boolean;
    shift: (delta: -1 | 1) => void;
    jumpTo: (iso: string) => void;
}

export function useMonthView({ initialIso, min, max }: UseMonthViewArgs): UseMonthViewResult {
    const [view, setView] = useState<MonthView>(() => {
        const { year, month } = parseIso(initialIso);
        return { year, month };
    });

    const cells = useMemo(() => buildMonthCells(view), [view]);
    const prevDisabled = useMemo(
        () => isMonthOutOfRange(shiftMonth(view, -1), min, max),
        [view, min, max],
    );
    const nextDisabled = useMemo(
        () => isMonthOutOfRange(shiftMonth(view, 1), min, max),
        [view, min, max],
    );

    const shift = useCallback((delta: -1 | 1) => {
        setView((current) => shiftMonth(current, delta));
    }, []);

    const jumpTo = useCallback((iso: string) => {
        const { year, month } = parseIso(iso);
        setView({ year, month });
    }, []);

    return { view, cells, prevDisabled, nextDisabled, shift, jumpTo };
}
