using DotNetApiTemplate.Application.Features.Auth.Dtos;
using FluentValidation;

namespace DotNetApiTemplate.Application.Features.Auth.Validation;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.Token)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Token is required.");

        RuleFor(command => command.NewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("New password is required.")
            .MinimumLength(10)
            .WithMessage("Password must be at least 10 characters long.")
            .Matches("[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]")
            .WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]")
            .WithMessage("Password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]")
            .WithMessage("Password must contain at least one symbol.");

        RuleFor(command => command.ConfirmPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Confirm password is required.")
            .Equal(command => command.NewPassword)
            .WithMessage("Confirm password must match new password.");
    }
}
