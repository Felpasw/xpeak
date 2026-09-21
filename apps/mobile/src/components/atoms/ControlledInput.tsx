'use client';

import type { InputHTMLAttributes } from 'react';
import {
    Controller,
    type Control,
    type FieldValues,
    type Path,
} from 'react-hook-form';

import { Input } from '@/components/atoms/Input';

interface ControlledInputProps<T extends FieldValues>
    extends Omit<InputHTMLAttributes<HTMLInputElement>, 'name' | 'value' | 'onChange'> {
    name: Path<T>;
    control: Control<T>;
    label: string;
    showPasswordToggle?: boolean;
    transform?: (value: string) => string;
}

export function ControlledInput<T extends FieldValues>({
    name,
    control,
    label,
    showPasswordToggle,
    transform,
    ...rest
}: ControlledInputProps<T>) {
    return (
        <Controller
            name={name}
            control={control}
            render={({ field, fieldState }) => (
                <div className="space-y-2">
                    <Input
                        {...rest}
                        {...field}
                        label={label}
                        value={(field.value as string | undefined) ?? ''}
                        onChange={(event) => {
                            const raw = event.target.value;
                            field.onChange(transform ? transform(raw) : raw);
                        }}
                        showPasswordToggle={showPasswordToggle}
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
