'use client';

import type { Category } from '@xpeak/shared';
import { Loader2 } from 'lucide-react';

interface CategoryPickerProps {
    categories: Category[];
    isLoading: boolean;
    selectedId: string;
    onSelect: (id: string) => void;
    errorMessage?: string;
}

const NO_CATEGORIES = 'Ainda não há categorias neste grupo.';
const GROUP_LABEL = 'Categoria';

const BUTTON_CLASSES =
    'flex flex-col items-center gap-2 rounded-2xl border p-4 text-sm transition-colors ' +
    'border-zinc-800 bg-zinc-900/60 text-zinc-300 hover:border-zinc-700 ' +
    'aria-checked:border-sky-400 aria-checked:bg-sky-500/10 aria-checked:text-sky-100';

function LoadingState() {
    return (
        <div className="flex items-center justify-center py-12 text-sky-300">
            <Loader2 className="h-6 w-6 animate-spin" />
        </div>
    );
}

function EmptyState() {
    return (
        <p className="rounded-2xl border border-zinc-800 bg-zinc-900/60 px-4 py-6 text-center text-sm text-zinc-400">
            {NO_CATEGORIES}
        </p>
    );
}

function ErrorMessage({ message }: { message: string }) {
    return (
        <p role="alert" className="text-xs font-medium text-red-400">
            {message}
        </p>
    );
}

export function CategoryPicker({
    categories,
    isLoading,
    selectedId,
    onSelect,
    errorMessage,
}: CategoryPickerProps) {
    if (isLoading) {
        return <LoadingState />;
    }

    if (categories.length === 0) {
        return <EmptyState />;
    }

    return (
        <div className="flex flex-col gap-2">
            <div role="radiogroup" aria-label={GROUP_LABEL} className="grid grid-cols-2 gap-3">
                {categories.map((c) => (
                    <button
                        type="button"
                        key={c.id}
                        role="radio"
                        aria-checked={selectedId === c.id}
                        onClick={() => onSelect(c.id)}
                        className={BUTTON_CLASSES}
                    >
                        <span className="font-medium">{c.name}</span>
                    </button>
                ))}
            </div>
            {errorMessage ? <ErrorMessage message={errorMessage} /> : null}
        </div>
    );
}
