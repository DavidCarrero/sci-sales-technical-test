using Microsoft.AspNetCore.Diagnostics;
using SciSales.Api;
using SciSales.Api.Endpoints;
using SciSales.Application;
using SciSales.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Composition root: the only place in the API that knows Infrastructure exists.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Any unhandled exception leaves as a plain RFC 9457 problem. The stack trace
// goes to the log, never to the caller.
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var feature = context.Features.Get<IExceptionHandlerFeature>();

    if (feature?.Error is not null)
    {
        ApiLog.UnhandledException(app.Logger, feature.Error, context.Request.Method, context.Request.Path);
    }

    await Results.Problem(
        title: "Unexpected error",
        detail: "The request could not be completed. The incident was logged.",
        statusCode: StatusCodes.Status500InternalServerError)
        .ExecuteAsync(context);
}));

app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("SCI Sales Catalog API"));
}

app.MapGet("/", () => Results.Redirect("/scalar/v1"))
    .ExcludeFromDescription();

app.MapGet("/health", () => TypedResults.Ok(new { status = "healthy" }))
    .WithTags("Diagnostics")
    .WithSummary("Liveness probe");

app.MapProductEndpoints();

await app.RunAsync();

/// <summary>Exposed so the integration tests can boot the real pipeline.</summary>
public partial class Program
{
    protected Program()
    {
    }
}
