import { isAxiosError } from 'axios';

import api from '@/api';
import { clearToken, setToken } from '@/lib/auth/storage';

import type {
    IAuthService,
    LoginPayload,
    LoginResponse,
    MeResponse,
} from './interfaces/auth.interface';

const extractBearer = (raw: string | undefined | null): string | null => {
    if (!raw) {
        return null;
    }
    const match = /^Bearer\s+(.+)$/i.exec(raw);
    return match?.[1] ?? null;
};

const readBearerFromHeaders = (headers: Record<string, unknown>): string | null => {
    const raw = (headers['authorization'] ?? headers['Authorization']) as string | undefined;
    return extractBearer(raw ?? null);
};

class AuthService implements IAuthService {
    async login(payload: LoginPayload) {
        const response = await api.post<LoginResponse>('/auth/login', payload);
        const token = readBearerFromHeaders(response.headers as Record<string, unknown>);
        if (token) {
            await setToken(token);
        }
        return { user: response.data.user, token };
    }

    async logout(): Promise<void> {
        try {
            await api.post('/auth/logout', {});
        } finally {
            await clearToken();
        }
    }

    async me() {
        try {
            const response = await api.get<MeResponse>('/me');
            return response.data.user;
        } catch (error) {
            if (isAxiosError(error) && error.response?.status === 401) {
                return null;
            }
            throw error;
        }
    }
}

const authService = new AuthService();

export default authService;
