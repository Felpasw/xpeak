import type { PublicUser } from '@xpeak/shared';
import { isAxiosError } from 'axios';

import api from '@/api';

import type { IUsersService } from './interfaces/users.interface';

class UsersService implements IUsersService {
    async get(username: string) {
        try {
            const response = await api.get<PublicUser>(
                `/users/${encodeURIComponent(username)}`,
            );
            return response.data;
        } catch (error) {
            if (isAxiosError(error) && error.response?.status === 404) {
                return null;
            }
            throw error;
        }
    }
}

const usersService = new UsersService();

export default usersService;
