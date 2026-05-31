using DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;
using FluentValidation;

namespace DotNetApiTemplate.Application.Features.Admin.Roles.Validation;

public sealed class AdminRoleUpsertModelValidator : AbstractValidator<AdminRoleUpsertModel>
{
    public AdminRoleUpsertModelValidator()
    {
        RuleFor(model => model.Name)
            .NotEmpty()
            .WithMessage("Name is required.");

        RuleFor(model => model.ParentRoleId)
            .NotEmpty()
            .WithMessage("ParentRoleId is required.");

        RuleForEach(model => model.Permissions)
            .NotEmpty()
            .WithMessage("Permissions must not contain empty values.");
    }
}
