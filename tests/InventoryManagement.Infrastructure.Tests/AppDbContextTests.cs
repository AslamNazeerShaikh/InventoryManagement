using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Infrastructure.Tests;

public class AppDbContextTests
{
    [Fact]
    public async Task SaveChanges_SetsCreatedAt_OnAdd()
    {
        using var fixture = new SqliteInMemoryFixture();
        var before = DateTime.UtcNow.AddSeconds(-5);

        var user = new User { Name = "Audit", Email = "audit@test.com" };
        fixture.Context.Users.Add(user);
        await fixture.Context.SaveChangesAsync();

        Assert.InRange(user.CreatedAt, before, DateTime.UtcNow.AddSeconds(5));
        Assert.Null(user.UpdatedAt);
    }

    [Fact]
    public async Task SaveChanges_SetsUpdatedAt_OnModify()
    {
        using var fixture = new SqliteInMemoryFixture();

        var user = new User { Name = "Audit", Email = "audit@test.com" };
        fixture.Context.Users.Add(user);
        await fixture.Context.SaveChangesAsync();

        user.Name = "Audit-Updated";
        await fixture.Context.SaveChangesAsync();

        Assert.NotNull(user.UpdatedAt);
    }
}
