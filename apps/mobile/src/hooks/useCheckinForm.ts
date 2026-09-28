'use client';

import { standardSchemaResolver } from '@hookform/resolvers/standard-schema';
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

export const CHECKIN_MESSAGES = {
    categoryRequired: 'Escolha uma categoria pra registrar um check-in.',
    durationInvalid: 'Duração precisa ser um número inteiro positivo.',
    notesTooLong: 'Descrição deve ter no máximo 280 caracteres.',
    submit: 'Registrar check-in',
    submitting: 'Registrando…',
    successToast: 'Check-in registrado!',
    genericError: 'Não deu pra salvar o check-in.',
} as const;

const checkinSchema = z.object({
    categoryId: z.string().min(1, CHECKIN_MESSAGES.categoryRequired),
    duration: z
        .string()
        .refine(
            (v) => v === '' || (Number.isInteger(Number(v)) && Number(v) > 0),
            CHECKIN_MESSAGES.durationInvalid,
        ),
    notes: z.string().max(280, CHECKIN_MESSAGES.notesTooLong),
});

export function useCheckinForm(): UseCheckinFormResult {
    const router = useRouter();
    const createCheckIn = useCreateCheckIn();
    const [levelUpTo, setLevelUpTo] = useState<number | null>(null);

    const form = useForm<CheckinFormValues>({
        defaultValues: { categoryId: '', duration: '', notes: '' },
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
