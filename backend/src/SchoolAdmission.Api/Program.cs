using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Application.Options;
using SchoolAdmission.Api.Application.Services;
using SchoolAdmission.Api.Endpoints;
using SchoolAdmission.Api.Infrastructure.Background;
using SchoolAdmission.Api.Infrastructure.Database;
using SchoolAdmission.Api.Infrastructure.Persistence;
using SchoolAdmission.Api.Middleware;
using SchoolAdmission.Api.Validation;

var builder = WebApplication.CreateBuilder(args);

// 1. ProblemDetails & Exception Handling
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

// 2. JSON Options
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// 3. CORS — only the origins listed in CORS_ALLOWED_ORIGINS (comma-separated) may call the API
var allowedOrigins = (builder.Configuration["CORS_ALLOWED_ORIGINS"] ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

if (allowedOrigins.Length == 0)
{
    allowedOrigins = ["http://localhost:3000"];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("Date");
    });
});

// 4. Database Context
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("Default");
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention();
});

// 5. Validation
builder.Services.AddValidatorsFromAssemblyContaining<CreateClassRequestValidator>();

// 5b. Options — validated on start so a bad HOLD_DURATION_SECONDS fails fast instead of at request time
builder.Services
    .AddOptions<HoldOptions>()
    .Configure<IConfiguration>((options, configuration) =>
    {
        var raw = configuration[HoldOptions.ConfigKey];
        if (string.IsNullOrWhiteSpace(raw))
        {
            options.DurationSeconds = HoldOptions.DefaultDurationSeconds;
            return;
        }

        if (!int.TryParse(raw, out var seconds))
        {
            throw new InvalidOperationException(
                $"{HoldOptions.ConfigKey} must be an integer number of seconds, but was '{raw}'.");
        }

        options.DurationSeconds = seconds;
    })
    .Validate(o => o.DurationSeconds > 0, $"{HoldOptions.ConfigKey} must be greater than zero.")
    .ValidateOnStart();

// 6. Application Services
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IHoldService, HoldService>();

// 7. Background Services
builder.Services.AddHostedService<ExpirySweeper>();

// 8. Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.Logger.LogInformation("CORS allowed origins: {Origins}", string.Join(", ", allowedOrigins));

// 9. Startup Migrations and Demo Data Seeder
if (!app.Environment.IsEnvironment("Testing"))
{
    try
    {
        await DatabaseMigrator.MigrateAndSeedAsync(app.Services, app.Configuration, app.Logger);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to run migrations and seeder on startup.");
        if (!app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

// 10. Middleware Pipeline
app.UseExceptionHandler();
app.UseCors("AllowFrontend");

// Swagger always enabled
app.UseSwagger();
app.UseSwaggerUI();

// Startup informational banner with both URLs
app.Lifetime.ApplicationStarted.Register(() =>
{
    var hostName = System.Net.Dns.GetHostName();
    var ipAddresses = System.Net.Dns.GetHostAddresses(hostName)
        .Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        .Select(a => a.ToString())
        .ToList();

    app.Logger.LogInformation("=================================================");
    app.Logger.LogInformation("  School Admission Backend API is live!");
    app.Logger.LogInformation("  ➜ Local:   http://localhost:8080");
    foreach (var ip in ipAddresses)
    {
        app.Logger.LogInformation("  ➜ Network: http://{Ip}:8080", ip);
    }
    app.Logger.LogInformation("  ➜ Swagger: http://localhost:8080/swagger");
    app.Logger.LogInformation("=================================================");
});

// 11. Map Endpoints
app.MapClassEndpoints();
app.MapHoldEndpoints();
app.MapStudentEndpoints();
app.MapHealthEndpoints();

app.Run();

public partial class Program { }
