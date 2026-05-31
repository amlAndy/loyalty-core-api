namespace DotNetApiTemplate.Application.Features.Admin.Interfaces;

public interface IAdminMutationScope
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
