using DotNetApiTemplate.Application.Features.System.Echo.Dtos;
using FluentValidation;

namespace DotNetApiTemplate.Application.Features.System.Echo.Validation;

public sealed class EchoCommandValidator : AbstractValidator<EchoCommand>
{
    public EchoCommandValidator()
    {
        RuleFor(command => command.Message)
            .NotEmpty()
            .WithMessage("Message is required.")
            .MaximumLength(500)
            .WithMessage("Message must not exceed 500 characters.");
    }
}
