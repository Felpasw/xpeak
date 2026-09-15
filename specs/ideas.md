# Product Ideas & Backlog

> Not prioritized. Not scoped. Not implemented. This document captures
> product ideas as they come up so nothing is lost between the initial
> brief (`README.md`) and the phased specs (`specs/NNN-*/`). Items
> here are candidates for future phases, not commitments.

## How to use this file

- Add ideas freely, grouped by theme.
- When an idea graduates into an actual phase, move it into
  `specs/NNN-slug/plan.md` and leave a short note here pointing to
  the spec.
- If an idea is discarded, keep it in a "Rejected" section at the
  bottom with a one-line reason (avoids relitigating).

---

## 1. XP & progression system (deep customization)

- **Variable XP bonus by frequency.** Beyond the current streak
  multiplier, allow tiered bonuses (e.g., 3 days = 1.1x, 7 days =
  1.25x, 14 days = 1.5x, 30 days = 2x). The tier table itself is
  editable (see "customization" below).
- **Fully customizable XP values.** Every XP source (category base,
  streak tier, group bonus, challenge bonus, exploration bonus,
  achievement completion) is stored as configurable data, not
  hardcoded. Admins (or challenge owners, or the user themselves in
  solo mode?) can edit these values within safe bounds.
- **Customizable level curve.** The XP required to reach each level
  is defined by a formula OR a lookup table. Both are editable. The
  system ships with a default logarithmic curve but nothing else in
  the code depends on it being fixed.
- **Personalizable XP-per-check-in.** A user (or the owner of a
  challenge) can define per-category XP overrides for their scope.
  Guard against exploit with min/max caps.
- **Workout log XP contribution.** The check-in log entry itself
  (title, description, media count, quality) can influence XP within
  a small band (e.g., a check-in with 3 media items +5% XP, capped).
  Full rules TBD.

**Open question:** who has editing authority for these values?
- Global app admin only?
- Challenge owner within their challenge?
- Individual user for their own solo runs?
- All of the above, with layered precedence?

## 2. Group and social bonuses

- **Group workout bonus.** When multiple members of a group check in
  at the same location within a small time window, everyone in the
  session gets a multiplier (e.g., 1.2x for 2 people, 1.5x for 3+).
- **Friend challenges.** 1v1 or small-group challenges between
  connected users, separate from public/private challenges. Faster
  setup than a full challenge — closer to "let's race for 7 days".
- **Group streak.** A group has its own collective streak: at least
  N% of members must check in on the same day. Breaking it is a
  bigger deal than breaking a personal streak — bigger bonus while
  active, no punishment when it dies.

## 2.5. Social graph (friends) & timeline

The bridge that turns XPeak from a personal tracker into a network.

### 2.5.1. Adding friends

- **Add by username.** Enter or search `@username` → send friend
  request → other user accepts (or auto-accepts if setting is on).
- **Model choice (still to decide):**
  - **Mutual friends** (Facebook-style): both sides must accept.
    Recommended default for a fitness/progression app where the
    feed shares personal media (check-in photos/videos).
  - **Follow** (Twitter/Instagram-style): unilateral. Anyone can
    follow anyone (unless blocked). Simpler, but weaker privacy.
  - **Hybrid:** users can be "public" (follow model) or "private"
    (friend request required) via profile setting.
- **Discovery:**
  - Search by exact `@username` (case-insensitive).
  - Optional later: search by name/handle prefix, suggest by
    mutual friends, suggest by shared challenges.
- **Cap:** initial soft limit on friends (e.g., 500) to keep the
  feed sane and the DB queries bounded.

### 2.5.2. Friend profile view (read-only)

Visiting a friend's profile shows:
- `@username`, avatar, banner, frame, current title.
- Level, XP, current streak, longest streak.
- Trophy case (insignias, medals, trophies earned).
- Recent activity summary (last N check-ins, per privacy rules).
- Mutual friends count (if applicable).
- Buttons: **Challenge** (starts a friend challenge — Phase 15),
  **Remove friend**, **Block**.

### 2.5.3. Timeline / activity feed

Chronological feed of events from friends (and optionally from
yourself, toggleable).

