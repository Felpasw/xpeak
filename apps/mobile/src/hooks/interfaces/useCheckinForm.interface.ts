import type { FormEvent } from 'react';
import type {
    Control,
    UseFormSetValue,
    UseFormWatch,
} from 'react-hook-form';

import type { SelectedMedia } from '@/lib/media/types';

export interface CheckinFormValues {
    categoryId: string;
    title: string;
    performedOn: string;
    duration: string;
    notes: string;
    media: SelectedMedia[];
}

export interface UseCheckinFormResult {
    control: Control<CheckinFormValues>;
    setValue: UseFormSetValue<CheckinFormValues>;
    watch: UseFormWatch<CheckinFormValues>;
    onSubmit: (event: FormEvent<HTMLFormElement>) => Promise<void>;
    isPending: boolean;
    levelUpTo: number | null;
    dismissLevelUp: () => void;
}
