namespace InventoryManagement.Domain.Common;

/// <summary>
/// Lightweight container returned by paged repository queries: the materialized page of entities
/// plus the total row count, both computed against the database in a single logical operation.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <param name="Items">The current page of results (already ordered and limited in SQL).</param>
/// <param name="TotalCount">Total number of rows matching the query, ignoring paging.</param>
public readonly record struct PagedList<T>(IReadOnlyList<T> Items, int TotalCount);
