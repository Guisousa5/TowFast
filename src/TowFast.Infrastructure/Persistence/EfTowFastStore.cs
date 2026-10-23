using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TowFast.Application.Abstractions;
using TowFast.Domain.ServiceRequests;

namespace TowFast.Infrastructure.Persistence;

public sealed class EfTowFastStore(TowFastDbContext db) : ITowFastStore
{
    public async Task<T?> FindAsync<T>(Guid id, CancellationToken cancellationToken) where T : class
    {
        if (typeof(T) == typeof(ServiceRequest))
        {
            var request = await db.ServiceRequests.Include(x => x.Items).Include(x => x.History)
                .Include(x => x.Assignments).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            return request as T;
        }
        return await db.Set<T>().FindAsync([id], cancellationToken);
    }

    public async Task<T?> FirstAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken) where T : class =>
        await Query<T>().FirstOrDefaultAsync(predicate, cancellationToken);

    public Task<bool> AnyAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken) where T : class =>
        db.Set<T>().AnyAsync(predicate, cancellationToken);

    public async Task<List<T>> ListAsync<T>(Expression<Func<T, bool>>? predicate, CancellationToken cancellationToken) where T : class
    {
        var query = Query<T>();
        return await (predicate is null ? query : query.Where(predicate)).ToListAsync(cancellationToken);
    }

    public async Task AddAsync<T>(T entity, CancellationToken cancellationToken) where T : class =>
        await db.Set<T>().AddAsync(entity, cancellationToken);

    public void Remove<T>(T entity) where T : class => db.Set<T>().Remove(entity);
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public async Task<TResult> InTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
    {
        if (db.Database.ProviderName?.Contains("InMemory", StringComparison.Ordinal) == true)
            return await operation(cancellationToken);
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    private IQueryable<T> Query<T>() where T : class
    {
        if (typeof(T) == typeof(ServiceRequest))
        {
            return (IQueryable<T>)db.ServiceRequests.Include(x => x.Items).Include(x => x.History).Include(x => x.Assignments);
        }
        return db.Set<T>();
    }
}
