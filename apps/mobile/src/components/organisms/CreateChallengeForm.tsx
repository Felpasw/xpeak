'use client';

import { Plus, Trash2 } from 'lucide-react';

import { Button } from '@/components/atoms/Button';
import { ControlledInput } from '@/components/atoms/ControlledInput';
import {
    CREATE_CHALLENGE_LIMITS,
    useCreateChallengeForm,
} from '@/hooks/useCreateChallengeForm';

const SUBMIT_LABEL = 'Criar challenge';
const SUBMITTING_LABEL = 'Criando...';
const ADD_TIER_LABEL = 'Adicionar fase';
const TIERS_SECTION_TITLE = 'Fases (opcional)';
const TIERS_SECTION_HELP = 'Defina marcos de progresso. Ex: nível 30 ⇒ Mago.';
const TIER_MIN_LEVEL_LABEL = 'Nível mínimo';
const TIER_TITLE_LABEL = 'Título da fase';
const REMOVE_TIER_LABEL = 'Remover fase';

export function CreateChallengeForm() {
    const { form, tierFields, canAddTier, isSubmitting, onSubmit, addTier, removeTier } =
        useCreateChallengeForm();

    return (
        <form onSubmit={onSubmit} className="flex flex-col gap-6">
            <ControlledInput
                name="title"
                control={form.control}
                label="Título"
                maxLength={CREATE_CHALLENGE_LIMITS.titleMaxLength}
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

            <section
                data-testid="create-challenge-tiers"
                className="flex flex-col gap-3 rounded-2xl border border-zinc-800 bg-zinc-900/40 p-4"
            >
                <header className="flex flex-col gap-1">
                    <h3 className="font-mono text-xs uppercase tracking-[0.25em] text-zinc-500">
                        {TIERS_SECTION_TITLE}
                    </h3>
                    <p className="text-xs text-zinc-500">{TIERS_SECTION_HELP}</p>
                </header>

                {tierFields.fields.map((field, index) => (
                    <div
                        key={field.id}
                        data-testid={`tier-row-${index}`}
                        className="flex flex-col gap-2 rounded-xl bg-zinc-950/40 p-3"
                    >
                        <div className="flex gap-2">
                            <div className="w-24">
                                <ControlledInput
                                    name={`tiers.${index}.minLevel`}
                                    control={form.control}
                                    label={TIER_MIN_LEVEL_LABEL}
                                    type="number"
                                    inputMode="numeric"
                                />
                            </div>
                            <div className="flex-1">
                                <ControlledInput
                                    name={`tiers.${index}.title`}
                                    control={form.control}
                                    label={TIER_TITLE_LABEL}
                                    maxLength={CREATE_CHALLENGE_LIMITS.tierTitleMaxLength}
                                />
                            </div>
                        </div>
                        <button
                            type="button"
                            onClick={() => removeTier(index)}
                            className="flex items-center justify-center gap-2 self-end rounded-full border border-zinc-700 bg-zinc-900/60 px-3 py-1.5 text-xs font-semibold text-zinc-300 hover:border-red-500/40 hover:text-red-300"
                        >
                            <Trash2 className="h-3.5 w-3.5" />
                            {REMOVE_TIER_LABEL}
                        </button>
                    </div>
                ))}

                <button
                    type="button"
                    onClick={addTier}
                    disabled={!canAddTier}
                    className="flex items-center justify-center gap-2 rounded-full border border-zinc-700 bg-zinc-900/60 px-4 py-2 text-sm font-semibold text-zinc-200 transition-colors hover:border-emerald-500/40 hover:text-emerald-200 disabled:opacity-50"
                >
                    <Plus className="h-4 w-4" />
                    {ADD_TIER_LABEL}
                </button>
            </section>

            <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? SUBMITTING_LABEL : SUBMIT_LABEL}
            </Button>
        </form>
    );
}
