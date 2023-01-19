using Domain.Models;
using FluentValidation;

namespace IsolatedFunctions.DTO.Validators;

public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.Name).Length(3, 30).WithMessage("User name must be between 3 and 30 characters.");
        RuleFor(x => x.Email).EmailAddress().WithMessage("Email must be a valid email address.");
        RuleFor(x => x.Password).Length(8, 50).WithMessage("Password must be between 8 and 50 characters.");
        RuleFor(x => x.Role).IsInEnum().WithMessage("UserRole must be provided.");
    }
}
