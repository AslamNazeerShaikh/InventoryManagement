using System.Data.Common;
using System.Reflection;
using InventoryManagement.Domain.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Data.Providers;

/// <summary>
/// Resolves the <see cref="IDatabaseProviderDialect"/> for an EF Core provider name. Kept as a pure
/// function of the provider name so both model building (<see cref="AppDbContext.OnModelCreating"/>)
/// and runtime error translation resolve exactly the same dialect without extra DI wiring, and so
/// every dialect can be exercised in unit tests without that provider being installed.
/// </summary>
public static class DatabaseProviderDialects
{
    /// <summary>EF Core provider name for Microsoft.Data.Sqlite.</summary>
    public const string SqliteProvider = "Microsoft.EntityFrameworkCore.Sqlite";

    /// <summary>EF Core provider name for Microsoft SQL Server.</summary>
    public const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";

    /// <summary>EF Core provider name for PostgreSQL (Npgsql).</summary>
    public const string PostgreSqlProvider = "Npgsql.EntityFrameworkCore.PostgreSQL";

    /// <summary>
    /// Returns the dialect for <paramref name="providerName"/>, or a conservative unknown-provider
    /// dialect that neither claims unique violations nor emits filtered-index SQL.
    /// </summary>
    /// <param name="providerName">Value of <c>DatabaseFacade.ProviderName</c>.</param>
    public static IDatabaseProviderDialect For(string? providerName) =>
        providerName switch
        {
            SqliteProvider => Sqlite,
            SqlServerProvider => SqlServer,
            PostgreSqlProvider => PostgreSql,
            _ => Unknown,
        };

    /// <summary>Dialect for SQLite.</summary>
    public static IDatabaseProviderDialect Sqlite { get; } = new SqliteDialect();

    /// <summary>Dialect for SQL Server.</summary>
    public static IDatabaseProviderDialect SqlServer { get; } = new SqlServerDialect();

    /// <summary>Dialect for PostgreSQL.</summary>
    public static IDatabaseProviderDialect PostgreSql { get; } = new PostgreSqlDialect();

    /// <summary>Fallback dialect for providers this application has no specific knowledge of.</summary>
    public static IDatabaseProviderDialect Unknown { get; } = new UnknownDialect();

    /// <summary>
    /// Whether <paramref name="providerName"/> resolves to a dialect that can express the filtered
    /// "unique when present" index predicate. When this is <c>false</c> the integrity constraint is
    /// silently absent, so callers must surface that loudly at startup rather than let the
    /// application run believing duplicates are impossible.
    /// </summary>
    /// <param name="providerName">Value of <c>DatabaseFacade.ProviderName</c>.</param>
    public static bool CanEnforceUniqueWhenPresent(string? providerName) =>
        For(providerName).BuildUniqueWhenPresentFilter("Probe", nameof(BaseEntity.IsDeleted))
            is not null;

    /// <summary>Enumerates an exception and its inner exceptions, outermost first.</summary>
    private static IEnumerable<Exception> Chain(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            yield return current;
        }
    }

    /// <summary>
    /// Reads an <c>int</c> property (e.g. <c>SqlException.Number</c>) reflectively so a provider's
    /// error codes can be recognized without taking a compile-time dependency on its ADO.NET client.
    /// </summary>
    private static bool HasErrorNumber(Exception exception, params int[] numbers)
    {
        var property = exception
            .GetType()
            .GetProperty("Number", BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(exception) is int number && numbers.Contains(number);
    }

    /// <summary>
    /// SQLite: extended result codes 2067 (<c>SQLITE_CONSTRAINT_UNIQUE</c>) and 1555
    /// (<c>SQLITE_CONSTRAINT_PRIMARYKEY</c>) identify a uniqueness violation. Partial indexes are
    /// supported, and booleans are stored as <c>INTEGER</c>.
    /// </summary>
    private sealed class SqliteDialect : IDatabaseProviderDialect
    {
        private const int ConstraintUnique = 2067;
        private const int ConstraintPrimaryKey = 1555;

        /// <inheritdoc />
        public string ProviderName => SqliteProvider;

        /// <inheritdoc />
        public bool IsUniqueConstraintViolation(DbUpdateException exception) =>
            exception is not null
            && Chain(exception)
                .OfType<SqliteException>()
                .Any(e => e.SqliteExtendedErrorCode is ConstraintUnique or ConstraintPrimaryKey);

        /// <inheritdoc />
        public string? BuildUniqueWhenPresentFilter(string valueColumn, string softDeleteColumn) =>
            $"\"{valueColumn}\" IS NOT NULL AND \"{valueColumn}\" <> '' AND \"{softDeleteColumn}\" = 0";
    }

    /// <summary>
    /// SQL Server: errors 2627 (unique constraint) and 2601 (unique index). Filtered indexes are
    /// supported, identifiers are bracket-quoted and <c>bit</c> compares against <c>0</c>.
    /// </summary>
    private sealed class SqlServerDialect : IDatabaseProviderDialect
    {
        private const int UniqueConstraint = 2627;
        private const int UniqueIndex = 2601;

        /// <inheritdoc />
        public string ProviderName => SqlServerProvider;

        /// <inheritdoc />
        public bool IsUniqueConstraintViolation(DbUpdateException exception) =>
            exception is not null
            && Chain(exception).Any(e => HasErrorNumber(e, UniqueConstraint, UniqueIndex));

        /// <inheritdoc />
        public string? BuildUniqueWhenPresentFilter(string valueColumn, string softDeleteColumn) =>
            $"[{valueColumn}] IS NOT NULL AND [{valueColumn}] <> '' AND [{softDeleteColumn}] = 0";
    }

    /// <summary>
    /// PostgreSQL: SQLSTATE <c>23505</c> (<c>unique_violation</c>), surfaced through
    /// <see cref="DbException.SqlState"/>. Partial indexes are supported and <c>boolean</c> compares
    /// against <c>false</c>.
    /// </summary>
    private sealed class PostgreSqlDialect : IDatabaseProviderDialect
    {
        private const string UniqueViolation = "23505";

        /// <inheritdoc />
        public string ProviderName => PostgreSqlProvider;

        /// <inheritdoc />
        public bool IsUniqueConstraintViolation(DbUpdateException exception) =>
            exception is not null
            && Chain(exception)
                .OfType<DbException>()
                .Any(e => string.Equals(e.SqlState, UniqueViolation, StringComparison.Ordinal));

        /// <inheritdoc />
        public string? BuildUniqueWhenPresentFilter(string valueColumn, string softDeleteColumn) =>
            $"\"{valueColumn}\" IS NOT NULL AND \"{valueColumn}\" <> '' AND \"{softDeleteColumn}\" = false";
    }

    /// <summary>
    /// Fallback for an unrecognized provider: never claims a failure is a uniqueness violation (so
    /// unrelated faults keep their original semantics) and emits no filtered-index SQL, leaving the
    /// model exactly as portable as it was before.
    /// </summary>
    private sealed class UnknownDialect : IDatabaseProviderDialect
    {
        /// <inheritdoc />
        public string ProviderName => "Unknown";

        /// <inheritdoc />
        public bool IsUniqueConstraintViolation(DbUpdateException exception) => false;

        /// <inheritdoc />
        public string? BuildUniqueWhenPresentFilter(string valueColumn, string softDeleteColumn) =>
            null;
    }
}
