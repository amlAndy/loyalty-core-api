using DotNetApiTemplate.Application.Features.Admin.Users.Dtos;
using FluentValidation;

namespace DotNetApiTemplate.Application.Features.Admin.Users.Validation;

public sealed class AdminUserUpsertModelValidator : AbstractValidator<AdminUserUpsertModel>
{
    public AdminUserUpsertModelValidator()
    {
        RuleFor(model => model.DisplayName)
            .NotEmpty()
            .WithMessage("DisplayName is required.");

        RuleFor(model => model.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email must be a valid email address.");

        RuleFor(model => model.Status)
            .NotEmpty()
            .WithMessage("Status is required.")
            .Must(status => AdminUserStatuses.Values.Contains(status))
            .WithMessage("Status is invalid.");

        RuleFor(model => model.RoleIds)
            .NotEmpty()
            .WithMessage("RoleIds is required.");

        RuleForEach(model => model.RoleIds)
            .NotEmpty()
            .WithMessage("RoleIds must not contain empty values.");
    }
}
