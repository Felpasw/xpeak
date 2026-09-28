'use client';

import { GLOBAL_GROUP_ID } from '@xpeak/shared';

import { Button } from '@/components/atoms/Button';
import { ControlledCategoryPicker } from '@/components/atoms/ControlledCategoryPicker';
import { ControlledInput } from '@/components/atoms/ControlledInput';
import { ControlledTextarea } from '@/components/atoms/ControlledTextarea';
import { LevelUpOverlay } from '@/components/atoms/LevelUpOverlay';
import { useCategories } from '@/hooks/useCheckIn';
import { CHECKIN_MESSAGES, useCheckinForm } from '@/hooks/useCheckinForm';

const HEADER = 'Novo check-in';
const HEADER_DESCRIPTION = 'Registre seu treino e ganhe XP.';
const DESCRIPTION_LABEL = 'Descrição';
const DESCRIPTION_PLACEHOLDER = 'Como foi? (opcional)';
const DURATION_LABEL = 'Duração (min)';
const DESCRIPTION_LIMIT = 280;

export function CheckinForm() {
    const categories = useCategories(GLOBAL_GROUP_ID);
    const { control, onSubmit, isPending, levelUpTo, dismissLevelUp } = useCheckinForm();

    return (
        <>
            <form onSubmit={onSubmit} noValidate className="flex flex-1 flex-col gap-6 px-6 pt-8 pb-48">
                <header className="flex flex-col gap-1">
                    <h1 className="text-2xl font-bold tracking-tight text-zinc-100">{HEADER}</h1>
                    <p className="text-sm text-zinc-400">{HEADER_DESCRIPTION}</p>
                </header>

                <ControlledCategoryPicker
                    control={control}
                    name="categoryId"
                    categories={categories.data ?? []}
                    isLoading={categories.isPending}
                />

                <ControlledInput
                    control={control}
                    name="duration"
                    label={DURATION_LABEL}
                    type="number"
                    min={1}
                    inputMode="numeric"
                />

                <ControlledTextarea
                    control={control}
                    name="notes"
                    label={DESCRIPTION_LABEL}
                    placeholder={DESCRIPTION_PLACEHOLDER}
                    rows={3}
                    maxLength={DESCRIPTION_LIMIT}
                    counter
                />

                <Button type="submit" variant="primary" disabled={isPending} className="mt-auto">
                    {isPending ? CHECKIN_MESSAGES.submitting : CHECKIN_MESSAGES.submit}
                </Button>
            </form>

            <LevelUpOverlay
                open={levelUpTo !== null}
                level={levelUpTo ?? 0}
                onDismiss={dismissLevelUp}
            />
        </>
    );
}
