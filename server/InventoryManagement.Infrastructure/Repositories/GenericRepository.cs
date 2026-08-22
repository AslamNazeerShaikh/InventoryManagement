using System.Linq.Expressions;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

/// <summary>
/// Base EF Core repository. Reads are tracking-free by default and compose filtering, includes,
/// ordering and paging directly in SQL to minimize materialized memory and database round-trips.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
public abstract class GenericRepository<T> : IGenericRepository<T>
    where T : class
{
    /// <summary>The shared database context for the current unit of work.</summary>
    protected AppDbContext DbContext { get; }

    /// <summary>Convenience accessor for the entity set.</summary>
    protected DbSet<T> EntitySet => DbContext.Set<T>();

    /// <summary>Creates the repository over the supplied context.</summary>
    protected GenericRepository(AppDbContext dbContext)
    {
        DbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await EntitySet.FindAsync(new object?[] { id }, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default
    ) =>
        await BuildQuery(predicate, include, asNoTracking)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> ListAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        int? take = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default
    ) =>
        await BuildQuery(predicate, include, asNoTracking, orderBy, take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default
    ) => await EntitySet.AnyAsync(predicate, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default
    ) =>
        predicate is null
            ? await EntitySet.CountAsync(cancellationToken).ConfigureAwait(false)
            : await EntitySet.CountAsync(predicate, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await EntitySet.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        return entity;
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default
    ) => await EntitySet.AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void Update(T entity) => EntitySet.Update(entity);

    /// <inheritdoc />
    public void Remove(T entity) => EntitySet.Remove(entity);

    /// <inheritdoc />
    public void RemoveRange(IEnumerable<T> entities) => EntitySet.RemoveRange(entities);

    /// <inheritdoc />
    public async Task<PagedList<T>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(orderBy);
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }
        if (pageSize < 1)
        {
            pageSize = 1;
        }

        var query = BuildQuery(predicate, include, asNoTracking);
        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await orderBy(query)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedList<T>(items, totalCount);
    }

    /// <summary>Composes a query applying tracking, includes, filter, ordering and limit in order.</summary>
    private IQueryable<T> BuildQuery(
        Expression<Func<T, bool>>? predicate,
        Func<IQueryable<T>, IQueryable<T>>? include,
        bool asNoTracking,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        int? take = null
    )
    {
        IQueryable<T> query = EntitySet;

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (include is not null)
        {
            query = include(query);
        }

        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        if (orderBy is not null)
        {
            query = orderBy(query);
        }

        if (take is > 0)
        {
            query = query.Take(take.Value);
        }

        return query;
    }
}
