using Domain.Models;
using FluentValidation;

namespace IsolatedFunctions.DTO.Validators;

public class GameSessionValidator : AbstractValidator<GameSession>
{
    public GameSessionValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("GameSessionId must be provided.");
        RuleFor(x => x.Rounds).InclusiveBetween(3, 9).WithMessage("Rounds must be between 3 and 9.");
        RuleFor(x => x.RoundDurationSeconds).InclusiveBetween(10,900).WithMessage("round duration must be between 10 and 900 seconds.");
    }
}
