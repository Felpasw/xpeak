'use client';

import type { Category } from '@xpeak/shared';
import {
    Controller,
    type Control,
    type FieldValues,
    type Path,
} from 'react-hook-form';

import { CategoryPicker } from '@/components/atoms/CategoryPicker';

interface ControlledCategoryPickerProps<T extends FieldValues> {
    name: Path<T>;
    control: Control<T>;
    categories: Category[];
    isLoading: boolean;
}

export function ControlledCategoryPicker<T extends FieldValues>({
    name,
    control,
    categories,
    isLoading,
}: ControlledCategoryPickerProps<T>) {
    return (
        <Controller
            name={name}
            control={control}
            render={({ field, fieldState }) => (
                <CategoryPicker
                    categories={categories}
                    isLoading={isLoading}
                    selectedId={(field.value as string | undefined) ?? ''}
                    onSelect={(id) => field.onChange(id)}
                    errorMessage={fieldState.error?.message}
                />
            )}
        />
    );
}
