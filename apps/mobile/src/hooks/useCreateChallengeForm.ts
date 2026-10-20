'use client';

import { standardSchemaResolver } from '@hookform/resolvers/standard-schema';
import { useRouter } from 'next/navigation';
import { useFieldArray, useForm } from 'react-hook-form';
import { toast } from 'sonner';
import type { CreateChallengeRequest, CreateChallengeTierInput } from '@xpeak/shared';
import { ChallengeVisibility } from '@xpeak/shared';
import { z } from 'zod';

import challengesHooks from '@/hooks/useChallenges';

import type {
    CreateChallengeFormValues,
    UseCreateChallengeFormResult,
} from './interfaces/useCreateChallengeForm.interface';

const TITLE_MAX_LENGTH = 80;
const TARGET_LEVEL_MIN = 1;
const TARGET_LEVEL_MAX = 999;
const DURATION_MIN_DAYS = 1;
const DURATION_MAX_DAYS = 365;
const TIER_TITLE_MAX_LENGTH = 60;
const TIER_MAX_COUNT = 10;

export const CREATE_CHALLENGE_LIMITS = {
    titleMaxLength: TITLE_MAX_LENGTH,
    tierTitleMaxLength: TIER_TITLE_MAX_LENGTH,
    tierMaxCount: TIER_MAX_COUNT,
} as const;

export const CREATE_CHALLENGE_MESSAGES = {
    titleRequired: 'Coloca um título pro challenge.',
    titleTooLong: `Título deve ter no máximo ${TITLE_MAX_LENGTH} caracteres.`,
    targetLevelRange: `Target level entre ${TARGET_LEVEL_MIN} e ${TARGET_LEVEL_MAX}.`,
    durationRange: `Duração entre ${DURATION_MIN_DAYS} e ${DURATION_MAX_DAYS} dias.`,
    tierTitleRequired: 'Coloca um título pra fase.',
    tierTitleTooLong: `Título deve ter no máximo ${TIER_TITLE_MAX_LENGTH} caracteres.`,
    tierLevelRange: `Nível entre ${TARGET_LEVEL_MIN} e ${TARGET_LEVEL_MAX}.`,
    tiersTooMany: `No máximo ${TIER_MAX_COUNT} fases por challenge.`,
    successToast: 'Challenge criado!',
    genericError: 'Deu ruim criando o challenge. Tenta de novo.',
} as const;

const schema = z.object({
    title: z
        .string()
        .trim()
        .min(1, CREATE_CHALLENGE_MESSAGES.titleRequired)
        .max(TITLE_MAX_LENGTH, CREATE_CHALLENGE_MESSAGES.titleTooLong),
    targetLevel: z
        .coerce.number()
        .int()
        .min(TARGET_LEVEL_MIN, CREATE_CHALLENGE_MESSAGES.targetLevelRange)
        .max(TARGET_LEVEL_MAX, CREATE_CHALLENGE_MESSAGES.targetLevelRange),
    durationDays: z
        .coerce.number()
        .int()
        .min(DURATION_MIN_DAYS, CREATE_CHALLENGE_MESSAGES.durationRange)
        .max(DURATION_MAX_DAYS, CREATE_CHALLENGE_MESSAGES.durationRange),
    tiers: z
        .array(z.object({
            minLevel: z
                .coerce.number()
                .int()
                .min(TARGET_LEVEL_MIN, CREATE_CHALLENGE_MESSAGES.tierLevelRange)
                .max(TARGET_LEVEL_MAX, CREATE_CHALLENGE_MESSAGES.tierLevelRange),
            title: z
                .string()
                .trim()
                .min(1, CREATE_CHALLENGE_MESSAGES.tierTitleRequired)
                .max(TIER_TITLE_MAX_LENGTH, CREATE_CHALLENGE_MESSAGES.tierTitleTooLong),
        }))
        .max(TIER_MAX_COUNT, CREATE_CHALLENGE_MESSAGES.tiersTooMany),
});

export function useCreateChallengeForm(): UseCreateChallengeFormResult {
    const router = useRouter();
    const createChallenge = challengesHooks.useCreate();

    const form = useForm<CreateChallengeFormValues>({
        resolver: standardSchemaResolver(schema),
        defaultValues: {
            title: '',
            targetLevel: 5,
            durationDays: 30,
            tiers: [],
        },
    });

    const tierFields = useFieldArray({
        control: form.control,
        name: 'tiers',
    });

    const canAddTier = tierFields.fields.length < TIER_MAX_COUNT;

    const onSubmit = form.handleSubmit(async (values) => {
        const now = new Date();
        const endsAt = new Date(now.getTime() + values.durationDays * 86_400_000);
        const tiers: CreateChallengeTierInput[] = values.tiers.map((t) => ({
            minLevel: t.minLevel,
            title: t.title.trim(),
        }));
        const payload: CreateChallengeRequest = {
            title: values.title.trim(),
            visibility: ChallengeVisibility.Private,
            targetLevel: values.targetLevel,
            xpMultiplier: 1.0,
            startsAt: now.toISOString(),
            endsAt: endsAt.toISOString(),
            ...(tiers.length > 0 ? { tiers } : {}),
        };

        try {
            await createChallenge.mutateAsync(payload);
            toast.success(CREATE_CHALLENGE_MESSAGES.successToast);
            router.replace('/challenges');
        } catch {
            toast.error(CREATE_CHALLENGE_MESSAGES.genericError);
        }
    });

    return {
        form,
        tierFields,
        canAddTier,
        isSubmitting: createChallenge.isPending,
        onSubmit,
        addTier: () => tierFields.append({ minLevel: 1, title: '' }),
        removeTier: (index) => tierFields.remove(index),
    };
}
