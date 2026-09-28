import type { FormEvent } from 'react';
import type {
    Control,
    UseFormSetValue,
    UseFormWatch,
} from 'react-hook-form';

export interface CheckinFormValues {
    categoryId: string;
    duration: string;
    notes: string;
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
