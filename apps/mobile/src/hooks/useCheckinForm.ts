'use client';

import { standardSchemaResolver } from '@hookform/resolvers/standard-schema';
import { isAfter, isBefore, isValid, parseISO, startOfToday, subDays } from 'date-fns';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';
import { z } from 'zod';

import { useCreateCheckIn } from '@/hooks/useCheckIn';

import type {
    CheckinFormValues,
    UseCheckinFormResult,
} from './interfaces/useCheckinForm.interface';

const TITLE_MAX_LENGTH = 60;
const NOTES_MAX_LENGTH = 280;
const MAX_BACKFILL_DAYS = 7;

export const CHECKIN_MAX_BACKFILL_DAYS = MAX_BACKFILL_DAYS;

export const CHECKIN_MESSAGES = {
    categoryRequired: 'Escolha uma categoria pra registrar um check-in.',
    titleRequired: 'Coloca um título pro treino.',
    titleTooLong: `Título deve ter no máximo ${TITLE_MAX_LENGTH} caracteres.`,
    dateInFuture: 'A data não pode ser no futuro.',
    dateTooOld: `A data não pode ser mais de ${MAX_BACKFILL_DAYS} dias atrás.`,
    durationInvalid: 'Duração precisa ser um número inteiro positivo.',
    notesTooLong: `Descrição deve ter no máximo ${NOTES_MAX_LENGTH} caracteres.`,
    submit: 'Registrar check-in',
    submitting: 'Registrando…',
    successToast: 'Check-in registrado!',
    genericError: 'Não deu pra salvar o check-in.',
} as const;

const todayIso = () => new Date().toISOString().slice(0, 10);

const checkinSchema = z.object({
    categoryId: z.string().min(1, CHECKIN_MESSAGES.categoryRequired),
    title: z
        .string()
        .trim()
        .min(1, CHECKIN_MESSAGES.titleRequired)
        .max(TITLE_MAX_LENGTH, CHECKIN_MESSAGES.titleTooLong),
    performedOn: z
        .string()
        .min(1)
        .refine((v) => {
            const parsed = parseISO(v);
            return isValid(parsed) && !isAfter(parsed, startOfToday());
        }, CHECKIN_MESSAGES.dateInFuture)
        .refine(
            (v) => !isBefore(parseISO(v), subDays(startOfToday(), MAX_BACKFILL_DAYS)),
            CHECKIN_MESSAGES.dateTooOld,
        ),
    duration: z
        .string()
        .refine(
            (v) => v === '' || (Number.isInteger(Number(v)) && Number(v) > 0),
            CHECKIN_MESSAGES.durationInvalid,
        ),
    notes: z.string().max(NOTES_MAX_LENGTH, CHECKIN_MESSAGES.notesTooLong),
});

export function useCheckinForm(): UseCheckinFormResult {
    const router = useRouter();
    const createCheckIn = useCreateCheckIn();
    const [levelUpTo, setLevelUpTo] = useState<number | null>(null);

    const form = useForm<CheckinFormValues>({
        defaultValues: {
            categoryId: '',
            title: '',
            performedOn: todayIso(),
            duration: '',
            notes: '',
        },
        resolver: standardSchemaResolver(checkinSchema),
        mode: 'onSubmit',
        reValidateMode: 'onChange',
    });

    const onSubmit = form.handleSubmit(async (values) => {
        const parsedDuration = values.duration.trim() === '' ? null : Number(values.duration);
        const trimmedNotes = values.notes.trim();

        try {
            const result = await createCheckIn.mutateAsync({
                categoryId: values.categoryId,
                title: values.title.trim(),
                performedAt: performedOnToIso(values.performedOn),
                durationMinutes: parsedDuration,
                notes: trimmedNotes === '' ? null : trimmedNotes,
            });
            toast.success(CHECKIN_MESSAGES.successToast);
            if (result.user.leveledUp) {
                setLevelUpTo(result.user.level);
                return;
            }
            router.replace('/profile');
        } catch {
            toast.error(CHECKIN_MESSAGES.genericError);
        }
    });

    const dismissLevelUp = () => {
        setLevelUpTo(null);
        router.replace('/profile');
    };

    return {
        control: form.control,
        setValue: form.setValue,
        watch: form.watch,
        onSubmit,
        isPending: createCheckIn.isPending,
        levelUpTo,
        dismissLevelUp,
    };
}

// Anchor the date input to noon so timezone shifts don't push it into
// the previous day when serialized as UTC.
function performedOnToIso(performedOn: string) {
    return new Date(`${performedOn}T12:00:00`).toISOString();
}
