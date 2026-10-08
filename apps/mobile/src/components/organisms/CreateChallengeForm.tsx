'use client';

import { standardSchemaResolver } from '@hookform/resolvers/standard-schema';
import { useRouter } from 'next/navigation';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';
import type { CreateChallengeRequest } from '@xpeak/shared';
import { ChallengeVisibility } from '@xpeak/shared';
import { z } from 'zod';

import { Button } from '@/components/atoms/Button';
import { ControlledInput } from '@/components/atoms/ControlledInput';
import { useCreateChallenge } from '@/hooks/useChallenges';

const TITLE_MAX = 80;
const TARGET_LEVEL_MIN = 1;
const TARGET_LEVEL_MAX = 999;
const DURATION_MIN_DAYS = 1;
const DURATION_MAX_DAYS = 365;

const MESSAGES = {
    titleRequired: 'Coloca um título pro challenge.',
    titleTooLong: `Título deve ter no máximo ${TITLE_MAX} caracteres.`,
    targetLevelRange: `Target level entre ${TARGET_LEVEL_MIN} e ${TARGET_LEVEL_MAX}.`,
    durationRange: `Duração entre ${DURATION_MIN_DAYS} e ${DURATION_MAX_DAYS} dias.`,
    successToast: 'Challenge criado!',
    genericError: 'Deu ruim criando o challenge. Tenta de novo.',
} as const;

const schema = z.object({
    title: z
        .string()
        .trim()
        .min(1, MESSAGES.titleRequired)
        .max(TITLE_MAX, MESSAGES.titleTooLong),
    targetLevel: z
        .coerce.number()
        .int()
        .min(TARGET_LEVEL_MIN, MESSAGES.targetLevelRange)
        .max(TARGET_LEVEL_MAX, MESSAGES.targetLevelRange),
    durationDays: z
        .coerce.number()
        .int()
        .min(DURATION_MIN_DAYS, MESSAGES.durationRange)
        .max(DURATION_MAX_DAYS, MESSAGES.durationRange),
});

type FormValues = z.infer<typeof schema>;

export function CreateChallengeForm() {
    const router = useRouter();
    const createChallenge = useCreateChallenge();

    const form = useForm<FormValues>({
        resolver: standardSchemaResolver(schema),
        defaultValues: {
            title: '',
            targetLevel: 5,
            durationDays: 30,
        },
    });

    const onSubmit = form.handleSubmit(async (values) => {
        const now = new Date();
        const endsAt = new Date(now.getTime() + values.durationDays * 86_400_000);
        const payload: CreateChallengeRequest = {
            title: values.title.trim(),
            visibility: ChallengeVisibility.Private,
            targetLevel: values.targetLevel,
            xpMultiplier: 1.0,
            startsAt: now.toISOString(),
            endsAt: endsAt.toISOString(),
        };

        try {
            await createChallenge.mutateAsync(payload);
            toast.success(MESSAGES.successToast);
            router.replace('/challenges');
        } catch {
            toast.error(MESSAGES.genericError);
        }
    });

    return (
        <form onSubmit={onSubmit} className="flex flex-col gap-6">
            <ControlledInput
                name="title"
                control={form.control}
                label="Título"
                maxLength={TITLE_MAX}
            />

            <ControlledInput
                name="targetLevel"
                control={form.control}
                label="Target level"
                type="number"
                inputMode="numeric"
            />

            <ControlledInput
                name="durationDays"
                control={form.control}
                label="Dura (dias)"
                type="number"
                inputMode="numeric"
            />

            <Button type="submit" disabled={createChallenge.isPending}>
                {createChallenge.isPending ? 'Criando...' : 'Criar challenge'}
            </Button>
        </form>
    );
}
