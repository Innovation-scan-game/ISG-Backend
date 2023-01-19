using Domain.Models;
using FluentValidation;

namespace IsolatedFunctions.DTO.Validators;

public class GameSessionValidator : AbstractValidator<GameSession>
{
    public GameSessionValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("GameSessionId must be provided.");
        RuleFor(x => x.Cards).NotEmpty().WithMessage("Cards must be provided.");
        RuleFor(x => x.Rounds).InclusiveBetween(3, 9).WithMessage("Rounds must be between 3 and 9.");
    }
}
