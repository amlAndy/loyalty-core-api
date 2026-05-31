namespace DotNetApiTemplate.Application.Features.Admin.Audit.Dtos;

public sealed class AuditWriteDetailModel
{
    public string Field { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public object? BeforeValue { get; init; }
    public object? AfterValue { get; init; }
}
