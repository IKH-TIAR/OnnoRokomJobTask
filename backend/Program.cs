using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Application.Classes;
using SchoolAdmission.Application.Holds;
using SchoolAdmission.Endpoints;
using SchoolAdmission.Infrastructure.Exceptions;
using SchoolAdmission.Infrastructure.Persistence;
using SchoolAdmission.Infrastructure.Persistence.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog structured logging
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
});

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    };
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Binding failures (malformed JSON, wrong types) throw BadHttpRequestException in every
// environment so GlobalExceptionHandler returns a consistent ProblemDetails body.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

// Database (override with env var ConnectionStrings__DefaultConnection)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

// Enums are serialized as "HELD", "CONFIRMED", ... to match the API contract.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper)));

// Hold duration from the HOLD_DURATION_SECONDS env var (default 600)
var holdSeconds = builder.Configuration.GetValue<int?>(HoldSettings.DurationEnvVar) ?? HoldSettings.DefaultDurationSeconds;
if (holdSeconds <= 0)
    throw new InvalidOperationException($"{HoldSettings.DurationEnvVar} must be a positive number of seconds.");
builder.Services.AddSingleton(new HoldSettings(TimeSpan.FromSeconds(holdSeconds)));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IHoldService, HoldService>();

builder.Services.AddValidatorsFromAssemblyContaining<IClassService>();

// Report validation errors with camelCase keys ("totalSeats") to match the JSON contract.
ValidatorOptions.Global.PropertyNameResolver = (_, member, _) =>
    member is null ? null : JsonNamingPolicy.CamelCase.ConvertName(member.Name);

// The React app runs on a different origin. Restrict with Cors__AllowedOrigins (comma-separated);
// "*" allows any origin, which is fine because there is no auth or cookies.
var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"] ?? "*";
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Trim() == "*")
        policy.AllowAnyOrigin();
    else
        policy.WithOrigins(allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    policy.AllowAnyHeader().AllowAnyMethod();
}));

var app = builder.Build();

// Log every HTTP request with status code and elapsed time. Registered before the exception
// handler so it logs the final status code (e.g. 404) instead of seeing the raw exception as a 500.
app.UseSerilogRequestLogging();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).WithTags("Health");
app.MapClassEndpoints();
app.MapHoldEndpoints();

// Automatically apply pending database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Checking and applying pending database migrations...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migration completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations on startup.");
        throw;
    }
}

app.Run();

// Exposed for WebApplicationFactory in integration tests
public partial class Program;
