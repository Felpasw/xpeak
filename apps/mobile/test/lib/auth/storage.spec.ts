import { beforeEach, describe, expect, it, vi } from 'vitest';

const isNativeMock = vi.fn(() => false);
const preferencesMock = {
    get: vi.fn(async () => ({ value: null as string | null })),
    set: vi.fn(async () => undefined),
    remove: vi.fn(async () => undefined),
};

vi.mock('@capacitor/core', () => ({
    Capacitor: {
        isNativePlatform: () => isNativeMock(),
    },
}));

vi.mock('@capacitor/preferences', () => ({
    Preferences: preferencesMock,
}));

const importStorage = async () => await import('@/lib/auth/storage');

describe('token storage — web (localStorage)', () => {
    beforeEach(() => {
        isNativeMock.mockReset().mockReturnValue(false);
        window.localStorage.clear();
    });

    it('round-trips a token', async () => {
        const storage = await importStorage();

        await storage.setToken('abc.def');
        await expect(storage.getToken()).resolves.toBe('abc.def');

        await storage.clearToken();
        await expect(storage.getToken()).resolves.toBeNull();
    });

    it('returns null when nothing was ever stored', async () => {
        const storage = await importStorage();
        await expect(storage.getToken()).resolves.toBeNull();
    });
});

describe('token storage — native (Capacitor Preferences)', () => {
    beforeEach(() => {
        isNativeMock.mockReset().mockReturnValue(true);
        preferencesMock.get.mockReset();
        preferencesMock.set.mockReset();
        preferencesMock.remove.mockReset();
    });

    it('routes reads through Preferences.get', async () => {
        preferencesMock.get.mockResolvedValueOnce({ value: 'native.token' });
        const storage = await importStorage();

        await expect(storage.getToken()).resolves.toBe('native.token');
        expect(preferencesMock.get).toHaveBeenCalledWith({ key: 'xpeak.auth.token' });
    });

    it('routes writes through Preferences.set', async () => {
        const storage = await importStorage();

        await storage.setToken('native.token');
        expect(preferencesMock.set).toHaveBeenCalledWith({
            key: 'xpeak.auth.token',
            value: 'native.token',
        });
    });

    it('routes clears through Preferences.remove', async () => {
        const storage = await importStorage();

        await storage.clearToken();
        expect(preferencesMock.remove).toHaveBeenCalledWith({ key: 'xpeak.auth.token' });
    });
});
