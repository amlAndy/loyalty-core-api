namespace DotNetApiTemplate.Api.Contracts.Responses.Admin;

public sealed class AuditFilterOptionsDto
{
    public AuditFilterOptionDto[] Actions { get; init; } = [];
    public AuditFilterOptionDto[] EntityTypes { get; init; } = [];
    public AuditFilterOptionDto[] ActingUsers { get; init; } = [];
}
