using Microsoft.AspNetCore.Diagnostics;
using Stockroom.Api;
using Stockroom.Api.Data;
using Stockroom.Api.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new DecimalConverter()));
builder.Services.AddOpenApi(o => o.AddSchemaTransformer((schema, context, _) =>
{
    if (context.JsonTypeInfo.Type == typeof(decimal))
    {
        schema.Type = Microsoft.OpenApi.JsonSchemaType.String;
        schema.Format = null;
        schema.Pattern = @"^\d+\.\d{2}$";
    }
    return Task.CompletedTask;
}));
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<InventoryDb>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Inventory") ?? "Host=localhost;Database=stockroom"));
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<StockService>();
builder.Services.AddScoped<ReportService>();
var app = builder.Build();
if (builder.Configuration.GetValue<bool>("Database:Migrate"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<InventoryDb>().Database.MigrateAsync();
}
if (builder.Configuration.GetValue<bool>("Database:Seed"))
{
    using var scope = app.Services.CreateScope();
    await SeedData.Initialize(scope.ServiceProvider.GetRequiredService<InventoryDb>());
}
app.MapGet("/api/health", async (InventoryDb db) => await db.Database.CanConnectAsync() ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503));
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()!.Error;
    var pg = (error as DbUpdateException)?.InnerException as PostgresException ?? error as PostgresException;
    var status = error is ApiException api ? api.Status : error is DbUpdateConcurrencyException ? 409 : pg?.SqlState switch
    {
        PostgresErrorCodes.UniqueViolation => 409,
        PostgresErrorCodes.ForeignKeyViolation => 409,
        PostgresErrorCodes.CheckViolation => 400,
        _ => 500
    };
    if (status == 500) app.Logger.LogError(error, "Unhandled request error {TraceId}", context.TraceIdentifier);
    var title = error is ApiException ? error.Message : status switch { 409 => "A duplicate or referenced record prevents this change.", 400 => "The change violates inventory constraints.", _ => "An unexpected error occurred." };
    await Results.Problem(statusCode: status, title: title,
        extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }).ExecuteAsync(context);
}));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapOpenApi();
app.MapControllers();
app.Run();

public partial class Program;
