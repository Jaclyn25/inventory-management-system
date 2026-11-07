using System.Net;
using System.Net.Http.Json;
using Stockroom.Api.Contracts;
namespace Stockroom.Tests;

[Collection("postgres")]
public class StockOutTests(PostgresFixture app)
{
    private readonly HttpClient client = app.CreateClient();
    [Fact]
    public async Task Issue_preserves_balances_history_and_exact_retries()
    {
        var product = await ProductTests.Create(client);
        (await StockInTests.Send(client, "in", StockInTests.Input(product.Id, 12))).EnsureSuccessStatusCode();
        var input = StockInTests.Input(product.Id, 4) with { Reason = "Customer dispatch" };
        var key = Guid.NewGuid();
        var response = await StockInTests.Send(client, "out", input, key);
        var movement = (await response.Content.ReadFromJsonAsync<MovementView>())!;
        Assert.Equal(-4, movement.Delta);
        Assert.Equal(12, movement.QuantityBefore);
        Assert.Equal(8, movement.QuantityAfter);
        Assert.Equal("out", movement.Kind);
        Assert.Equal(HttpStatusCode.OK, (await StockInTests.Send(client, "out", input, key)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await StockInTests.Send(client, "out", input)).StatusCode);
        Assert.Equal(8, (await client.GetFromJsonAsync<ProductView>($"/api/products/{product.Id}"))!.Quantity);
        var history = await client.GetFromJsonAsync<Page<MovementView>>($"/api/stock/movements?productId={product.Id}&kind=out&search=dispatch");
        Assert.Equal(input.OperationId, Assert.Single(history!.Items).Id);
    }
    [Fact]
    public async Task Concurrent_decreases_cannot_oversell()
    {
        var product = await ProductTests.Create(client);
        (await StockInTests.Send(client, "in", StockInTests.Input(product.Id, 10))).EnsureSuccessStatusCode();
        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => StockInTests.Send(client, "out", StockInTests.Input(product.Id, 2))));
        Assert.Equal(5, responses.Count(x => x.StatusCode == HttpStatusCode.OK));
        Assert.Equal(15, responses.Count(x => x.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(0, (await client.GetFromJsonAsync<ProductView>($"/api/products/{product.Id}"))!.Quantity);
        var history = await client.GetFromJsonAsync<Page<MovementView>>($"/api/stock/movements?productId={product.Id}");
        Assert.Equal(6, history!.Total);
        Assert.Equal(0, history.Items.Sum(x => x.Delta));
    }
    [Fact]
    public async Task Empty_stock_and_negative_issue_quantities_are_rejected()
    {
        var product = await ProductTests.Create(client);
        Assert.Equal(HttpStatusCode.Conflict, (await StockInTests.Send(client, "out", StockInTests.Input(product.Id, 1))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await StockInTests.Send(client, "out", StockInTests.Input(product.Id, -1))).StatusCode);
        var history = await client.GetFromJsonAsync<Page<MovementView>>($"/api/stock/movements?productId={product.Id}");
        Assert.Empty(history!.Items);
    }
}
