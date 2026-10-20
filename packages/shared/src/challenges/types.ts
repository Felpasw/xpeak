export enum ChallengeVisibility {
    Public = 'public',
    Private = 'private',
}

export enum ChallengeState {
    Active = 'active',
    Completed = 'completed',
    Failed = 'failed',
}

export enum ChallengeMembershipRole {
    Owner = 'owner',
    Member = 'member',
}

export enum ChallengeMembershipResult {
    Completed = 'completed',
    Failed = 'failed',
}

export enum ChallengeInvitationState {
    Pending = 'pending',
    Accepted = 'accepted',
    Rejected = 'rejected',
    Expired = 'expired',
}

export interface MembershipProgress {
  value: number;
  target: number;
  achievedAt: string | null;
}

export interface Challenge {
  id: string;
  ownerId: string;
  title: string;
  description: string | null;
  iconPublicId: string | null;
  visibility: ChallengeVisibility;
  targetLevel: number;
  xpMultiplier: number;
  startsAt: string;
  endsAt: string;
  memberCap: number | null;
  state: ChallengeState;
  createdAt: string;
}

export interface ChallengeMembership {
  id: string;
  challengeId: string;
  userId: string;
  role: ChallengeMembershipRole;
  joinedAt: string;
  leftAt: string | null;
  result: ChallengeMembershipResult | null;
  progress: MembershipProgress;
}

export interface ChallengeInvitation {
  id: string;
  challengeId: string;
  invitedByUserId: string;
  state: ChallengeInvitationState;
  createdAt: string;
}

export interface CreateChallengeRequest {
  title: string;
  description?: string | null;
  iconPublicId?: string | null;
  visibility: ChallengeVisibility;
  targetLevel: number;
  xpMultiplier: number;
  startsAt: string;
  endsAt: string;
  memberCap?: number | null;
}

export interface CreateChallengeResponse {
  challenge: Challenge;
  ownerMembership: ChallengeMembership;
  shareLink: ChallengeInvitation;
}

export interface ListChallengesResponse {
  challenges: Challenge[];
}

export interface GetChallengeResponse {
  challenge: Challenge;
  membership: ChallengeMembership | null;
  shareLink: ChallengeInvitation | null;
}

export interface ChallengeProgressResponse {
  progress: MembershipProgress;
}

export interface JoinChallengeRequest {
  invitationId: string;
}

export interface JoinChallengeResponse {
  challenge: Challenge;
  membership: ChallengeMembership;
}