**Event types (feed items):**
- Check-in posted (with media preview and caption).
- Level up (`@felpa reached level 12 — Warrior`).
- Streak milestone hit (7/30/100/365-day medal earned).
- Insignia earned (challenge completion).
- Trophy earned (challenge victory).
- Challenge started / finished.
- Joined a group / friend request accepted (optional, muted by
  default).

**Storage model (sketch):**
- `activity_events` table: `id, user_id, kind, subject_id,
  subject_type, payload (JSONB), created_at`.
- Written by domain services (check-in service emits `check_in.posted`,
  achievement service emits `medal.earned`, etc.).
- Feed query fans out from friendship: `WHERE user_id IN (friends
  of :me)` ordered by `created_at DESC`, paginated.
- Cache and denormalize once volume matters (fan-out-on-write to a
  per-user timeline table); start with fan-out-on-read.

**Interactions on feed items (future, not initial):**
- React (emoji).
- Comment.
- Share to another user.

### 2.5.4. Privacy & blocking

- **Per-user visibility settings:**
  - "Who can send me friend requests?" (everyone / friends of
    friends / no one).
  - "Who can see my check-ins?" (friends / friends of friends /
    only me).
  - "Who can see my trophies/medals?" (public / friends / only me).
  - "Show my check-ins in friends' feed?" (yes / no).
- **Blocking:**
  - Blocked users can't see profile, can't send requests, don't
    appear in feed.
  - Two-way effect: neither side sees the other anywhere.
- **Reporting:** report a user for abuse / fake account.
  Moderation queue is a much later feature.

### 2.5.5. Notifications tied to the social graph

Feed events that generate a push:
- Friend request received / accepted.
- A friend just beat your streak record.
- A friend tagged you in a check-in (later).
- A friend challenged you (Phase 15).

Push infrastructure lives in a later phase; this phase just needs
to write to a `notifications` table.

## 2.6. Global rankings & discovery

The public read-only surface where users see how they stack up
against everyone (not just friends).

### 2.6.1. Ranking scopes

Multiple leaderboards, each with its own query and cache:

- **Global — All-time XP** — top N users by total XP.
- **Global — Weekly XP** — XP earned in the current ISO week
  (resets Monday 00:00 in the user's timezone, computed in UTC and
  displayed in local time).
- **Global — Monthly XP** — same, month-scoped.
- **Global — Longest current streak** — top by `current_streak_days`.
- **Global — Longest streak ever** — top by `longest_streak_days`.
- **Per-category** — top XP earners for each category (legs,
  running, mobility, etc.).
- **Regional** (later, requires exploration/locations from
  `ideas.md` §3) — top by city / country. Opt-in per user.
- **Friends** — same board scoped to `friends of :me`. Uses the
  social graph from Phase 12.

### 2.6.2. Enrollment

**Decision (locked):** every new user is enrolled in the global
leaderboard automatically the moment they earn their first XP.

- **Opt-out toggle** in privacy settings: "Show me on the global
  leaderboard" (default: on). Toggling off hides the user from all
  global boards immediately.
- Regardless of the toggle, the user always sees their own rank
  privately (`GET /rankings/me`).

### 2.6.3. Ranking row shape

