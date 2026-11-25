using FluentValidation;
using Xpeak.Api.Social.Dto;

namespace Xpeak.Api.Social.Validators;

public sealed class CreateCommentRequestValidator
    : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(c => c.Body)
            .Must(b => !string.IsNullOrWhiteSpace(b))
            .WithErrorCode("empty_body");

        RuleFor(c => c.Body)
            .Must(b => (b ?? string.Empty).Length <= 280)
            .WithErrorCode("body_too_long");
    }
}
