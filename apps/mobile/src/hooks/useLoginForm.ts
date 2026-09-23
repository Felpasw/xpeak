'use client';

import { standardSchemaResolver } from '@hookform/resolvers/standard-schema';
import { isAxiosError } from 'axios';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';
import { z } from 'zod';

import authHooks from '@/hooks/useAuth';

export const LOGIN_MESSAGES = {
    identifierRequired: 'Enter your email or username',
    passwordRequired: 'Password is required',
    submit: 'Log in',
    submitting: 'Logging in…',
    successToast: 'Welcome back!',
    invalidCredentials: 'Invalid credentials.',
    rateLimited: 'Too many attempts. Wait a few minutes.',
    genericError: "Couldn't log in. Please try again.",
    identifierLabel: 'Email or username',
    passwordLabel: 'Password',
} as const;

const loginSchema = z.object({
    identifier: z
        .string({ error: LOGIN_MESSAGES.identifierRequired })
        .trim()
        .min(1, LOGIN_MESSAGES.identifierRequired),
    password: z
        .string({ error: LOGIN_MESSAGES.passwordRequired })
        .min(1, LOGIN_MESSAGES.passwordRequired),
});

export type LoginFormValues = z.infer<typeof loginSchema>;

export function useLoginForm() {
    const router = useRouter();
    const auth = authHooks.use();
    const [isSuccess, setIsSuccess] = useState(false);

    const form = useForm<LoginFormValues>({
        defaultValues: { identifier: '', password: '' },
        resolver: standardSchemaResolver(loginSchema),
        mode: 'onSubmit',
        reValidateMode: 'onChange',
    });

    const onSubmit = form.handleSubmit(async (values) => {
        try {
            await auth.login.mutateAsync(values);
            toast.success(LOGIN_MESSAGES.successToast);
            setIsSuccess(true);
            router.replace('/home');
        } catch (error) {
            if (isAxiosError(error)) {
                if (error.response?.status === 401) {
                    toast.error(LOGIN_MESSAGES.invalidCredentials);
                    return;
                }
                if (error.response?.status === 429) {
                    toast.error(LOGIN_MESSAGES.rateLimited);
                    return;
                }
            }
            toast.error(LOGIN_MESSAGES.genericError);
        }
    });

    return {
        control: form.control,
        setValue: form.setValue,
        onSubmit,
        isPending: auth.login.isPending,
        isSuccess,
    };
}
