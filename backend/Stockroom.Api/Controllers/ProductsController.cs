using Microsoft.AspNetCore.Mvc;
using Stockroom.Api.Contracts;
using Stockroom.Api.Services;
namespace Stockroom.Api.Controllers;

[ApiController, Route("api/products")]
public sealed class ProductsController(ProductService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] ProductQuery query) => Ok(await service.List(query));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id) => Ok(await service.Get(id));
    [HttpPost] public async Task<IActionResult> Create(ProductInput input)
    {
        var product = await service.Save(input);
        return Created($"/api/products/{product.Id}", product);
    }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, ProductInput input) => Ok(await service.Save(input, id));
    [HttpPost("{id:guid}/archive")] public async Task<IActionResult> Archive(Guid id, VersionInput input) { await service.Archive(id, input.Version); return NoContent(); }
}
