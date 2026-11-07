using Microsoft.AspNetCore.Diagnostics;
using Stockroom.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()!.Error;
    var status = error is ApiException api ? api.Status : 500;
    if (status == 500) app.Logger.LogError(error, "Unhandled request error {TraceId}", context.TraceIdentifier);
    await Results.Problem(statusCode: status, title: status == 500 ? "An unexpected error occurred." : error.Message,
        extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }).ExecuteAsync(context);
}));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapOpenApi();
app.MapControllers();
app.Run();

public partial class Program;
