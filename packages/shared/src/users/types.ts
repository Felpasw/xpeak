import type { Backgrounds } from '../auth/types';

/**
 * Public projection of a user, safe to hand to any authenticated
 * viewer. Mirrors the backend `PublicUserResponse`: drops email
 * and streak counters that live only on the private `/me` surface.
 */
export interface PublicUser {
  id: string;
  username: string;
  avatarUrl: string | null;
  level: number;
  xp: number;
  background: Backgrounds | null;
  createdAt: string;
}
