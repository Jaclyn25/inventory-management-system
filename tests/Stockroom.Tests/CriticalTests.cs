using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Stockroom.Api.Contracts;
using Stockroom.Api.Data;
namespace Stockroom.Tests;

[Collection("postgres")]
public class CriticalTests(PostgresFixture app)
{
    private readonly HttpClient client = app.CreateClient();
    [Fact]
    public async Task Catalog_edits_revalue_current_stock_and_preserve_historical_details()
    {
        var product = await ProductTests.Create(client);
        (await StockInTests.Send(client, "in", StockInTests.Input(product.Id, 3))).EnsureSuccessStatusCode();
        var edit = new ProductInput(product.Sku + "-X", "Renamed product", product.Description, product.CategoryId, 30m, 20m, 5, 2);
        var updated = await client.PutAsJsonAsync($"/api/products/{product.Id}", edit);
        updated.EnsureSuccessStatusCode();
        Assert.Equal(60m, (await updated.Content.ReadFromJsonAsync<ProductView>())!.InventoryValue);
        var history = (await client.GetFromJsonAsync<Page<MovementView>>($"/api/stock/movements?productId={product.Id}"))!;
        Assert.Equal(product.Sku, Assert.Single(history.Items).Sku);
        Assert.Equal(product.CostPrice, history.Items[0].CostPrice);
        (await StockInTests.Send(client, "out", StockInTests.Input(product.Id, 3))).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/products/{product.Id}/archive", new { version = 4 })).EnsureSuccessStatusCode();
        history = (await client.GetFromJsonAsync<Page<MovementView>>($"/api/stock/movements?productId={product.Id}"))!;
        Assert.Equal(2, history.Total);
        Assert.Equal(0, history.Items.Sum(x => x.Delta));
        Assert.Equal(0, (await client.GetFromJsonAsync<Page<ProductView>>($"/api/products?search={edit.Sku}"))!.Total);
        Assert.Equal(1, (await client.GetFromJsonAsync<Page<ProductView>>($"/api/products?search={edit.Sku}&archived=true"))!.Total);
    }
    [Fact]
    public async Task Duplicate_business_operation_across_products_rolls_back_loser()
    {
        var first = await ProductTests.Create(client);
        var second = await ProductTests.Create(client);
        var operation = Guid.NewGuid();
        var responses = await Task.WhenAll(StockInTests.Send(client, "in", StockInTests.Input(first.Id, 5) with { OperationId = operation }), StockInTests.Send(client, "in", StockInTests.Input(second.Id, 5) with { OperationId = operation }));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
        var a = (await client.GetFromJsonAsync<ProductView>($"/api/products/{first.Id}"))!;
        var b = (await client.GetFromJsonAsync<ProductView>($"/api/products/{second.Id}"))!;
        Assert.Equal(5, a.Quantity + b.Quantity);
    }
    [Fact]
    public async Task Database_constraints_reject_negative_stock()
    {
        var product = await ProductTests.Create(client);
        await using var db = new InventoryDb(new DbContextOptionsBuilder<InventoryDb>().UseNpgsql(app.Connection).Options);
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Products\" SET \"Quantity\" = -1 WHERE \"Id\" = {product.Id}"));
        Assert.Equal(0, (await client.GetFromJsonAsync<ProductView>($"/api/products/{product.Id}"))!.Quantity);
    }
    [Fact]
    public async Task Unknown_routes_malformed_json_and_wrong_methods_return_problems()
    {
        var response = await client.GetAsync("/api/does-not-exist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        response = await client.PostAsync("/api/products", new StringContent("{bad", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        response = await client.DeleteAsync("/api/reports");
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}
