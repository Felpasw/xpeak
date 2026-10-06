using FluentValidation;

namespace Xpeak.Api.CheckIns.Dto;

public sealed class CreateCheckInRequestValidator : AbstractValidator<CreateCheckInRequest>
{
    public const int TitleMaxLength = 60;
    public const int NotesMaxLength = 280;
    public static readonly TimeSpan MaxBackfill = TimeSpan.FromDays(7);

    public CreateCheckInRequestValidator(TimeProvider time)
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithMessage("Category id is required.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .Must(t => !string.IsNullOrWhiteSpace(t))
            .WithMessage("Title must not be blank.")
            .Must(t => t.Trim().Length <= TitleMaxLength)
            .WithMessage($"Title must be {TitleMaxLength} characters or fewer.");

        RuleFor(x => x.PerformedAt!.Value)
            .Must(p => p <= time.GetUtcNow())
            .When(x => x.PerformedAt.HasValue)
            .WithMessage("performedAt must not be in the future.")
            .Must(p => time.GetUtcNow() - p <= MaxBackfill)
            .When(x => x.PerformedAt.HasValue)
            .WithMessage($"performedAt must be within the last {MaxBackfill.TotalDays:F0} days.")
            .OverridePropertyName(nameof(CreateCheckInRequest.PerformedAt));

        RuleFor(x => x.DurationMinutes!.Value)
            .GreaterThan(0)
            .When(x => x.DurationMinutes.HasValue)
            .WithMessage("Must be greater than 0 when provided.")
            .OverridePropertyName(nameof(CreateCheckInRequest.DurationMinutes));

        RuleFor(x => x.Notes!)
            .MaximumLength(NotesMaxLength)
            .When(x => x.Notes is not null)
            .WithMessage($"Must be {NotesMaxLength} characters or fewer.")
            .OverridePropertyName(nameof(CreateCheckInRequest.Notes));
    }
}
