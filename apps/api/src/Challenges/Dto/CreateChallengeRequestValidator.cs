using FluentValidation;

namespace Xpeak.Api.Challenges.Dto;

public sealed class CreateChallengeRequestValidator : AbstractValidator<CreateChallengeRequest>
{
    public const int TitleMaxLength = 80;
    public const int DescriptionMaxLength = 400;
    public const decimal XpMultiplierFloor = 0.5m;
    public const decimal XpMultiplierCeiling = 1.5m;
    public const int TargetLevelMin = 1;
    public const int TargetLevelMax = 999;

    public CreateChallengeRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .Must(t => !string.IsNullOrWhiteSpace(t))
            .WithMessage("Title must not be blank.")
            .Must(t => t.Trim().Length <= TitleMaxLength)
            .WithMessage($"Title must be {TitleMaxLength} characters or fewer.");

        RuleFor(x => x.Description!)
            .MaximumLength(DescriptionMaxLength)
            .When(x => x.Description is not null)
            .WithMessage($"Must be {DescriptionMaxLength} characters or fewer.")
            .OverridePropertyName(nameof(CreateChallengeRequest.Description));

        RuleFor(x => x.TargetLevel)
            .InclusiveBetween(TargetLevelMin, TargetLevelMax)
            .WithMessage($"targetLevel must be between {TargetLevelMin} and {TargetLevelMax}.");

        RuleFor(x => x.XpMultiplier)
            .InclusiveBetween(XpMultiplierFloor, XpMultiplierCeiling)
            .WithMessage($"xpMultiplier must be between {XpMultiplierFloor} and {XpMultiplierCeiling}.");

        RuleFor(x => x)
            .Must(x => x.StartsAt < x.EndsAt)
            .WithMessage("startsAt must be strictly before endsAt.")
            .OverridePropertyName(nameof(CreateChallengeRequest.EndsAt));

        RuleFor(x => x.MemberCap!.Value)
            .GreaterThan(0)
            .When(x => x.MemberCap.HasValue)
            .WithMessage("memberCap must be positive when provided.")
            .OverridePropertyName(nameof(CreateChallengeRequest.MemberCap));

        RuleFor(x => x.CategoryIds)
            .NotNull()
            .WithMessage("categoryIds is required.")
            .Must(ids => ids.Count > 0)
            .WithMessage("categoryIds must contain at least one category.");
    }
}
