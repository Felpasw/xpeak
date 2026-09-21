'use client';

import { motion, type Variants } from 'motion/react';

import { ControlledInput } from '@/components/atoms/ControlledInput';
import { FlowButton } from '@/components/atoms/FlowButton';
import { LOGIN_MESSAGES, useLoginForm } from '@/hooks/useLoginForm';

const containerVariants: Variants = {
    initial: {},
    animate: {
        transition: {
            staggerChildren: 0.12,
            delayChildren: 0.15,
        },
    },
};

const itemVariants: Variants = {
    initial: { opacity: 0, y: 12 },
    animate: {
        opacity: 1,
        y: 0,
        transition: { duration: 0.5, ease: 'easeOut' },
    },
};

export function LoginForm() {
    const { control, onSubmit, isPending } = useLoginForm();

    return (
        <motion.form
            onSubmit={onSubmit}
            noValidate
            className="w-full max-w-sm space-y-8"
            variants={containerVariants}
            initial="initial"
            animate="animate"
        >
            <div className="space-y-8">
                <motion.div variants={itemVariants}>
                    <ControlledInput
                        control={control}
                        name="identifier"
                        label={LOGIN_MESSAGES.identifierLabel}
                        autoComplete="username"
                    />
                </motion.div>
                <motion.div variants={itemVariants}>
                    <ControlledInput
                        control={control}
                        name="password"
                        type="password"
                        label={LOGIN_MESSAGES.passwordLabel}
                        autoComplete="current-password"
                        showPasswordToggle
                    />
                </motion.div>
            </div>

            <motion.div variants={itemVariants} className="flex justify-center">
                <FlowButton
                    type="submit"
                    loading={isPending}
                    text={isPending ? LOGIN_MESSAGES.submitting : LOGIN_MESSAGES.submit}
                />
            </motion.div>
        </motion.form>
    );
}
