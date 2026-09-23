import { Capacitor } from '@capacitor/core';
import { Preferences } from '@capacitor/preferences';

const TOKEN_KEY = 'xpeak.auth.token';

const isNative = () => Capacitor.isNativePlatform();

const hasLocalStorage = () =>
    typeof window !== 'undefined' && typeof window.localStorage !== 'undefined';

export async function getToken(): Promise<string | null> {
    if (isNative()) {
        const { value } = await Preferences.get({ key: TOKEN_KEY });
        return value;
    }
    return hasLocalStorage() ? window.localStorage.getItem(TOKEN_KEY) : null;
}

export async function setToken(token: string): Promise<void> {
    if (isNative()) {
        await Preferences.set({ key: TOKEN_KEY, value: token });
        return;
    }
    if (hasLocalStorage()) {
        window.localStorage.setItem(TOKEN_KEY, token);
    }
}

export async function clearToken(): Promise<void> {
    if (isNative()) {
        await Preferences.remove({ key: TOKEN_KEY });
        return;
    }
    if (hasLocalStorage()) {
        window.localStorage.removeItem(TOKEN_KEY);
    }
}
