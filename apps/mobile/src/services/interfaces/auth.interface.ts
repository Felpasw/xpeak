import type { AuthResponse, User } from '@xpeak/shared';

export interface LoginPayload {
    identifier: string;
    password: string;
}

export type LoginResponse = AuthResponse;
export type MeResponse = AuthResponse;

export interface IAuthService {
    login(payload: LoginPayload): Promise<{ user: User; token: string | null }>;
    logout(): Promise<void>;
    me(): Promise<User | null>;
}
