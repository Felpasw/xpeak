import axios, { AxiosError } from 'axios';

import { API_URL } from '@/globals';
import { clearToken, getToken } from '@/lib/auth/storage';

const api = axios.create({
    baseURL: API_URL,
    headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use(async (config) => {
    const token = await getToken();
    if (token) {
        config.headers.set('Authorization', `Bearer ${token}`);
    }
    return config;
});

let unauthorizedHandler: (() => void) | null = null;

export function onUnauthorized(handler: (() => void) | null): void {
    unauthorizedHandler = handler;
}

api.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
        if (error.response?.status === 401) {
            await clearToken();
            unauthorizedHandler?.();
        }
        throw error;
    },
);

export default api;
