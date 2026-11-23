using FluentValidation;
using Xpeak.Api.Social.Dto;
using Xpeak.Api.Social.Palette;

namespace Xpeak.Api.Social.Validators;

public sealed class ToggleReactionRequestValidator
    : AbstractValidator<ToggleReactionRequest>
{
    public ToggleReactionRequestValidator()
    {
        RuleFor(r => r.Emoji)
            .NotEmpty().WithErrorCode("missing_emoji")
            .Must(ReactionPalette.Contains).WithErrorCode("invalid_emoji");
    }
}
