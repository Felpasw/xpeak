'use client';

import type { TextareaHTMLAttributes } from 'react';
import {
    Controller,
    type Control,
    type FieldValues,
    type Path,
} from 'react-hook-form';

import { Textarea } from '@/components/atoms/Textarea';

interface ControlledTextareaProps<T extends FieldValues>
    extends Omit<TextareaHTMLAttributes<HTMLTextAreaElement>, 'name' | 'value' | 'onChange'> {
    name: Path<T>;
    control: Control<T>;
    label: string;
    counter?: boolean;
}

export function ControlledTextarea<T extends FieldValues>({
    name,
    control,
    label,
    counter,
    ...rest
}: ControlledTextareaProps<T>) {
    return (
        <Controller
            name={name}
            control={control}
            render={({ field, fieldState }) => (
                <div className="space-y-2">
                    <Textarea
                        {...rest}
                        {...field}
                        label={label}
                        counter={counter}
                        value={(field.value as string | undefined) ?? ''}
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
