using Microsoft.AspNetCore.Mvc;
using Stockroom.Api.Contracts;
using Stockroom.Api.Services;
namespace Stockroom.Api.Controllers;

[ApiController, Route("api/stock")]
public sealed class StockController(StockService service) : ControllerBase
{
    private Guid Key(string? value) => Guid.TryParse(value, out var key) && key != Guid.Empty ? key : throw new ApiException(400, "A nonempty UUID Idempotency-Key header is required.");
    [HttpPost("in")] public async Task<IActionResult> Receive(StockInput input, [FromHeader(Name = "Idempotency-Key")] string? key) => Ok(await service.Receive(input, Key(key)));
    [HttpPost("out")] public async Task<IActionResult> Issue(StockInput input, [FromHeader(Name = "Idempotency-Key")] string? key) => Ok(await service.Issue(input, Key(key)));
    [HttpGet("movements")] public async Task<IActionResult> History([FromQuery] MovementQuery query) => Ok(await service.History(query));
}
