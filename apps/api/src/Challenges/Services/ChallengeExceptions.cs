namespace Xpeak.Api.Challenges.Services;

/// <summary>Thrown when <c>CreateChallengeInput.CategoryIds</c> is
/// empty — a challenge with no eligible categories cannot score any
/// check-in and would always Fail at <c>ends_at</c>.</summary>
public sealed class NoCategoriesException()
    : InvalidOperationException("A challenge must target at least one category.");

/// <summary>Thrown when a referenced category id does not exist in
/// the <c>categories</c> table. Carries the offending id so the
/// endpoint can map to a 404 / 422 payload.</summary>
public sealed class CategoryNotFoundException(Guid categoryId)
    : InvalidOperationException($"Category '{categoryId}' not found.")
{
    public Guid CategoryId { get; } = categoryId;
}
