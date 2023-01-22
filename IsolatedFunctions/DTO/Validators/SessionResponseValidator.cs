using Domain.Models;
using FluentValidation;

namespace IsolatedFunctions.DTO.Validators;

public class SessionResponseValidator : AbstractValidator<SessionResponse>
{
    public SessionResponseValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("Session id must be provided.");
        RuleFor(x => x.ResponseType).IsInEnum().WithMessage("ResponseType must be provided.");
        RuleFor(x => x.CardNumber).GreaterThanOrEqualTo(0).WithMessage("CardNumber must be greater than or equal to 0.");
        RuleFor(x => x.Response).Length(1,300).WithMessage("Response must be between 1 and 300 characters.");
    }
}
