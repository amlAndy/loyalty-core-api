using DotNetApiTemplate.Application.Features.Admin.Audit.Dtos;

namespace DotNetApiTemplate.Application.Features.Admin.Audit.Interfaces;

public interface IAuditService
{
    Task<AuditSearchResult> SearchAsync(AuditSearchCriteria criteria, CancellationToken cancellationToken = default);
    Task<AuditFilterOptionsModel> GetFilterOptionsAsync(CancellationToken cancellationToken = default);
}
