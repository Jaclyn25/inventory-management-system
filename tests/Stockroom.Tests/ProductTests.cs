using System.Net;
using System.Net.Http.Json;
using Stockroom.Api.Contracts;
using Stockroom.Api.Data;
namespace Stockroom.Tests;

[Collection("postgres")]
public class ProductTests(PostgresFixture app)
{
    private readonly HttpClient client = app.CreateClient();
    public static async Task<Guid> Category(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/categories", new { name = "QA " + Guid.NewGuid() });
        return (await response.Content.ReadFromJsonAsync<Stockroom.Api.Data.Category>())!.Id;
    }
    public static ProductInput Input(Guid category) => new("QA-" + Guid.NewGuid().ToString("N"), "QA product", "Fictional test item", category, 29.95m, 12.45m, 5);
    public static async Task<ProductView> Create(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/products", Input(await Category(client)));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductView>())!;
    }
    [Fact]
    public async Task Product_lifecycle_and_category_integrity()
    {
        var input = Input(await Category(client));
        var response = await client.PostAsJsonAsync("/api/products", input);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = (await response.Content.ReadFromJsonAsync<ProductView>())!;
        Assert.Equal(0, product.Quantity);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/categories/{input.CategoryId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/products/{product.Id}", input with { Name = "Updated" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/products/{product.Id}", input)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync($"/api/products/{product.Id}/archive", new { version = 2 })).StatusCode);
        Assert.True((await client.GetFromJsonAsync<ProductView>($"/api/products/{product.Id}"))!.IsArchived);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/products/{product.Id}", input with { Version = 3 })).StatusCode);
    }
    [Fact]
    public async Task Duplicate_sku_is_case_insensitive()
    {
        var input = Input(await Category(client));
        (await client.PostAsJsonAsync("/api/products", input)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/products", input with { Sku = input.Sku.ToLowerInvariant() })).StatusCode);
    }
    [Theory]
    [InlineData("name")][InlineData("sku")][InlineData("price")][InlineData("precision")][InlineData("threshold")][InlineData("category")]
    public async Task Invalid_products_are_rejected(string field)
    {
        var input = Input(await Category(client));
        input = field switch
        {
            "name" => input with { Name = " " }, "sku" => input with { Sku = "bad sku" },
            "price" => input with { UnitPrice = -1 }, "precision" => input with { CostPrice = 1.001m },
            "threshold" => input with { LowStockThreshold = -1 }, _ => input with { CategoryId = Guid.NewGuid() }
        };
        var response = await client.PostAsJsonAsync("/api/products", input);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType!.ToString());
    }
    [Fact]
    public async Task Search_filter_sort_and_pagination()
    {
        var category = await Category(client);
        var input = Input(category);
        (await client.PostAsJsonAsync("/api/products", input with { Name = "Zebra" })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/products", input with { Name = "Amber", Sku = input.Sku + "A" })).EnsureSuccessStatusCode();
        var result = await client.GetFromJsonAsync<Page<ProductView>>($"/api/products?categoryId={category}&status=out&pageSize=1&sort=name");
        Assert.Equal(2, result!.Total);
        Assert.Equal("Amber", Assert.Single(result.Items).Name);
        result = await client.GetFromJsonAsync<Page<ProductView>>($"/api/products?categoryId={category}&search=zEbRa");
        Assert.Equal("Zebra", Assert.Single(result!.Items).Name);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/products?page=0&sort=invalid")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/products/{Guid.NewGuid()}")).StatusCode);
    }
}
