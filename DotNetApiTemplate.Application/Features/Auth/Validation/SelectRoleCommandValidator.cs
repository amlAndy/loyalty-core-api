using DotNetApiTemplate.Application.Features.Auth.Dtos;
using FluentValidation;

namespace DotNetApiTemplate.Application.Features.Auth.Validation;

public sealed class SelectRoleCommandValidator : AbstractValidator<SelectRoleCommand>
{
    public SelectRoleCommandValidator()
    {
        RuleFor(command => command.RoleId)
            .NotEmpty()
            .WithMessage("RoleId is required.");
    }
}
