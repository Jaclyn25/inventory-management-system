using System.Net;
using System.Net.Http.Json;
using Stockroom.Api.Contracts;
namespace Stockroom.Tests;

[Collection("postgres")]
public class AdjustmentTests(PostgresFixture app)
{
    private readonly HttpClient client = app.CreateClient();
    private Task<HttpResponseMessage> Send(AdjustmentInput input, Guid? key = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/stock/adjustment") { Content = JsonContent.Create(input) };
        request.Headers.Add("Idempotency-Key", (key ?? Guid.NewGuid()).ToString());
        return client.SendAsync(request);
    }
    [Fact]
    public async Task Adjustment_records_count_difference_reason_and_replays()
    {
        var product = await ProductTests.Create(client);
        (await StockInTests.Send(client, "in", StockInTests.Input(product.Id, 10))).EnsureSuccessStatusCode();
        var input = new AdjustmentInput(Guid.NewGuid(), product.Id, 6, "Cycle count discrepancy", 2);
        var key = Guid.NewGuid();
        var response = await Send(input, key);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var movement = (await response.Content.ReadFromJsonAsync<MovementView>())!;
        Assert.Equal(-4, movement.Delta);
        Assert.Equal(6, movement.QuantityAfter);
        Assert.Equal("adjustment", movement.Kind);
        Assert.Equal(input.Reason, movement.Reason);
        Assert.Equal(movement, await (await Send(input, key)).Content.ReadFromJsonAsync<MovementView>());
        Assert.Equal(HttpStatusCode.Conflict, (await Send(input with { OperationId = Guid.NewGuid(), Quantity = 8 })).StatusCode);
        (await Send(input with { OperationId = Guid.NewGuid(), Quantity = 0, Version = 3 })).EnsureSuccessStatusCode();
        Assert.Equal(0, (await client.GetFromJsonAsync<ProductView>($"/api/products/{product.Id}"))!.Quantity);
    }
    [Fact]
    public async Task Concurrent_adjustments_reject_stale_counts()
    {
        var product = await ProductTests.Create(client);
        var responses = await Task.WhenAll(Send(new(Guid.NewGuid(), product.Id, 3, "First cycle count", 1)), Send(new(Guid.NewGuid(), product.Id, 7, "Second cycle count", 1)));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
        var history = await client.GetFromJsonAsync<Page<MovementView>>($"/api/stock/movements?productId={product.Id}");
        Assert.Single(history!.Items);
    }
    [Theory]
    [InlineData(-1, "Valid reason")][InlineData(1, " ")][InlineData(0, "Count matches")]
    public async Task Invalid_and_noop_adjustments_are_rejected(int quantity, string reason)
    {
        var product = await ProductTests.Create(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(new(Guid.NewGuid(), product.Id, quantity, reason, 1))).StatusCode);
        Assert.Equal(0, (await client.GetFromJsonAsync<ProductView>($"/api/products/{product.Id}"))!.Quantity);
    }
}
