using Microsoft.AspNetCore.Mvc;
using Stockroom.Api.Contracts;
using Stockroom.Api.Services;
namespace Stockroom.Api.Controllers;

[ApiController, Route("api/reports")]
public sealed class ReportsController(ReportService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Summary([FromQuery] ProductQuery query) => Ok(await service.Summary(query));
}
