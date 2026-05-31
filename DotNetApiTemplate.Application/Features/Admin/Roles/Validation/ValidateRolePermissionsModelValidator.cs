using DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;
using FluentValidation;

namespace DotNetApiTemplate.Application.Features.Admin.Roles.Validation;

public sealed class ValidateRolePermissionsModelValidator : AbstractValidator<ValidateRolePermissionsModel>
{
    public ValidateRolePermissionsModelValidator()
    {
        RuleForEach(model => model.Permissions)
            .NotEmpty()
            .WithMessage("Permissions must not contain empty values.");
    }
}
