export enum MediaKind {
  Photo = 'photo',
  Video = 'video',
}

export interface PresignMediaItem {
  kind: MediaKind;
}

export interface PresignMediaRequest {
  items: PresignMediaItem[];
}

export interface PresignMediaSlot {
  storageKey: string;
  url: string;
  expiresIn: number;
}

export interface PresignMediaResponse {
  presigned: PresignMediaSlot[];
}

export interface ConfirmMediaItem {
  storageKey: string;
  kind: MediaKind;
  position: number;
}

export interface ConfirmMediaRequest {
  items: ConfirmMediaItem[];
}

export interface CheckInMedia {
  id: string;
  checkInId: string;
  kind: MediaKind;
  storageKey: string;
  width: number | null;
  height: number | null;
  durationSeconds: number | null;
  position: number;
  createdAt: string;
}

export interface UserProgressionDelta {
  id: string;
  xp: number;
  level: number;
  leveledUp: boolean;
  levelsGained: number;
}

export interface StreakDelta {
  current: number;
  longest: number;
  unit: string;
}

export interface PublishDelta {
  user: UserProgressionDelta;
  streak: StreakDelta;
}

export interface ConfirmMediaResponse {
  media: CheckInMedia[];
  publish: PublishDelta | null;
}
