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

/** Author snapshot embedded on every check-in payload so a feed card
 * renders the header without a secondary request. */
export interface CheckInAuthor {
  id: string;
  username: string;
  avatarUrl: string | null;
}

/** Aggregate of emoji reactions on a check-in: `counts` is grouped by
 * emoji; `mine` carries the caller's own reactions. */
export interface CheckInReactionsSummary {
  counts: Record<string, number>;
  mine: string[];
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
  reactions: CheckInReactionsSummary;
  commentsCount: number;
}

/** Full payload of `GET /check_ins/{id}` — includes the author
 * snapshot on top of the enriched `CheckIn` shape. */
export interface CheckInDetail extends CheckIn {
  author: CheckInAuthor;
}

export interface ReactionsWithUsers {
  counts: Record<string, number>;
  mine: string[];
  users: Record<string, CheckInAuthor[]>;
}

export interface CheckInComment {
  id: string;
  checkInId: string;
  author: CheckInAuthor;
  body: string;
  createdAt: string;
}

export interface ListCheckInCommentsResponse {
  items: CheckInComment[];
  nextCursor: string | null;
}

export interface ToggleReactionRequest {
  emoji: string;
}

export interface CreateCommentRequest {
  body: string;
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
