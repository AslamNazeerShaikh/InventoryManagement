using InventoryManagement.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Tests;

/// <summary>
/// Creates an <see cref="AppDbContext"/> backed by a private in-memory SQLite database. The
/// connection is kept open for the fixture lifetime so the schema persists, and additional contexts
/// can be created on the same connection to exercise cross-context concurrency behaviour.
/// </summary>
public sealed class SqliteInMemoryFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    /// <summary>Opens the shared in-memory connection and creates the schema.</summary>
    public SqliteInMemoryFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

        Context = new AppDbContext(_options);
        Context.Database.EnsureCreated();
    }

    /// <summary>The primary context.</summary>
    public AppDbContext Context { get; }

    /// <summary>Creates an additional context over the same in-memory database.</summary>
    public AppDbContext NewContext() => new(_options);

    /// <inheritdoc />
    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
