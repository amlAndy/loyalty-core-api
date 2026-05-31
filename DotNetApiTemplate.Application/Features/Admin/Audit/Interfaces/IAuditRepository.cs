using DotNetApiTemplate.Application.Features.Admin.Audit.Dtos;

namespace DotNetApiTemplate.Application.Features.Admin.Audit.Interfaces;

public interface IAuditRepository
{
    Task<AuditSearchResult> SearchAsync(AuditSearchCriteria criteria, CancellationToken cancellationToken = default);
    Task<string[]> GetActingUsersAsync(CancellationToken cancellationToken = default);
}
