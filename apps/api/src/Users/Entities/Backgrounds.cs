using System.Text.Json;

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
    Matrix,
    Fluid,
    ScanlineBloom,
    DimensionalField,
}

/// <summary>Wire-form parsing for <see cref="Backgrounds"/>. The
/// snake_case keys are derived from the enum members through the
/// same naming policy the global <c>JsonStringEnumConverter</c>
/// uses, so request parsing and response serialisation stay in
/// lockstep without a hand-maintained table.</summary>
public static class BackgroundsExtensions
{
    private static readonly IReadOnlyDictionary<string, Backgrounds> WIRE_MAP =
        Enum.GetValues<Backgrounds>()
            .ToDictionary(
                e => JsonNamingPolicy.SnakeCaseLower.ConvertName(e.ToString()),
                e => e);

    public static IReadOnlyCollection<string> WireValues => (IReadOnlyCollection<string>)WIRE_MAP.Keys;

    public static bool TryParseWire(string value, out Backgrounds parsed) =>
        WIRE_MAP.TryGetValue(value, out parsed);
}
