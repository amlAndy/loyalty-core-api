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

        RuleFor(command => command.Screen)
            .NotEmpty()
            .WithMessage("Screen is required.")
            .Must(screen => LoginScreenTypes.Values.Contains(screen, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Screen must be one of: member, business, admin.");
    }
}
