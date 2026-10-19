export interface ScoringMultiplier {
  source: string;
  value: number;
}

export interface ScoringSnapshot {
  baseXp: number;
  multipliers: ScoringMultiplier[];
  total: number;
}

export interface MediaPreview {
  kind: 'photo' | 'video';
  thumbUrl: string;
}

export interface CheckIn {
  id: string;
  title: string;
  xpEarned: number;
  scoringSnapshot: ScoringSnapshot;
  performedAt: string;
  durationMinutes: number | null;
  notes: string | null;
  hasMedia: boolean;
  mediaPreview: MediaPreview | null;
}

export interface CreateCheckInRequest {
  title: string;
  performedAt?: string | null;
  durationMinutes?: number | null;
  notes?: string | null;
  withMedia?: boolean;
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
