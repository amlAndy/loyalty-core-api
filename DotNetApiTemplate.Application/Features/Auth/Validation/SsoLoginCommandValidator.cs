using DotNetApiTemplate.Application.Features.Auth.Dtos;
using FluentValidation;

namespace DotNetApiTemplate.Application.Features.Auth.Validation;

public sealed class SsoLoginCommandValidator : AbstractValidator<SsoLoginCommand>
{
    public SsoLoginCommandValidator()
    {
        RuleFor(command => command.Token)
            .NotEmpty()
            .WithMessage("Token is required.");
    }
}
