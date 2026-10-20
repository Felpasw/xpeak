import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@capacitor/core', () => ({
    Capacitor: { isNativePlatform: () => false },
}));
vi.mock('@capacitor/preferences', () => ({
    Preferences: {
        get: vi.fn(async () => ({ value: null })),
        set: vi.fn(async () => undefined),
        remove: vi.fn(async () => undefined),
    },
}));
vi.mock('sonner', () => ({
    toast: { success: vi.fn(), error: vi.fn() },
}));

describe('<ChallengeDetailShareButton />', () => {
    const invitationId = '11111111-2222-3333-4444-555555555555';
    const expectedUrl = `http://localhost:3000/challenges/join?invitation=${invitationId}`;

    beforeEach(() => {
        Object.defineProperty(window, 'location', {
            value: new URL('http://localhost:3000/challenges/view'),
            configurable: true,
        });
    });

    afterEach(() => {
        vi.clearAllMocks();
    });

    it('renders the share url built from the invitation id', async () => {
        const { ChallengeDetailShareButton } = await import(
            '@/components/organisms/ChallengeDetailShareButton'
        );

        render(<ChallengeDetailShareButton invitationId={invitationId} />);

        const url = screen.getByTestId('challenge-share-url');
        expect(url.textContent).toBe(expectedUrl);
    });

    it('writes the url to the clipboard and toasts success on copy click', async () => {
        const { toast } = await import('sonner');
        const user = userEvent.setup();
        const writeTextSpy = vi.spyOn(navigator.clipboard, 'writeText');
        const { ChallengeDetailShareButton } = await import(
            '@/components/organisms/ChallengeDetailShareButton'
        );

        render(<ChallengeDetailShareButton invitationId={invitationId} />);

        await user.click(screen.getByRole('button', { name: /copiar link/i }));

        expect(writeTextSpy).toHaveBeenCalledWith(expectedUrl);
        expect(toast.success).toHaveBeenCalled();
    });
});
