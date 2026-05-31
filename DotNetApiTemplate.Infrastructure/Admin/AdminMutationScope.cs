using DotNetApiTemplate.Application.Features.Admin.Interfaces;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DotNetApiTemplate.Infrastructure.Admin;

public sealed class AdminMutationScope : IAdminMutationScope
{
    private readonly ApplicationDbContext _dbContext;

    public AdminMutationScope(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }
}
