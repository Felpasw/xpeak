using FluentValidation;

namespace Xpeak.Api.Challenges.Dto;

public sealed class JoinChallengeRequestValidator : AbstractValidator<JoinChallengeRequest>
{
    public JoinChallengeRequestValidator()
    {
        RuleFor(x => x.InvitationId)
            .NotEqual(Guid.Empty)
            .WithMessage("invitationId is required.");
    }
}
