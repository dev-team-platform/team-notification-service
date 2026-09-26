using Microsoft.EntityFrameworkCore;
using TeamNotificationService.Application.Interfaces.Repositories;

namespace TeamNotificationService.Infrastructure.Repositories;

public abstract class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly Serilog.ILogger _logger;
    protected readonly DbContext _dbContext;

    protected GenericRepository(Serilog.ILogger logger, DbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public virtual T Add(T entity)
    {
        _dbContext.Set<T>().Add(entity);
        return entity;
    }

    public virtual IReadOnlyList<T> AddRange(IReadOnlyList<T> entities)
    {
        _dbContext.Set<T>().AddRange(entities);
        return entities;
    }

    public virtual async Task<T?> FindFirstByConditionAsync(
        Func<IQueryable<T>, IQueryable<T>> condition,
        bool trackChanges = false,
        CancellationToken cancellationToken = default
   )
    {
        IQueryable<T> query = _dbContext.Set<T>();

        if (!trackChanges)
            query = query.AsNoTracking();

        query = condition(query);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> FindAllAsync(
        bool trackChanges = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = _dbContext.Set<T>();

        if (!trackChanges)
            query = query.AsNoTracking();

        return await query.ToListAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> FindAllByConditionAsync(
        Func<IQueryable<T>, IQueryable<T>> condition,
        bool trackChanges = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = _dbContext.Set<T>();

        if (!trackChanges)
            query = query.AsNoTracking();

        query = condition(query);
        return await query.ToListAsync(cancellationToken);
    }

    public virtual IQueryable<T> QueryByCondition(
        Func<IQueryable<T>, IQueryable<T>> condition,
        bool trackChanges = false)
    {
        IQueryable<T> query = _dbContext.Set<T>();

        if (!trackChanges)
            query = query.AsNoTracking();

        query = condition(query);
        return query;
    }
}