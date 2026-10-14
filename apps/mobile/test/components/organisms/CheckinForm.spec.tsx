import type { PropsWithChildren } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MediaKind } from '@xpeak/shared';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import {
    afterAll,
    afterEach,
    beforeAll,
    beforeEach,
    describe,
    expect,
    it,
    vi,
} from 'vitest';

import type { SelectedMedia } from '@/lib/media/types';

const routerReplace = vi.fn();

vi.mock('next/navigation', () => ({
    useRouter: () => ({ replace: routerReplace, push: routerReplace }),
}));

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

vi.mock('@/lib/media/picker', () => ({
    pickFromGallery: vi.fn(),
    pickFromCamera: vi.fn(),
}));

vi.mock('@/lib/media/uploader', () => ({
    uploadCheckInMedia: vi.fn(),
}));

const { pickFromGallery } = await import('@/lib/media/picker');
const { uploadCheckInMedia } = await import('@/lib/media/uploader');

function buildMedia(id: string): SelectedMedia {
    return {
        id,
        kind: MediaKind.Photo,
        previewUrl: `blob:fake/${id}`,
        blob: new Blob([id], { type: 'image/jpeg' }),
        mimeType: 'image/jpeg',
    };
}

const server = setupServer();
beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => {
    server.resetHandlers();
    routerReplace.mockReset();
    vi.mocked(pickFromGallery).mockReset();
    vi.mocked(uploadCheckInMedia).mockReset();
});
afterAll(() => server.close());

beforeEach(() => {
    window.localStorage.clear();
    window.localStorage.setItem('xpeak.auth.token', 'test.jwt');
});

const renderForm = async () => {
    const client = new QueryClient({
        defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    const Wrapper = ({ children }: PropsWithChildren) => (
        <QueryClientProvider client={client}>{children}</QueryClientProvider>
    );
    const { CheckinForm } = await import('@/components/organisms/CheckinForm');
    render(<CheckinForm />, { wrapper: Wrapper });
};

async function attachOnePhoto() {
    const user = userEvent.setup();
    vi.mocked(pickFromGallery).mockResolvedValueOnce([buildMedia('a')]);
    await user.click(screen.getByRole('button', { name: /galeria/i }));
    await waitFor(() =>
        expect(screen.getByAltText(/mídia 1/i)).toBeInTheDocument(),
    );
}

describe('<CheckinForm />', () => {
    it('blocks submit when the title is blank', async () => {
        const user = userEvent.setup();

        await renderForm();
        await attachOnePhoto();
        await user.click(screen.getByRole('button', { name: /registrar check-in/i }));

        await waitFor(() =>
            expect(
                screen.getByText(/coloca um t(í|i)tulo/i),
            ).toBeInTheDocument(),
        );
    });

    it('blocks submit when no media is attached', async () => {
        const user = userEvent.setup();

        await renderForm();

        await user.type(screen.getByLabelText(/t(í|i)tulo/i), 'Perna B');
        await user.click(screen.getByRole('button', { name: /registrar check-in/i }));

        await waitFor(() =>
            expect(
                screen.getByText(/anexa pelo menos uma foto/i),
            ).toBeInTheDocument(),
        );
        expect(uploadCheckInMedia).not.toHaveBeenCalled();
    });

    it('creates the check-in with withMedia=true, uploads and redirects', async () => {
        const user = userEvent.setup();
        let receivedBody: unknown = null;

        server.use(
            http.post('http://localhost:5000/check_ins', async ({ request }) => {
                receivedBody = await request.json();
                return HttpResponse.json(
                    {
                        checkIn: {
                            id: 'ck-1',
                            title: 'Perna A',
                            xpEarned: 0,
                            scoringSnapshot: { baseXp: 0, multipliers: [], total: 0 },
                            performedAt: '2026-09-25T12:00:00Z',
                            durationMinutes: 45,
                            notes: 'hard set',
                            hasMedia: false,
                            mediaPreview: null,
                        },
                        user: { id: 'u', xp: 0, level: 0, leveledUp: false, levelsGained: 0 },
                        streak: { current: 0, longest: 0, unit: 'day' },
                    },
                    { status: 201 },
                );
            }),
        );
        vi.mocked(uploadCheckInMedia).mockResolvedValue({
            media: [],
            publish: {
                user: { id: 'u', xp: 10, level: 0, leveledUp: false, levelsGained: 0 },
                streak: { current: 1, longest: 1, unit: 'day' },
            },
        });

        await renderForm();

        await user.type(screen.getByLabelText(/t(í|i)tulo/i), '  Perna A  ');
        await user.type(screen.getByLabelText(/dura(ç|c)ão/i), '45');
        await user.type(screen.getByPlaceholderText(/como foi/i), '  hard set  ');
        await attachOnePhoto();
        await user.click(screen.getByRole('button', { name: /registrar check-in/i }));

        await waitFor(() => expect(routerReplace).toHaveBeenCalledWith('/profile'));
        expect(receivedBody).toMatchObject({
            title: 'Perna A',
            durationMinutes: 45,
            notes: 'hard set',
            withMedia: true,
        });
        expect(uploadCheckInMedia).toHaveBeenCalledWith(
            'ck-1',
            expect.arrayContaining([expect.objectContaining({ kind: MediaKind.Photo })]),
        );
    });

    it('shows the level-up overlay when the publish delta reports leveled_up', async () => {
        const user = userEvent.setup();

        server.use(
            http.post('http://localhost:5000/check_ins', () =>
                HttpResponse.json(
                    {
                        checkIn: {
                            id: 'ck-2',
                            title: 'Leg day',
                            xpEarned: 0,
                            scoringSnapshot: { baseXp: 0, multipliers: [], total: 0 },
                            performedAt: '2026-09-25T12:00:00Z',
                            durationMinutes: null,
                            notes: null,
                            hasMedia: false,
                            mediaPreview: null,
                        },
                        user: { id: 'u', xp: 0, level: 0, leveledUp: false, levelsGained: 0 },
                        streak: { current: 0, longest: 0, unit: 'day' },
                    },
                    { status: 201 },
                ),
            ),
        );
        vi.mocked(uploadCheckInMedia).mockResolvedValue({
            media: [],
            publish: {
                user: { id: 'u', xp: 100, level: 1, leveledUp: true, levelsGained: 1 },
                streak: { current: 1, longest: 1, unit: 'day' },
            },
        });

        await renderForm();

        await user.type(screen.getByLabelText(/t(í|i)tulo/i), 'Leg day');
        await attachOnePhoto();
        await user.click(screen.getByRole('button', { name: /registrar check-in/i }));

        await waitFor(() =>
            expect(screen.getByRole('dialog', { name: /subiu de n(í|i)vel/i })).toBeInTheDocument(),
        );
        expect(routerReplace).not.toHaveBeenCalled();

        await user.click(screen.getByRole('dialog', { name: /subiu de n(í|i)vel/i }));
        await waitFor(() => expect(routerReplace).toHaveBeenCalledWith('/profile'));
    });
});
