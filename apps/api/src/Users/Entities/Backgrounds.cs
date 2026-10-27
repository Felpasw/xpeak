namespace Xpeak.Api.Users;

/// <summary>
/// Set of backgrounds the mobile is willing to render.
/// Backed on Postgres by a native <c>ENUM</c> type
/// (<c>backgrounds</c>): Npgsql serialises each member to its
/// snake_case name (<c>stars</c>, <c>lightning</c>,
/// <c>etched_accretion</c>) and the DB rejects anything else.
///
/// Mirrored on the client side by the TS <c>Backgrounds</c> enum
/// in <c>@xpeak/shared</c>; the two must stay in sync — adding
/// an option means (1) a new member here (+ a migration that
/// extends the Postgres type via <c>ALTER TYPE ... ADD VALUE</c>),
/// (2) a matching member on the TS enum, (3) a manifest in
/// <c>apps/mobile/src/components/backgrounds/registry.ts</c>.
/// </summary>
public enum Backgrounds
{
    Stars,
    Lightning,
    EtchedAccretion,
}
