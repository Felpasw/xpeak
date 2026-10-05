export const GLOBAL_GROUP_ID = '00000000-0000-0000-0000-000000000001';

export interface Category {
  id: string;
  groupId: string;
  slug: string;
  name: string;
  iconPublicId: string | null;
  active: boolean;
}

export interface ListCategoriesResponse {
  categories: Category[];
}

export interface ScoringMultiplier {
  source: string;
  value: number;
}

export interface ScoringSnapshot {
  baseXp: number;
  multipliers: ScoringMultiplier[];
  total: number;
}

export interface CheckIn {
  id: string;
  categoryId: string;
  groupId: string;
  title: string;
  xpEarned: number;
  scoringSnapshot: ScoringSnapshot;
  performedAt: string;
  durationMinutes: number | null;
  notes: string | null;
}

export interface CreateCheckInRequest {
  categoryId: string;
  title: string;
  performedAt?: string | null;
  durationMinutes?: number | null;
  notes?: string | null;
}

export interface UserProgression {
  id: string;
  xp: number;
  level: number;
  leveledUp: boolean;
  levelsGained: number;
}

export enum StreakUnit {
  Day = 'day',
  Week = 'week',
}

export interface StreakInfo {
  current: number;
  longest: number;
  unit: StreakUnit;
}

export interface CreateCheckInResponse {
  checkIn: CheckIn;
  user: UserProgression;
  streak: StreakInfo;
}

export interface ListCheckInsResponse {
  checkIns: CheckIn[];
  nextCursor: string | null;
}
