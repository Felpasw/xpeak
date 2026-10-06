using FluentValidation;

namespace Xpeak.Api.Media.Dto;

public sealed class PresignMediaRequestValidator : AbstractValidator<PresignMediaRequest>
{
    public const int MaxItemsPerRequest = 10;
    private static readonly string[] AllowedKinds = ["photo", "video"];

    public PresignMediaRequestValidator()
    {
        RuleFor(r => r.Items)
            .NotEmpty().WithMessage("items must not be empty")
            .Must(items => items.Count <= MaxItemsPerRequest)
                .WithMessage($"items must contain at most {MaxItemsPerRequest} entries");

        RuleForEach(r => r.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Kind)
                .NotEmpty().WithMessage("kind is required")
                .Must(k => AllowedKinds.Contains(k, StringComparer.OrdinalIgnoreCase))
                    .WithMessage("kind must be 'photo' or 'video'");
        });
    }
}
