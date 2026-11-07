using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Stockroom.Api.Contracts;
using Stockroom.Api.Data;
namespace Stockroom.Tests;

[Collection("postgres")]
public class StockInTests(PostgresFixture app)
{
    private readonly HttpClient client = app.CreateClient();
    public static Task<HttpResponseMessage> Send(HttpClient client, string kind, StockInput input, Guid? key = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/stock/{kind}") { Content = JsonContent.Create(input) };
        request.Headers.Add("Idempotency-Key", (key ?? Guid.NewGuid()).ToString());
        return client.SendAsync(request);
    }
    public static StockInput Input(Guid product, int quantity = 10) => new(Guid.NewGuid(), product, quantity, "Fictional delivery receipt");
    [Fact]
    public async Task Receipt_is_atomic_and_replays_once()
    {
        var product = await ProductTests.Create(client);
        var input = Input(product.Id);
        var key = Guid.NewGuid();
        var first = await Send(client, "in", input, key);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var movement = (await first.Content.ReadFromJsonAsync<MovementView>())!;
        Assert.Equal(0, movement.QuantityBefore);
        Assert.Equal(10, movement.QuantityAfter);
        Assert.Equal(DateTimeKind.Utc, movement.CreatedAt.Kind);
        var replay = await Send(client, "in", input, key);
        Assert.Equal(movement, await replay.Content.ReadFromJsonAsync<MovementView>());
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, "in", input with { Quantity = 2 }, key)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, "in", input)).StatusCode);
        Assert.Equal(10, (await client.GetFromJsonAsync<ProductView>($"/api/products/{product.Id}"))!.Quantity);
        var history = await client.GetFromJsonAsync<Page<MovementView>>($"/api/stock/movements?productId={product.Id}");
        Assert.Equal(1, history!.Total);
    }
    [Fact]
    public async Task Concurrent_receipts_have_no_lost_updates()
    {
        var product = await ProductTests.Create(client);
        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Send(client, "in", Input(product.Id, 1))));
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        Assert.Equal(20, (await client.GetFromJsonAsync<ProductView>($"/api/products/{product.Id}"))!.Quantity);
        var history = await client.GetFromJsonAsync<Page<MovementView>>($"/api/stock/movements?productId={product.Id}&pageSize=100");
        Assert.Equal(Enumerable.Range(1, 20), history!.Items.Select(x => x.QuantityAfter).Order());
    }
    [Fact]
    public async Task Concurrent_retries_create_one_movement()
    {
        var product = await ProductTests.Create(client);
        var input = Input(product.Id);
        var key = Guid.NewGuid();
        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Send(client, "in", input, key)));
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        Assert.Equal(10, (await client.GetFromJsonAsync<ProductView>($"/api/products/{product.Id}"))!.Quantity);
    }
    [Theory]
    [InlineData(0)][InlineData(-1)][InlineData(1000000001)]
    public async Task Invalid_quantities_are_rejected(int quantity)
    {
        var product = await ProductTests.Create(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, "in", Input(product.Id, quantity))).StatusCode);
    }
    [Fact]
    public async Task Missing_key_archived_product_and_overflow_are_rejected()
    {
        var product = await ProductTests.Create(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/stock/in", Input(product.Id))).StatusCode);
        (await Send(client, "in", Input(product.Id, 1000000000))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, "in", Input(product.Id, 1))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/products/{product.Id}/archive", new { version = 2 })).StatusCode);
        var archived = await ProductTests.Create(client);
        (await client.PostAsJsonAsync($"/api/products/{archived.Id}/archive", new { version = 1 })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await Send(client, "in", Input(archived.Id))).StatusCode);
    }
    [Fact]
    public async Task Database_rejects_history_update_and_delete()
    {
        var product = await ProductTests.Create(client);
        var input = Input(product.Id);
        (await Send(client, "in", input)).EnsureSuccessStatusCode();
        await using var db = new InventoryDb(new DbContextOptionsBuilder<InventoryDb>().UseNpgsql(app.Connection).Options);
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"StockMovements\" SET \"Reason\" = 'Tampered' WHERE \"Id\" = {input.OperationId}"));
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"StockMovements\" WHERE \"Id\" = {input.OperationId}"));
    }
}
