using FluentValidation;

namespace Xpeak.Api.Media.Dto;

public sealed class ConfirmMediaRequestValidator : AbstractValidator<ConfirmMediaRequest>
{
    private static readonly string[] AllowedKinds = ["photo", "video"];

    public ConfirmMediaRequestValidator()
    {
        RuleFor(r => r.Items)
            .NotEmpty().WithMessage("items must not be empty")
            .Must(items => items.Count <= PresignMediaRequestValidator.MaxItemsPerRequest)
                .WithMessage($"items must contain at most {PresignMediaRequestValidator.MaxItemsPerRequest} entries");

        RuleForEach(r => r.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.StorageKey)
                .NotEmpty().WithMessage("storage_key is required");
            item.RuleFor(i => i.Kind)
                .NotEmpty().WithMessage("kind is required")
                .Must(k => AllowedKinds.Contains(k, StringComparer.OrdinalIgnoreCase))
                    .WithMessage("kind must be 'photo' or 'video'");
            item.RuleFor(i => i.Position)
                .GreaterThanOrEqualTo(0).WithMessage("position must be zero or positive");
        });
    }
}
