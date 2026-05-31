namespace DotNetApiTemplate.Application.Features.Admin.Audit.Dtos;

public sealed class AuditFilterOptionsModel
{
    public AuditFilterOptionModel[] Actions { get; init; } = [];
    public AuditFilterOptionModel[] EntityTypes { get; init; } = [];
    public AuditFilterOptionModel[] ActingUsers { get; init; } = [];
}
