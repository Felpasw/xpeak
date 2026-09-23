import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useForm, type Resolver } from 'react-hook-form';
import { describe, expect, it } from 'vitest';

import { ControlledInput } from '@/components/atoms/ControlledInput';

interface FormShape {
    email: string;
    username: string;
}

function Host({ defaultEmail = '' }: { defaultEmail?: string }) {
    const { control } = useForm<FormShape>({
        defaultValues: { email: defaultEmail, username: '' },
    });
    return (
        <ControlledInput
            control={control}
            name="email"
            label="Email"
            type="email"
        />
    );
}

const requiredEmailResolver: Resolver<FormShape> = async (values) => {
    if (!values.email) {
        return {
            values: {} as Record<string, never>,
            errors: {
                email: { type: 'required', message: 'Email is required' },
            },
        };
    }
    return { values, errors: {} as Record<string, never> };
};

function HostWithErrors() {
    const { control, handleSubmit } = useForm<FormShape>({
        defaultValues: { email: '', username: '' },
        resolver: requiredEmailResolver,
        mode: 'onSubmit',
    });
    return (
        <form onSubmit={handleSubmit(() => undefined)} noValidate>
            <ControlledInput control={control} name="email" label="Email" />
            <button type="submit">Submit</button>
        </form>
    );
}

function HostWithTransform() {
    const { control } = useForm<FormShape>({
        defaultValues: { email: '', username: '' },
    });
    return (
        <ControlledInput
            control={control}
            name="username"
            label="Username"
            transform={(value) => value.toLowerCase()}
        />
    );
}

describe('<ControlledInput />', () => {
    it('is controlled via react-hook-form', async () => {
        const user = userEvent.setup();
        render(<Host />);

        const input = screen.getByLabelText(/email/i);
        await user.type(input, 'felipe@xpeak.com');

        expect(input).toHaveValue('felipe@xpeak.com');
    });

    it('respects the form defaultValues', () => {
        render(<Host defaultEmail="cagao@xpeak.com" />);

        expect(screen.getByLabelText(/email/i)).toHaveValue('cagao@xpeak.com');
    });

    it('renders the fieldState error under the input', async () => {
        const user = userEvent.setup();
        render(<HostWithErrors />);

        await user.click(screen.getByRole('button', { name: /submit/i }));

        expect(await screen.findByRole('alert')).toHaveTextContent(/email is required/i);
    });

    it('runs the transform prop before storing the value', async () => {
        const user = userEvent.setup();
        render(<HostWithTransform />);

        const input = screen.getByLabelText(/username/i);
        await user.type(input, 'FeLiPe_99');

        expect(input).toHaveValue('felipe_99');
    });
});
