import type { FormEventHandler } from 'react';
import type { UseFieldArrayReturn, UseFormReturn } from 'react-hook-form';

export interface CreateChallengeTierFormValues {
    minLevel: number;
    title: string;
}

export interface CreateChallengeFormValues {
    title: string;
    targetLevel: number;
    durationDays: number;
    tiers: CreateChallengeTierFormValues[];
}

export interface UseCreateChallengeFormResult {
    form: UseFormReturn<CreateChallengeFormValues>;
    tierFields: UseFieldArrayReturn<CreateChallengeFormValues, 'tiers'>;
    canAddTier: boolean;
    isSubmitting: boolean;
    onSubmit: FormEventHandler<HTMLFormElement>;
    addTier: () => void;
    removeTier: (index: number) => void;
}
