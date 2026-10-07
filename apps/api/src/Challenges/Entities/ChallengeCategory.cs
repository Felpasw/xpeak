namespace Xpeak.Api.Challenges.Entities;

/// <summary>
/// Many-to-many join: which category rows count toward a given
/// challenge. The pair <c>(challenge_id, category_id)</c> is unique.
/// </summary>
public sealed class ChallengeCategory
{
    public Guid Id { get; set; }

    public Guid ChallengeId { get; set; }

    public Guid CategoryId { get; set; }
}
