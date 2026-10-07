export type ChallengeVisibility = 'public' | 'private';

export type ChallengeState = 'active' | 'completed' | 'failed';

export type ChallengeMembershipRole = 'owner' | 'member';

export type ChallengeMembershipResult = 'completed' | 'failed';

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
  categoryIds: string[];
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
  categoryIds: string[];
}

export interface CreateChallengeResponse {
  challenge: Challenge;
  ownerMembership: ChallengeMembership;
}

export interface ListChallengesResponse {
  challenges: Challenge[];
}

export interface GetChallengeResponse {
  challenge: Challenge;
  membership: ChallengeMembership | null;
}

export interface ChallengeProgressResponse {
  progress: MembershipProgress;
}