Each row on a board shows:
- Rank (`#1`, `#2`, ...).
- `@username`, avatar + frame, current title.
- Scored value (XP, streak days, whatever the board sorts by).
- Level.
- Delta from previous period (weekly: "▲ 4 vs last week").
- Row is clickable → opens friend/public profile view
  (Phase 12 read view; if not friends, the visibility settings
  from §2.5.4 decide what's rendered).

### 2.6.4. Storage & computation

- Compute weekly/monthly boards from a materialized view or an Oban
  job that snapshots into a `rankings_snapshots` table on a schedule
  (e.g., hourly for current period, on rollover for closed periods).
- All-time boards can be a straight `ORDER BY xp DESC LIMIT 100`
  with an index on `xp DESC` (with pagination).
- Cache the top 100 in-memory (ETS via Cachex/Nebulex) or in Redis
  when we add it, with a short TTL (60–300s) to avoid hammering the
  DB on public views.

### 2.6.5. Cross-links with the social graph

- On a leaderboard row, if the user is not yet a friend and the
  target profile is public / accepts requests, show an inline
  "Add friend" action.
- Global leaderboards are a primary user-acquisition surface for
  the friend graph.

## 2.7. Challenge visibility rules (recap + detail)

Already sketched in `README.md` §3.6; fleshing out the rules that
we'll hold to across all challenge phases (Solo, Friend, Group):

### 2.7.1. Two visibility levels

- **Public.**
  - Appears in challenge search / discovery.
  - Any user can find and join (subject to member cap and any
    entry rules the owner sets).
  - Featured on the challenges tab as browsable content.
- **Private.**
  - **Not** listed anywhere.
  - **Access only via share link** (or share code).
  - The link is unguessable (signed token, e.g., ULID + HMAC).
  - Link can be revoked, has optional max-uses and expiration.

### 2.7.2. Modality × visibility matrix

|                   | Public                                | Private                                    |
|-------------------|---------------------------------------|--------------------------------------------|
| Solo              | Owner shares their solo run as a public challenge others can spin up their own copy of | Owner-only, unlinked |
| Friend            | Not applicable (friend challenges are, by definition, invite-only) | Invite via share link to specific friends |
| Group             | Browseable, joinable by anyone until cap | Share-link only; can be co-owned; joining requires the link |

### 2.7.3. Share-link mechanics

- Endpoint: `POST /challenges/:id/invites` returns
  `{ url: "https://xpeak.app/c/<code>", code: "<code>",
  expires_at, max_uses, uses }`.
- Endpoint: `POST /challenges/join` with `{ code }` joins if valid.
- The frontend `/c/<code>` route deep-links into the app
  (`xpeak://challenges/join?code=...`) and shows a preview card
  before the user confirms.
- Multiple active links per challenge allowed (e.g., one link per
  channel: WhatsApp vs. Instagram vs. Discord).

### 2.7.4. Discovery for public challenges

- `GET /challenges?search=...&category=...&status=open` with
  filters: category, duration, difficulty, participant count,
  starting date.
- Featured challenges section curated (later — for now, sort by
  popularity + recency).

## 2.8. Personal metrics & yearly retrospective

Two related surfaces that give a user a reflective view of their
own journey.

### 2.8.1. Metrics dashboard (always-on, front and center)

**Decision (locked):** the metrics surface is not a hidden screen —
it's part of what the user sees **every time they open the app**.
Options for exactly where it lands (to decide when building
Phase 21):

- Option A — the profile/home tab shows a metrics snapshot at the
  top (totals + streak calendar + XP-to-next-level), with a "See
  full metrics" link into a dedicated screen for the deeper cuts
  (multiplier history, category heatmap, best days).
- Option B — a dedicated "Metrics" tab in the bottom navigation,
  always one tap away, pre-loaded on session start.

Either way: data is fetched on app open, cached client-side so
navigation stays instant, and refreshed opportunistically after any
event that could change it (new check-in, level-up, streak update).

A screen the user can also open any time to see their own
progression data in more detail.

- **Totals:**
  - Lifetime XP, level, current title.
  - Total check-ins.
  - Total training days (distinct days with at least one check-in).
  - Current streak, longest streak.
- **Time slicing:** all metrics filterable by "last 7 days", "last
  30 days", "this month", "this year", "all time", or custom range.
- **Category breakdown:** XP per category (bar chart), % of
  training days per category (donut), heatmap of most-trained
  categories vs. day of week.
- **Streak calendar:** GitHub-style contribution heatmap of the
  current year — one cell per day, intensity based on XP earned.
- **Best days / weeks:** highest XP day ever, best week, best
  month.
- **Multiplier history:** how much XP came from streak bonus vs.
  category weight vs. challenge bonus vs. group bonus (stacked
  bar over time).
- **Progression to next level:** progress bar + estimated days to
  next level at current pace.

### 2.8.2. Yearly retrospective ("Year in XPeak")

A storytelling-style recap released at end of the year (or on the
user's join anniversary — decision below).

**Content per recap:**
- Total XP earned this year (with comparison to previous year if
  available).
- Number of training days.
- Categories trained (with the top 3 highlighted).
- Longest streak of the year.
- Biggest single-day XP.
- Medals earned this year.
- Insignias earned this year.
- Trophies earned this year.
- Challenges joined / completed / won.
- Friends made this year.
- Level progression (start-of-year level → end-of-year level).
- Global rank at year-end (if opted in).
- "You out-trained X% of XPeak users this year" (opt-in
  comparative stat).

**Format:**
- Full-screen scrollable "story" (Instagram-story-like), one card
  per stat, with the user's active theme applied to backgrounds
  and highlights.
- Auto-play with tap-to-skip / hold-to-pause.
- Each card is shareable individually (deep-link back to XPeak).
- Final card: full recap image the user can save/share to
  Instagram/WhatsApp.

**Generation:**
- Batch job (Oban) runs after year rollover for every user with
  at least one check-in that year.
- Result cached as a `recaps` record: `user_id`, `period` (e.g.,
  `2026`), `payload (JSONB)`, `generated_at`.
- Endpoint: `GET /recaps/:period` returns the payload; if not
  generated yet, returns 202 with an ETA.

### 2.8.3. Decisions

**Locked:**
- ✅ Metrics surface is always-on (see §2.8.1) — user sees it every
  time they open the app.
- ✅ Yearly recap must be shareable (per-card + final summary
  image).

**Still open (decide when Phase 21 is scheduled, not now):**
1. **Recap trigger date** — calendar year rollover (Dec → Jan) vs.
   per-user anniversary (12 months after signup).
2. **Charting library on mobile** — Recharts / Visx / Chart.js /
   hand-rolled SVG. Deferred to when we build.
3. **Comparative stats privacy** — "you out-trained X% of users"
   requires aggregate queries. OK to show, too intrusive, or make
   it opt-in?
4. **Shareable rendering** — pre-rendered PNG on the backend
   (canvas / Playwright screenshot) vs. client-side canvas render.
   Backend is prettier and consistent; client is cheaper.
5. **Home-surface layout** — Option A (snapshot on profile + deeper
   screen) vs. Option B (dedicated Metrics tab) from §2.8.1.

### 2.8.4. Reference to XP customization

The "variable XP per category, editable by admins/challenge owners"
requirement already lives in the roadmap:

- **Phase 4** (Categories & pure XP domain) — every category has a
  `base_xp` and a `weight_multiplier` from the start, so XP is
  category-variable by design from the first check-in.
- **Phase 9** (Editable XP & level configuration) — the admin
  surface (the "board") to tweak those values without a deploy.
  This is where the promise of "everything customizable via a
  board" lands.

No new phase needed for the category-XP requirement — it's already
scheduled.

## 3. Exploration mechanic (gyms / locations)

- **Visited locations.** Each unique gym / place unlocks a "location"
  entry on the profile. Visiting N different locations grants XP
  and/or a badge.
- **Location-based challenges.** Local admins (owners of a gym?)
  create location-tied challenges. Only check-ins from that GPS
  radius count.
- **Regional/city leaderboards.** Optional, opt-in for privacy.

**Open question:** how do we source the gym database? User-submitted?
Google Places? Manual admin list?

## 4. Achievements, medals, and trophies

Three overlapping-but-distinct collectible categories:

- **Insignias / badges — challenge completion.** Awarded on
  finishing a challenge. Different tiers based on rank (gold/silver/
  bronze podium, participation for the rest).
- **Medals — streak milestones.** 7-day medal, 30-day, 100-day,
  365-day, and so on. Non-competitive, individual.
- **Trophies — challenge victory.** Distinct visual from insignias,
  reserved for #1 in group challenges or completing a hardcore
  solo challenge.

**Trophy artwork is customizable, always.** Every trophy has an image
or GIF asset that can be chosen freely:
- Default trophy art shipped by the app (theme-aware, matches the
  user's active theme).
- Custom trophy uploaded by the challenge owner when creating the
  challenge (see §6 — Challenge customization).
- Optional pre-made catalog the challenge owner can pick from
  (curated by the app) as a middle ground between default and full
  upload.
- Format: static image (PNG/JPG/WebP) or animated (GIF/APNG/animated
  WebP), following the cross-cutting rules in §8.

Same logic applies to insignias and medals when they're
challenge-scoped or unlocked cosmetics: default art from the app
catalog, freely swappable when the scope allows.

All three are displayable on the profile (trophy case grid) and
optionally on the feed when unlocked.

## 5. Profile customization

- **Avatar.** Static image or animated GIF/APNG. Frame around the
  avatar can be a separate cosmetic ("moldura" / avatar frame)
  earned or purchased.
- **Profile banner.** Wide cover image at the top of the profile.
  Can be static image or GIF.
- **Avatar frame.** Overlay around the avatar — unlocks tied to
  achievements (e.g., unlock the "War God" frame at level 100).
- **Title display.** Which unlocked title is shown next to the
  username on the feed (defaults to the highest-level one, but
  user can pin any unlocked title).
- **Color scheme / theme accent.** Small palette tweak the user
  chooses; also unlockable.

## 6. Challenge customization

- **Challenge banner.** Wide hero image for the challenge card and
  detail page. Static image or GIF.
- **Custom XP rules per challenge.** Challenge owner can override
  per-category XP within the challenge scope (respecting global
  min/max caps).
- **Custom win rules per challenge.** Total XP, consecutive days,
  category-specific target, hybrid (e.g., "15 leg sessions AND 500
  km running in 60 days").
- **Custom trophy asset.** Challenge owner can upload the trophy
  image/GIF awarded to the winner(s).

## 7. Level visualization ("evolution forms")

Inspired by RPG evolution art:

- **Per-level (or per-tier) visual form.** Reaching level X shows an
  avatar illustration representing your "form" (Apprentice, Warrior,
  Knight, Mage, War God — matching the title tiers from `README.md`
  §3.4).
- **Customizable forms.** The forms are asset packs tied to themes.
  The user picks a theme and the sequence of illustrations changes
  accordingly.
- **Static image or GIF per form.** Animated forms unlock at higher
  tiers for prestige.
- **Optional: user-uploaded forms.** Advanced customization where a
  paying user (see monetization TBD in `README.md` §10) can upload
  their own illustration set. Requires moderation.

## 8. Media format policy (cross-cutting)

Every visual customization slot (avatar, avatar frame, banner,
challenge banner, level form, achievement icon, trophy) accepts:

- Static image: PNG, JPG, WebP.
- Animated image: GIF, APNG, WebP-animated.
- **Not video** (video is reserved for check-in proof).

Implications:
- Storage layer must handle animated formats without conversion
  losing frames.
- Size caps per slot (e.g., avatar max 2 MB, banner max 5 MB).
- The feed must render animated assets without janking scroll
  performance (lazy load + intersection observer + `img
  loading="lazy"`).

## 9. Workout logs (media in check-in — recap)

Already in `README.md` §3.1, but noting here for completeness:

- Every check-in requires at least one media attachment (photo or
  video).
- Multiple attachments allowed.
- Media is private to the check-in owner and the groups where the
  check-in is shared.
- Later idea: attach a "form" or "quality" rating on the check-in
  that influences XP within a bounded band (see §1 last bullet).

## 10. Meta idea: "everything is data, not hardcoded"

Recurring pattern across the ideas above:

- XP values, level curves, streak multipliers, group bonuses,
  achievement thresholds, cosmetics catalogs — **all live in
  editable configuration**, not in code.
- The domain layer reads these values from a config service backed
  by the database (or a JSON file bundled at boot for the first
  version).
- Migrations are how we ship new default values; runtime edits
  come from admins/challenge owners.

**Trade-off:** more flexibility ⇔ more surface for balancing bugs
and exploits. Requires a small admin UI (or IEx console + audit
log at minimum) and per-scope caps enforced at the domain layer.

---

## Impact on the phase roadmap (`README.md` §9)

Ideas here suggest new or expanded phases beyond the current 11:

- **Phase X — Achievements & cosmetics catalog** (badges, medals,
  trophies, avatar frames, banners, level forms). Storage +
  read-only API + minimal admin UI.
- **Phase Y — Editable XP / level configuration.** Admin surface,
  audit log, safe caps.
- **Phase Z — Exploration / locations.** Gym database source +
  location-tied XP + regional leaderboards.
- **Phase W — Group bonuses & group streak.** Adds detection
  logic for co-located check-ins and collective streak state.
- **Phase V — Friend challenges.** Lightweight 1v1/small-group
  challenge type separate from the full challenge model.

To be sequenced into `specs/001-xpeak-mvp/tasks.md` (or split into
their own `specs/NNN-*/` folders) once we finish Phases 1–2 and
have a clearer sense of priority.

---

## Rejected / deferred (with reason)

_None yet._
