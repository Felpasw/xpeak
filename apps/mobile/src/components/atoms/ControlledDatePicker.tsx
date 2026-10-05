'use client';

import {
    Controller,
    type Control,
    type FieldValues,
    type Path,
} from 'react-hook-form';

import { DatePicker } from '@/components/atoms/DatePicker';

interface ControlledDatePickerProps<T extends FieldValues> {
    name: Path<T>;
    control: Control<T>;
    label: string;
    min?: string;
    max?: string;
    disabled?: boolean;
    className?: string;
}

export function ControlledDatePicker<T extends FieldValues>({
    name,
    control,
    label,
    min,
    max,
    disabled,
    className,
}: ControlledDatePickerProps<T>) {
    return (
        <Controller
            name={name}
            control={control}
            render={({ field, fieldState }) => (
                <div className="space-y-2">
                    <DatePicker
                        label={label}
                        value={(field.value as string | undefined) ?? ''}
                        onChange={(next) => field.onChange(next)}
                        min={min}
                        max={max}
                        disabled={disabled}
                        className={className}
                        aria-invalid={Boolean(fieldState.error)}
                    />
                    {fieldState.error ? (
                        <p role="alert" className="text-xs font-medium text-red-400">
                            {fieldState.error.message}
                        </p>
                    ) : null}
                </div>
            )}
        />
    );
}
