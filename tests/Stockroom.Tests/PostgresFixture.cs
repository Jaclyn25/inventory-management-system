using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Stockroom.Api.Data;
namespace Stockroom.Tests;

public sealed class PostgresFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string database = "stockroom_test_" + Guid.NewGuid().ToString("N");
    private readonly string admin = Environment.GetEnvironmentVariable("INVENTORY_TEST_CONNECTION")
        ?? throw new InvalidOperationException("Set INVENTORY_TEST_CONNECTION to real PostgreSQL with CREATEDB permission.");
    public string Connection => new NpgsqlConnectionStringBuilder(admin) { Database = database }.ConnectionString;
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureServices(services =>
    {
        services.RemoveAll<DbContextOptions<InventoryDb>>();
        services.AddDbContext<InventoryDb>(o => o.UseNpgsql(Connection));
    });
    public async Task InitializeAsync()
    {
        await using var connection = new NpgsqlConnection(admin);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE {database}", connection);
        await command.ExecuteNonQueryAsync();
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<InventoryDb>().Database.MigrateAsync();
    }
    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(admin);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE {database} WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
    }
}
[CollectionDefinition("postgres")]
public class PostgresCollection : ICollectionFixture<PostgresFixture>;
