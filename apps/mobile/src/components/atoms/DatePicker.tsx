'use client';

import {
    Calendar as CalendarIcon,
    ChevronLeft,
    ChevronRight,
} from 'lucide-react';
import { AnimatePresence, motion, type Variants } from 'motion/react';

import { useDatePickerPopover } from '@/components/atoms/hooks/useDatePickerPopover';
import {
    formatDisplay,
    formatMonthLabel,
    isOutOfRange,
    type DayCell,
    type MonthView,
} from '@/components/atoms/utils/date';
import { cn } from '@/lib/utils';

interface DatePickerProps {
    label: string;
    value: string;
    onChange: (next: string) => void;
    min?: string;
    max?: string;
    disabled?: boolean;
    className?: string;
    'aria-invalid'?: boolean;
}

const WEEK_DAYS = ['D', 'S', 'T', 'Q', 'Q', 'S', 'S'];
const WEEK_DAYS_FULL = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'];

const labelContainer: Variants = {
    initial: {},
    animate: { transition: { staggerChildren: 0.05 } },
};

const labelLetter: Variants = {
    initial: { y: 0, color: 'var(--color-zinc-100)' },
    animate: {
        y: '-120%',
        color: 'var(--color-zinc-500)',
        transition: { type: 'spring', stiffness: 300, damping: 20 },
    },
};

export function DatePicker({
    label,
    value,
    onChange,
    min,
    max,
    disabled = false,
    className,
    'aria-invalid': ariaInvalid,
}: DatePickerProps) {
    const {
        open,
        containerRef,
        triggerRef,
        today,
        todayDisabled,
        month,
        toggle,
        commit,
        pickToday,
    } = useDatePickerPopover({ value, min, max, disabled, onChange });

    const showFloating = open || value.length > 0;
    const display = value ? formatDisplay(value) : '';

    return (
        <div ref={containerRef} className={cn('relative', className)}>
            <div className="relative">
                <motion.div
                    aria-hidden="true"
                    className="pointer-events-none absolute top-1/2 left-0 -translate-y-1/2 text-zinc-100"
                    variants={labelContainer}
                    initial="initial"
                    animate={showFloating ? 'animate' : 'initial'}
                >
                    {label.split('').map((char, index) => (
                        <motion.span
                            key={`${char}-${index}`}
                            className="inline-block text-sm"
                            variants={labelLetter}
                            style={{ willChange: 'transform' }}
                        >
                            {char === ' ' ? ' ' : char}
                        </motion.span>
                    ))}
                </motion.div>

                <button
                    ref={triggerRef}
                    type="button"
                    aria-label={label}
                    aria-haspopup="dialog"
                    aria-expanded={open}
                    data-invalid={ariaInvalid || undefined}
                    disabled={disabled}
                    onClick={toggle}
                    className={cn(
                        'flex min-h-[2.5rem] w-full items-center justify-between gap-3 border-b-2 border-sky-500 bg-transparent py-2 text-left text-base font-medium text-zinc-100 outline-none transition-colors',
                        'focus-visible:border-sky-400 disabled:opacity-50',
                        'data-[invalid=true]:border-red-500',
                    )}
                >
                    <span className="tabular-nums text-zinc-100">{display}</span>
                    <CalendarIcon
                        aria-hidden="true"
                        className={cn('h-5 w-5 shrink-0 transition-colors', open ? 'text-sky-300' : 'text-sky-400')}
                    />
                </button>
            </div>

            <AnimatePresence>
                {open ? (
                    <CalendarPopover
                        label={label}
                        view={month.view}
                        cells={month.cells}
                        value={value}
                        today={today}
                        min={min}
                        max={max}
                        prevDisabled={month.prevDisabled}
                        nextDisabled={month.nextDisabled}
                        todayDisabled={todayDisabled}
                        onShift={month.shift}
                        onPick={commit}
                        onPickToday={pickToday}
                    />
                ) : null}
            </AnimatePresence>
        </div>
    );
}

interface CalendarPopoverProps {
    label: string;
    view: MonthView;
    cells: DayCell[];
    value: string;
    today: string;
    min?: string;
    max?: string;
    prevDisabled: boolean;
    nextDisabled: boolean;
    todayDisabled: boolean;
    onShift: (delta: -1 | 1) => void;
    onPick: (iso: string) => void;
    onPickToday: () => void;
}

