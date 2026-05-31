using DotNetApiTemplate.Application.Features.Auth.Dtos;
using FluentValidation;

namespace DotNetApiTemplate.Application.Features.Auth.Validation;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email must be a valid email address.");
    }
}
