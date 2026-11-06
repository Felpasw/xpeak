/**
 * Mirror of the backend `Backgrounds` C# enum. The wire values
 * are snake_case to match the server's
 * `JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)` —
 * adding a member here means updating the C# enum in the same
 * commit (the backend rejects anything outside its own members).
 */
export enum Backgrounds {
  Stars = 'stars',
  Lightning = 'lightning',
  EtchedAccretion = 'etched_accretion',
  Matrix = 'matrix',
  Fluid = 'fluid',
  ScanlineBloom = 'scanline_bloom',
  DimensionalField = 'dimensional_field',
}

export interface User {
  id: string;
  username: string;
  email: string;
  avatarUrl: string | null;
  level: number;
  xp: number;
  currentStreakDays: number;
  longestStreakDays: number;
  background: Backgrounds | null;
  createdAt: string;
}

export interface AuthResponse {
  user: User;
}

export interface UpdateBackgroundRequest {
  background: Backgrounds;
}
