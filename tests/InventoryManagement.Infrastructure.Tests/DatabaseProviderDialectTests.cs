using InventoryManagement.Infrastructure.Data.Providers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Tests;

/// <summary>
/// Unit tests for the provider seam used by the duplicate-detection fix: unique-violation
/// recognition and filtered-index SQL. Each dialect is exercised without that provider being
/// installed, which is the point of keeping the provider-specific knowledge in one place.
/// </summary>
public class DatabaseProviderDialectTests
{
    /// <summary>Builds a <see cref="DbUpdateException"/> around a provider error, as EF Core does.</summary>
    private static DbUpdateException UpdateException(Exception providerException) =>
        new("An error occurred while saving the entity changes.", providerException);

    [Theory]
    [InlineData(DatabaseProviderDialects.SqliteProvider)]
    [InlineData(DatabaseProviderDialects.SqlServerProvider)]
    [InlineData(DatabaseProviderDialects.PostgreSqlProvider)]
    public void For_KnownProvider_ReturnsMatchingDialect(string providerName) =>
        Assert.Equal(providerName, DatabaseProviderDialects.For(providerName).ProviderName);

    [Fact]
    public void For_UnknownProvider_ReturnsConservativeDialect()
    {
        var dialect = DatabaseProviderDialects.For("Some.Other.Provider");

        Assert.Null(dialect.BuildUniqueWhenPresentFilter("Barcode", "IsDeleted"));
        Assert.False(dialect.IsUniqueConstraintViolation(UpdateException(new Exception("boom"))));
    }

    [Theory]
    [InlineData(2067)] // SQLITE_CONSTRAINT_UNIQUE
    [InlineData(1555)] // SQLITE_CONSTRAINT_PRIMARYKEY
    public void Sqlite_RecognizesUniqueViolationExtendedCodes(int extendedErrorCode)
    {
        var exception = UpdateException(
            new SqliteException("UNIQUE constraint failed", 19, extendedErrorCode)
        );

        Assert.True(DatabaseProviderDialects.Sqlite.IsUniqueConstraintViolation(exception));
    }

    [Theory]
    [InlineData(787)] // SQLITE_CONSTRAINT_FOREIGNKEY
    [InlineData(1299)] // SQLITE_CONSTRAINT_NOTNULL
    public void Sqlite_IgnoresOtherConstraintViolations(int extendedErrorCode)
    {
        var exception = UpdateException(
            new SqliteException("constraint failed", 19, extendedErrorCode)
        );

        Assert.False(DatabaseProviderDialects.Sqlite.IsUniqueConstraintViolation(exception));
    }

    [Fact]
    public void Sqlite_BuildsPartialIndexFilter()
    {
        var filter = DatabaseProviderDialects.Sqlite.BuildUniqueWhenPresentFilter(
            "Barcode",
            "IsDeleted"
        );

        Assert.Equal("\"Barcode\" IS NOT NULL AND \"Barcode\" <> '' AND \"IsDeleted\" = 0", filter);
    }

    [Fact]
    public void SqlServer_BuildsFilteredIndexFilter() =>
        Assert.Equal(
            "[Barcode] IS NOT NULL AND [Barcode] <> '' AND [IsDeleted] = 0",
            DatabaseProviderDialects.SqlServer.BuildUniqueWhenPresentFilter("Barcode", "IsDeleted")
        );

    [Fact]
    public void PostgreSql_BuildsPartialIndexFilter() =>
        Assert.Equal(
            "\"Barcode\" IS NOT NULL AND \"Barcode\" <> '' AND \"IsDeleted\" = false",
            DatabaseProviderDialects.PostgreSql.BuildUniqueWhenPresentFilter("Barcode", "IsDeleted")
        );

    [Theory]
    [InlineData(2627, true)] // unique constraint
    [InlineData(2601, true)] // unique index
    [InlineData(547, false)] // foreign-key violation
    public void SqlServer_RecognizesUniqueViolationByErrorNumber(int number, bool expected) =>
        Assert.Equal(
            expected,
            DatabaseProviderDialects.SqlServer.IsUniqueConstraintViolation(
                UpdateException(new FakeSqlServerException(number))
            )
        );

    [Theory]
    [InlineData("23505", true)] // unique_violation
    [InlineData("23503", false)] // foreign_key_violation
    public void PostgreSql_RecognizesUniqueViolationBySqlState(string sqlState, bool expected) =>
        Assert.Equal(
            expected,
            DatabaseProviderDialects.PostgreSql.IsUniqueConstraintViolation(
                UpdateException(new FakePostgresException(sqlState))
            )
        );

    /// <summary>
    /// The startup guard must report exactly the providers that can express the filtered index, so an
    /// unrecognized provider is never mistaken for one that enforces uniqueness.
    /// </summary>
    [Theory]
    [InlineData(DatabaseProviderDialects.SqliteProvider, true)]
    [InlineData(DatabaseProviderDialects.SqlServerProvider, true)]
    [InlineData(DatabaseProviderDialects.PostgreSqlProvider, true)]
    [InlineData("Pomelo.EntityFrameworkCore.MySql", false)]
    [InlineData("Microsoft.EntityFrameworkCore.InMemory", false)]
    [InlineData(null, false)]
    public void CanEnforceUniqueWhenPresent_MatchesDialectAvailability(
        string? providerName,
        bool expected
    ) => Assert.Equal(expected, DatabaseProviderDialects.CanEnforceUniqueWhenPresent(providerName));

    /// <summary>Stands in for <c>Microsoft.Data.SqlClient.SqlException</c> (its <c>Number</c> property).</summary>
    private sealed class FakeSqlServerException : Exception
    {
        public FakeSqlServerException(int number)
            : base($"SQL Server error {number}") => Number = number;

        public int Number { get; }
    }

    /// <summary>Stands in for <c>Npgsql.PostgresException</c> (a <c>DbException</c> carrying a SQLSTATE).</summary>
    private sealed class FakePostgresException : System.Data.Common.DbException
    {
        public FakePostgresException(string sqlState)
            : base($"PostgreSQL error {sqlState}") => SqlState = sqlState;

        public override string? SqlState { get; }
    }
}
