import { StreakUnit } from '@xpeak/shared';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';

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

const server = setupServer();
beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

beforeEach(() => {
    window.localStorage.clear();
    window.localStorage.setItem('xpeak.auth.token', 'test.jwt');
});

describe('checkInService', () => {
    it('create posts the payload and returns the full response envelope', async () => {
        let receivedBody: unknown = null;
        server.use(
            http.post('http://localhost:5000/check_ins', async ({ request }) => {
                receivedBody = await request.json();
                return HttpResponse.json(
                    {
                        checkIn: {
                            id: 'ci-1',
                            title: 'Leg day',
                            xpEarned: 10,
                            scoringSnapshot: {
                                baseXp: 10,
                                multipliers: [],
                                total: 10,
                            },
                            performedAt: '2026-09-25T12:00:00Z',
                            durationMinutes: 45,
                            notes: 'leg day',
                            hasMedia: false,
                            mediaPreview: null,
                        },
                        user: {
                            id: 'user-1',
                            xp: 10,
                            level: 0,
                            leveledUp: false,
                            levelsGained: 0,
                        },
                        streak: { current: 1, longest: 1, unit: 'day' },
                    },
                    { status: 201 },
                );
            }),
        );
        const { default: service } = await import('@/services/checkin.service');

        const result = await service.create({
            title: 'Leg day',
            durationMinutes: 45,
            notes: 'leg day',
        });

        expect(receivedBody).toEqual({
            title: 'Leg day',
            durationMinutes: 45,
            notes: 'leg day',
        });
        expect(result.checkIn.xpEarned).toBe(10);
        expect(result.streak.unit).toBe(StreakUnit.Day);
    });

    it('list forwards limit and cursor as query params', async () => {
        let seenUrl = '';
        server.use(
            http.get('http://localhost:5000/check_ins', ({ request }) => {
                seenUrl = request.url;
                return HttpResponse.json({ checkIns: [], nextCursor: null });
            }),
        );
        const { default: service } = await import('@/services/checkin.service');

        await service.list({ limit: 10, cursor: 'abc' });

        expect(seenUrl).toContain('limit=10');
        expect(seenUrl).toContain('cursor=abc');
    });
});
