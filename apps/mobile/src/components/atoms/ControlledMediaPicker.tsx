'use client';

import {
    Controller,
    type Control,
    type FieldValues,
    type Path,
} from 'react-hook-form';

import { MediaPicker } from '@/components/atoms/MediaPicker';
import { MEDIA_MAX_PER_CHECKIN, type SelectedMedia } from '@/lib/media/types';

interface ControlledMediaPickerProps<T extends FieldValues> {
    name: Path<T>;
    control: Control<T>;
    max?: number;
    disabled?: boolean;
    className?: string;
}

export function ControlledMediaPicker<T extends FieldValues>({
    name,
    control,
    max = MEDIA_MAX_PER_CHECKIN,
    disabled,
    className,
}: ControlledMediaPickerProps<T>) {
    return (
        <Controller
            name={name}
            control={control}
            render={({ field, fieldState }) => (
                <div className="space-y-2">
                    <MediaPicker
                        value={(field.value as SelectedMedia[] | undefined) ?? []}
                        onChange={(next) => field.onChange(next)}
                        max={max}
                        disabled={disabled}
                        className={className}
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
