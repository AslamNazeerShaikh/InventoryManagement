using System.Linq.Expressions;
using InventoryManagement.Domain.Common;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>
/// Generic, read-optimized repository abstraction over an aggregate. Reads are tracking-free by
/// default and compose ordering, filtering, projection-friendly includes and paging in SQL to
/// minimize memory and round-trips. All asynchronous operations honour a <see cref="CancellationToken"/>.
/// </summary>
/// <typeparam name="T">Entity type managed by the repository.</typeparam>
public interface IGenericRepository<T>
    where T : class
{
    /// <summary>Finds a tracked entity by primary key, or <c>null</c> when absent.</summary>
    /// <remarks>Tracked because callers typically mutate the returned entity and persist it.</remarks>
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Returns the first entity matching <paramref name="predicate"/>, or <c>null</c>.</summary>
    /// <param name="predicate">Filter expression translated to SQL.</param>
    /// <param name="include">Optional include composition (e.g. <c>q =&gt; q.Include(x =&gt; x.Nav)</c>).</param>
    /// <param name="asNoTracking">When <c>true</c> (default) the query does not track results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default
    );

    /// <summary>Materializes a list with optional filter, includes, ordering and a SQL-side limit.</summary>
    /// <param name="predicate">Optional filter expression translated to SQL.</param>
    /// <param name="include">Optional include composition.</param>
    /// <param name="orderBy">Optional ordering composition applied in SQL.</param>
    /// <param name="take">Optional maximum number of rows (applied after ordering, in SQL).</param>
    /// <param name="asNoTracking">When <c>true</c> (default) the query does not track results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<T>> ListAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        int? take = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default
    );

    /// <summary>Returns whether any row matches <paramref name="predicate"/>.</summary>
    Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default
    );

    /// <summary>Counts rows, optionally filtered by <paramref name="predicate"/>.</summary>
    Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>Stages a new entity for insertion (persisted on the next unit-of-work save).</summary>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>Stages multiple new entities for insertion.</summary>
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>Marks an entity as modified.</summary>
    void Update(T entity);

    /// <summary>Marks an entity for deletion (hard delete; prefer soft-delete flags where applicable).</summary>
    void Remove(T entity);

    /// <summary>Marks multiple entities for deletion.</summary>
    void RemoveRange(IEnumerable<T> entities);

    /// <summary>
    /// Returns a deterministic, ordered page plus the total matching count, computing both in SQL.
    /// Ordering is mandatory to guarantee stable, non-overlapping pages.
    /// </summary>
    /// <param name="pageNumber">1-based page index.</param>
    /// <param name="pageSize">Maximum items per page.</param>
    /// <param name="orderBy">Mandatory ordering composition applied in SQL.</param>
    /// <param name="predicate">Optional filter expression.</param>
    /// <param name="include">Optional include composition.</param>
    /// <param name="asNoTracking">When <c>true</c> (default) the query does not track results.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PagedList<T>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default
    );
}
