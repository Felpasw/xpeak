using FluentValidation;
using Xpeak.Api.Users;

namespace Xpeak.Api.Auth.Core;

public sealed class UpdateBackgroundRequestValidator : AbstractValidator<UpdateBackgroundRequest>
{
    public UpdateBackgroundRequestValidator()
    {
        RuleFor(x => x.Background)
            .NotEmpty()
            .WithMessage("Background is required.");

        RuleFor(x => x.Background)
            .Must(v => BackgroundsExtensions.TryParseWire(v!, out _))
            .When(x => !string.IsNullOrEmpty(x.Background))
            .WithMessage(_ =>
                "Must be one of: " + string.Join(", ", BackgroundsExtensions.WireValues));
    }
}
