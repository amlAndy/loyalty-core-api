using DotNetApiTemplate.Application.Features.Auth.Dtos;
using FluentValidation;

namespace DotNetApiTemplate.Application.Features.Auth.Validation;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email must be a valid email address.");

        RuleFor(command => command.Password)
            .NotEmpty()
            .WithMessage("Password is required.");
    }
}
