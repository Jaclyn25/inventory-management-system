using Microsoft.AspNetCore.Mvc.Testing;

namespace Stockroom.Tests;

public class FoundationTests
{
    [Fact]
    public async Task Health_and_openapi_are_available()
    {
        await using var app = new WebApplicationFactory<Program>();
        var client = app.CreateClient();
        Assert.True((await client.GetAsync("/health")).IsSuccessStatusCode);
        Assert.Contains("openapi", await client.GetStringAsync("/openapi/v1.json"));
    }
}
