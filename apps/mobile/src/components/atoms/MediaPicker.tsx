'use client';

import { Camera, Image as ImageIcon } from 'lucide-react';
import { useState } from 'react';

import { MediaThumb } from '@/components/atoms/MediaThumb';
import { PickerButton } from '@/components/atoms/PickerButton';
import { pickFromCamera, pickFromGallery } from '@/lib/media/picker';
import { MEDIA_MAX_PER_CHECKIN, type SelectedMedia } from '@/lib/media/types';
import { cn } from '@/lib/utils';

interface MediaPickerProps {
    value: SelectedMedia[];
    onChange: (next: SelectedMedia[]) => void;
    max?: number;
    disabled?: boolean;
    className?: string;
}

const GALLERY_LABEL = 'Galeria';
const CAMERA_LABEL = 'Câmera';

export function MediaPicker({
    value,
    onChange,
    max = MEDIA_MAX_PER_CHECKIN,
    disabled = false,
    className,
}: MediaPickerProps) {
    const [isPicking, setIsPicking] = useState(false);
    const remaining = Math.max(0, max - value.length);
    const addDisabled = disabled || remaining === 0 || isPicking;

    const openGallery = async () => {
        if (addDisabled) return;
        setIsPicking(true);
        try {
            const picked = await pickFromGallery({ limit: remaining });
            if (picked.length > 0) onChange([...value, ...picked]);
        } finally {
            setIsPicking(false);
        }
    };

    const openCamera = async () => {
        if (addDisabled) return;
        setIsPicking(true);
        try {
            const picked = await pickFromCamera();
            onChange([...value, picked]);
        } finally {
            setIsPicking(false);
        }
    };

    const removeAt = (index: number) => {
        onChange(value.filter((_, i) => i !== index));
    };

    return (
        <div className={cn('flex flex-col gap-3', className)}>
            <div className="flex gap-2">
                <PickerButton
                    label={GALLERY_LABEL}
                    icon={<ImageIcon className="h-4 w-4" />}
                    disabled={addDisabled}
                    onClick={openGallery}
                />
                <PickerButton
                    label={CAMERA_LABEL}
                    icon={<Camera className="h-4 w-4" />}
                    disabled={addDisabled}
                    onClick={openCamera}
                />
                <span className="ml-auto self-center text-xs text-zinc-500 tabular-nums">
                    {value.length} / {max}
                </span>
            </div>

            {value.length > 0 ? (
                <ul className="grid grid-cols-4 gap-2">
                    {value.map((item, index) => (
                        <MediaThumb
                            key={item.id}
                            src={item.previewUrl}
                            index={index}
                            onRemove={() => removeAt(index)}
                        />
                    ))}
                </ul>
            ) : null}
        </div>
    );
}
