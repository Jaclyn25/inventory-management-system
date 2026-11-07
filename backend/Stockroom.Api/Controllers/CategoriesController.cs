using Microsoft.AspNetCore.Mvc;
using Stockroom.Api.Contracts;
using Stockroom.Api.Services;
namespace Stockroom.Api.Controllers;

[ApiController, Route("api/categories")]
public sealed class CategoriesController(CategoryService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List() => Ok(await service.List());
    [HttpPost] public async Task<IActionResult> Create(CategoryInput input)
    {
        var category = await service.Save(input);
        return Created($"/api/categories/{category.Id}", category);
    }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, CategoryInput input) => Ok(await service.Save(input, id));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id) { await service.Delete(id); return NoContent(); }
}
