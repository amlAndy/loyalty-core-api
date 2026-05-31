using DotNetApiTemplate.Application.Features.Admin.Audit.Dtos;

namespace DotNetApiTemplate.Application.Features.Admin.Audit.Interfaces;

public interface IAuditWriter
{
    Task WriteAsync(AuditWriteEntryModel entry, CancellationToken cancellationToken = default);
}
