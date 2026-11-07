using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Stockroom.Api.Contracts;
using Stockroom.Api.Data;
namespace Stockroom.Tests;

[Collection("postgres")]
public class ReportTests(PostgresFixture app)
{
    private readonly HttpClient client = app.CreateClient();
    [Fact]
    public async Task Reports_use_exact_cost_valuation_and_stock_health()
    {
        var category = await ProductTests.Category(client);
        var input = ProductTests.Input(category) with { CostPrice = .10m, UnitPrice = .30m, LowStockThreshold = 3 };
        var first = (await (await client.PostAsJsonAsync("/api/products", input)).Content.ReadFromJsonAsync<ProductView>())!;
        (await client.PostAsJsonAsync("/api/products", input with { Sku = input.Sku + "B" })).EnsureSuccessStatusCode();
        (await StockInTests.Send(client, "in", StockInTests.Input(first.Id, 3))).EnsureSuccessStatusCode();
        var report = (await client.GetFromJsonAsync<InventoryReport>($"/api/reports?categoryId={category}"))!;
        Assert.Equal(2, report.TotalProducts);
        Assert.Equal(3, report.TotalUnits);
        Assert.Equal(.30m, report.InventoryValue);
        Assert.Equal(.90m, report.RetailValue);
        Assert.Equal(1, report.LowStockProducts);
        Assert.Equal(1, report.OutOfStockProducts);
        Assert.Equal(.30m, Assert.Single(report.Categories).Value);
        Assert.Equal(2, report.AttentionProducts.Count);
        Assert.Equal(3, Assert.Single(report.RecentMovements).Delta);
        Assert.Equal(14, report.Activity.Count);
        Assert.Equal(3, report.Activity.Sum(x => x.Received));
        Assert.Equal("USD", report.Currency);
        var filtered = (await client.GetFromJsonAsync<InventoryReport>($"/api/reports?categoryId={category}&status=out"))!;
        Assert.Equal(1, filtered.TotalProducts);
        Assert.Equal(0, filtered.InventoryValue);
        Assert.Empty(filtered.RecentMovements);
    }
    [Fact]
    public async Task Reports_keep_totals_consistent_while_stock_changes()
    {
        var category = await ProductTests.Category(client);
        var product = (await (await client.PostAsJsonAsync("/api/products", ProductTests.Input(category) with { CostPrice = .10m })).Content.ReadFromJsonAsync<ProductView>())!;
        var writes = Task.WhenAll(Enumerable.Range(0, 20).Select(_ => StockInTests.Send(client, "in", StockInTests.Input(product.Id, 1))));
        var reads = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => client.GetFromJsonAsync<InventoryReport>($"/api/reports?categoryId={category}")));
        Assert.All(reads, report =>
        {
            Assert.Equal(report!.TotalUnits * .10m, report.InventoryValue);
            Assert.Equal(report.TotalUnits, report.Categories.Sum(x => x.Units));
            Assert.Equal(report.InventoryValue, report.Categories.Sum(x => x.Value));
        });
        Assert.All(await writes, response => response.EnsureSuccessStatusCode());
    }
    [Fact]
    public async Task History_filters_dates_and_rejects_invalid_ranges()
    {
        var product = await ProductTests.Create(client);
        (await StockInTests.Send(client, "in", StockInTests.Input(product.Id))).EnsureSuccessStatusCode();
        var rows = (await client.GetFromJsonAsync<Page<MovementView>>($"/api/stock/movements?productId={product.Id}&from=2000-01-01T00:00:00Z&to=2001-01-01T00:00:00Z"))!;
        Assert.Empty(rows.Items);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/stock/movements?from=2030-01-01&to=2020-01-01")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/stock/movements?kind=bad")).StatusCode);
    }
    [Fact]
    public async Task Fictional_seed_is_repeatable_and_reconciles_every_ledger()
    {
        var database = "stockroom_seed_" + Guid.NewGuid().ToString("N");
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(app.Connection) { Database = database };
        await using var db = new InventoryDb(new DbContextOptionsBuilder<InventoryDb>().UseNpgsql(builder.ConnectionString).Options);
        try
        {
            await db.Database.MigrateAsync();
            await SeedData.Initialize(db);
            await SeedData.Initialize(db);
            Assert.Equal(18, await db.Products.CountAsync());
            Assert.Equal(36, await db.Movements.CountAsync());
            foreach (var product in await db.Products.ToListAsync())
                Assert.Equal(product.Quantity, await db.Movements.Where(x => x.ProductId == product.Id).SumAsync(x => x.Delta));
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
