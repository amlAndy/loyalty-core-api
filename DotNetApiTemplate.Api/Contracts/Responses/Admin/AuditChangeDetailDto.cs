namespace DotNetApiTemplate.Api.Contracts.Responses.Admin;

public sealed class AuditChangeDetailDto
{
    public string Field { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public object? BeforeValue { get; init; }
    public object? AfterValue { get; init; }
}
