import {
    addMonths,
    endOfMonth,
    format,
    getDay,
    getDaysInMonth,
    parseISO,
    startOfMonth,
} from 'date-fns';
import { ptBR } from 'date-fns/locale';

const ISO = 'yyyy-MM-dd';

export interface MonthView {
    year: number;
    month: number;
}

export type DayCell =
    | { key: string; day: null; iso: null }
    | { key: string; day: number; iso: string };

function viewToDate({ year, month }: MonthView): Date {
    return new Date(year, month, 1);
}

export function parseIso(iso: string) {
    const date = parseISO(iso);
    return { year: date.getFullYear(), month: date.getMonth(), day: date.getDate() };
}

export function toIso(year: number, month: number, day: number) {
    return format(new Date(year, month, day), ISO);
}

export function formatDisplay(iso: string) {
    return format(parseISO(iso), 'dd/MM/yyyy');
}

export function formatMonthLabel(view: MonthView) {
    const label = format(viewToDate(view), 'MMMM yyyy', { locale: ptBR });
    return label.charAt(0).toUpperCase() + label.slice(1);
}

export function todayIso() {
    return format(new Date(), ISO);
}

export function shiftMonth(view: MonthView, delta: -1 | 1): MonthView {
    const next = addMonths(viewToDate(view), delta);
    return { year: next.getFullYear(), month: next.getMonth() };
}

export function buildMonthCells(view: MonthView): DayCell[] {
    const first = startOfMonth(viewToDate(view));
    const total = getDaysInMonth(first);
    const offset = getDay(first);
    return Array.from({ length: 42 }, (_, index) => {
        const day = index - offset + 1;
        if (day < 1 || day > total) {
            return { key: `empty-${index}`, day: null, iso: null };
        }
        return { key: `day-${day}`, day, iso: toIso(view.year, view.month, day) };
    });
}

export function isOutOfRange(iso: string, min?: string, max?: string) {
    return (min !== undefined && iso < min) || (max !== undefined && iso > max);
}

export function isMonthOutOfRange(view: MonthView, min?: string, max?: string) {
    const anchor = viewToDate(view);
    const first = format(startOfMonth(anchor), ISO);
    const last = format(endOfMonth(anchor), ISO);
    return (min !== undefined && last < min) || (max !== undefined && first > max);
}
