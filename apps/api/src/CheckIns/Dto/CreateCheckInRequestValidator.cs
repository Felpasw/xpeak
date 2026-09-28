using FluentValidation;

namespace Xpeak.Api.CheckIns.Dto;

public sealed class CreateCheckInRequestValidator : AbstractValidator<CreateCheckInRequest>
{
    public CreateCheckInRequestValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithMessage("Category id is required.");

        RuleFor(x => x.DurationMinutes!.Value)
            .GreaterThan(0)
            .When(x => x.DurationMinutes.HasValue)
            .WithMessage("Must be greater than 0 when provided.")
            .OverridePropertyName(nameof(CreateCheckInRequest.DurationMinutes));

        RuleFor(x => x.Notes!)
            .MaximumLength(280)
            .When(x => x.Notes is not null)
            .WithMessage("Must be 280 characters or fewer.")
            .OverridePropertyName(nameof(CreateCheckInRequest.Notes));
    }
}