function CalendarPopover({
    label,
    view,
    cells,
    value,
    today,
    min,
    max,
    prevDisabled,
    nextDisabled,
    todayDisabled,
    onShift,
    onPick,
    onPickToday,
}: CalendarPopoverProps) {
    const monthLabel = formatMonthLabel(view);

    return (
        <motion.div
            role="dialog"
            aria-label={label}
            initial={{ opacity: 0, y: -4, scale: 0.98 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -4, scale: 0.98 }}
            transition={{ duration: 0.14, ease: 'easeOut' }}
            style={{ transformOrigin: 'top left' }}
            className="absolute top-full left-0 z-50 mt-3 w-[22rem] max-w-[calc(100vw-2rem)] overflow-hidden rounded-2xl border border-zinc-800/80 bg-zinc-950/95 shadow-2xl shadow-black/60 backdrop-blur-md"
        >
            <div className="flex items-center justify-between gap-2 border-b border-zinc-800/60 px-4 py-3">
                <NavButton direction="prev" disabled={prevDisabled} onClick={() => onShift(-1)} />
                <span aria-live="polite" className="text-sm font-semibold tracking-tight text-zinc-100">
                    {monthLabel}
                </span>
                <NavButton direction="next" disabled={nextDisabled} onClick={() => onShift(1)} />
            </div>

            <div className="px-3 pt-3 pb-2">
                <div className="mb-1 grid grid-cols-7">
                    {WEEK_DAYS.map((weekday, index) => (
                        <span
                            key={`wd-${index}`}
                            aria-label={WEEK_DAYS_FULL[index]}
                            className="flex h-8 items-center justify-center text-xs font-medium text-zinc-500"
                        >
                            {weekday}
                        </span>
                    ))}
                </div>

                <div role="grid" aria-label={monthLabel} className="grid grid-cols-7">
                    {cells.map((cell) =>
                        cell.day === null ? (
                            <span key={cell.key} aria-hidden="true" className="h-10 w-full" />
                        ) : (
                            <DayButton
                                key={cell.key}
                                day={cell.day}
                                selected={cell.iso === value}
                                today={cell.iso === today}
                                disabled={isOutOfRange(cell.iso, min, max)}
                                onClick={() => onPick(cell.iso)}
                            />
                        ),
                    )}
                </div>
            </div>

            <div className="flex items-center justify-end border-t border-zinc-800/60 px-3 py-2">
                <button
                    type="button"
                    onClick={onPickToday}
                    disabled={todayDisabled}
                    className="rounded-full px-3 py-1.5 text-xs font-semibold text-sky-400 transition-colors hover:bg-sky-500/10 hover:text-sky-300 disabled:cursor-not-allowed disabled:text-zinc-600 disabled:hover:bg-transparent"
                >
                    Hoje
                </button>
            </div>
        </motion.div>
    );
}

interface NavButtonProps {
    direction: 'prev' | 'next';
    disabled: boolean;
    onClick: () => void;
}

function NavButton({ direction, disabled, onClick }: NavButtonProps) {
    const isPrev = direction === 'prev';
    const Icon = isPrev ? ChevronLeft : ChevronRight;
    return (
        <button
            type="button"
            aria-label={isPrev ? 'Mês anterior' : 'Próximo mês'}
            onClick={onClick}
            disabled={disabled}
            className="flex h-8 w-8 items-center justify-center rounded-full text-zinc-400 transition-colors hover:bg-zinc-800/70 hover:text-zinc-100 disabled:cursor-not-allowed disabled:text-zinc-700 disabled:hover:bg-transparent"
        >
            <Icon className="h-4 w-4" strokeWidth={2.5} />
        </button>
    );
}

interface DayButtonProps {
    day: number;
    selected: boolean;
    today: boolean;
    disabled: boolean;
    onClick: () => void;
}

function DayButton({ day, selected, today, disabled, onClick }: DayButtonProps) {
    return (
        <div className="flex h-10 w-full items-center justify-center">
            <button
                type="button"
                role="gridcell"
                aria-selected={selected}
                aria-disabled={disabled || undefined}
                disabled={disabled}
                onClick={onClick}
                className={cn(
                    'relative flex h-9 w-9 items-center justify-center rounded-full text-sm font-medium tabular-nums transition-all',
                    'text-zinc-200 hover:bg-zinc-800/70 active:scale-95',
                    'disabled:cursor-not-allowed disabled:text-zinc-700 disabled:hover:bg-transparent',
                    'aria-selected:bg-sky-500 aria-selected:text-zinc-950 aria-selected:shadow-lg aria-selected:shadow-sky-500/30 aria-selected:hover:bg-sky-400',
                )}
            >
                {day}
                {today && !selected ? (
                    <span aria-hidden="true" className="absolute bottom-1 h-1 w-1 rounded-full bg-sky-400" />
                ) : null}
            </button>
        </div>
    );
}
