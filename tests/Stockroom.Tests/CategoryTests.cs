using System.Net;
using System.Net.Http.Json;
using Stockroom.Api.Data;
namespace Stockroom.Tests;

[Collection("postgres")]
public class CategoryTests(PostgresFixture app)
{
    [Fact]
    public async Task Categories_validate_normalize_update_and_delete()
    {
        var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/categories", new { name = " " })).StatusCode);
        var response = await client.PostAsJsonAsync("/api/categories", new { name = "Office" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var category = (await response.Content.ReadFromJsonAsync<Category>())!;
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/categories", new { name = " office " })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/categories/{category.Id}", new { name = "Office supplies" })).StatusCode);
        Assert.Contains("Office supplies", await client.GetStringAsync("/api/categories"));
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/categories/{category.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/categories/{category.Id}")).StatusCode);
    }
}
