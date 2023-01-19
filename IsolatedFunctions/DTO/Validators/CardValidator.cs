using Domain.Models;
using FluentValidation;

namespace IsolatedFunctions.DTO.Validators;

public class CardValidator : AbstractValidator<Card>
{
    public CardValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("CardId must be provided.");
        RuleFor(x => x.Name).Length(3, 50).WithMessage("Card name must be between 3 and 50 characters.");
        RuleFor(x => x.Body).Length(3, 200).WithMessage("Card name must be between 3 and 200 characters.");
        RuleFor(x=>x.Type).IsInEnum().WithMessage("CardType must be provided.");
    }
}
