using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
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

// 3. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
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
           .UseSnakeCaseNamingConvention()
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// 5. Validation
builder.Services.AddValidatorsFromAssemblyContaining<CreateClassRequestValidator>();

// 6. Application Services
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IHoldService, HoldService>();

// 7. Background Services
builder.Services.AddHostedService<ExpirySweeper>();

// 8. Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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

// 11. Map Endpoints
app.MapClassEndpoints();
app.MapHoldEndpoints();
app.MapStudentEndpoints();
app.MapHealthEndpoints();

app.Run();

public partial class Program { }
