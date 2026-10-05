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

const GLOBAL = '00000000-0000-0000-0000-000000000001';

describe('checkInService', () => {
    it('listCategories returns the categories array from the group endpoint', async () => {
        server.use(
            http.get(`http://localhost:5000/groups/${GLOBAL}/categories`, () =>
                HttpResponse.json({
                    categories: [
                        {
                            id: '11111111-1111-1111-1111-111111111111',
                            groupId: GLOBAL,
                            slug: 'legs',
                            name: 'Legs',
                            iconPublicId: 'categories/legs',
                            active: true,
                        },
                    ],
                }),
            ),
        );
        const { default: service } = await import('@/services/checkin.service');

        const result = await service.listCategories(GLOBAL);

        expect(result).toHaveLength(1);
        expect(result[0]?.slug).toBe('legs');
    });

    it('create posts the payload and returns the full response envelope', async () => {
        let receivedBody: unknown = null;
        server.use(
            http.post('http://localhost:5000/check_ins', async ({ request }) => {
                receivedBody = await request.json();
                return HttpResponse.json(
                    {
                        checkIn: {
                            id: 'ci-1',
                            categoryId: 'cat-1',
                            groupId: GLOBAL,
                            xpEarned: 21,
                            scoringSnapshot: {
                                baseXp: 15,
                                multipliers: [{ source: 'category_weight', value: 1.4 }],
                                total: 21,
                            },
                            performedAt: '2026-09-25T12:00:00Z',
                            durationMinutes: 45,
                            notes: 'leg day',
                        },
                        user: {
                            id: 'user-1',
                            xp: 21,
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
            categoryId: 'cat-1',
            title: 'Leg day',
            durationMinutes: 45,
            notes: 'leg day',
        });

        expect(receivedBody).toEqual({
            categoryId: 'cat-1',
            title: 'Leg day',
            durationMinutes: 45,
            notes: 'leg day',
        });
        expect(result.checkIn.xpEarned).toBe(21);
        expect(result.streak.unit).toBe(StreakUnit.Day);
    });

    it('list forwards limit / cursor / groupId as query params', async () => {
        let seenUrl = '';
        server.use(
            http.get('http://localhost:5000/check_ins', ({ request }) => {
                seenUrl = request.url;
                return HttpResponse.json({ checkIns: [], nextCursor: null });
            }),
        );
        const { default: service } = await import('@/services/checkin.service');

        await service.list({ limit: 10, cursor: 'abc', groupId: GLOBAL });

        expect(seenUrl).toContain('limit=10');
        expect(seenUrl).toContain('cursor=abc');
        expect(seenUrl).toContain(`group_id=${GLOBAL}`);
    });
});
