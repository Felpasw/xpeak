'use client';

import { GLOBAL_GROUP_ID } from '@xpeak/shared';
import { Check, Loader2 } from 'lucide-react';

import { AnimatedBorderFab } from '@/components/atoms/AnimatedBorderFab';
import { ControlledCategoryPicker } from '@/components/atoms/ControlledCategoryPicker';
import { ControlledDatePicker } from '@/components/atoms/ControlledDatePicker';
import { ControlledInput } from '@/components/atoms/ControlledInput';
import { ControlledMediaPicker } from '@/components/atoms/ControlledMediaPicker';
import { ControlledTextarea } from '@/components/atoms/ControlledTextarea';
import { LevelUpOverlay } from '@/components/atoms/LevelUpOverlay';
import { PageHeader } from '@/components/atoms/PageHeader';
import { useCategories } from '@/hooks/useCheckIn';
import {
    CHECKIN_MAX_BACKFILL_DAYS,
    CHECKIN_MESSAGES,
    useCheckinForm,
} from '@/hooks/useCheckinForm';

const HEADER = 'Novo check-in';
const HEADER_DESCRIPTION = 'Registre seu treino e ganhe XP.';
const TITLE_LABEL = 'Título';
const TITLE_PLACEHOLDER = 'Ex: Perna B, Cardio manhã';
const TITLE_MAX_LENGTH = 60;
const PERFORMED_ON_LABEL = 'Dia do treino';
const DESCRIPTION_LABEL = 'Descrição';
const DESCRIPTION_PLACEHOLDER = 'Como foi? (opcional)';
const DURATION_LABEL = 'Duração (min)';
const DESCRIPTION_LIMIT = 280;

function isoDaysAgo(days: number) {
    const now = new Date();
    now.setDate(now.getDate() - days);
    return now.toISOString().slice(0, 10);
}

function todayIsoString() {
    return new Date().toISOString().slice(0, 10);
}

export function CheckinForm() {
    const categories = useCategories(GLOBAL_GROUP_ID);
    const { control, onSubmit, isPending, levelUpTo, dismissLevelUp } = useCheckinForm();
    const performedOnMin = isoDaysAgo(CHECKIN_MAX_BACKFILL_DAYS);
    const performedOnMax = todayIsoString();

    return (
        <>
            <form onSubmit={onSubmit} noValidate className="flex flex-1 flex-col gap-6 px-6 pt-10 pb-48">
                <PageHeader title={HEADER} description={HEADER_DESCRIPTION} />

                <ControlledInput
                    control={control}
                    name="title"
                    label={TITLE_LABEL}
                    placeholder={TITLE_PLACEHOLDER}
                    maxLength={TITLE_MAX_LENGTH}
                />

                <ControlledCategoryPicker
                    control={control}
                    name="categoryId"
                    categories={categories.data ?? []}
                    isLoading={categories.isPending}
                />

                <ControlledDatePicker
                    control={control}
                    name="performedOn"
                    label={PERFORMED_ON_LABEL}
                    min={performedOnMin}
                    max={performedOnMax}
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

                <ControlledMediaPicker control={control} name="media" />

                <AnimatedBorderFab
                    type="submit"
                    disabled={isPending}
                    label={isPending ? CHECKIN_MESSAGES.submitting : CHECKIN_MESSAGES.submit}
                    icon={
                        isPending ? (
                            <Loader2 className="h-6 w-6 animate-spin" strokeWidth={2.5} />
                        ) : (
                            <Check className="h-7 w-7" strokeWidth={2.75} />
                        )
                    }
                    className="fixed right-6 bottom-32 z-30 shadow-2xl"
                />
            </form>

            <LevelUpOverlay
                open={levelUpTo !== null}
                level={levelUpTo ?? 0}
                onDismiss={dismissLevelUp}
            />
        </>
    );
}
