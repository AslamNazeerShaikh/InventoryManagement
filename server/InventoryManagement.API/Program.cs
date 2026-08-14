using InventoryManagement.API.Infrastructure;
using InventoryManagement.Application.Extensions;
using InventoryManagement.Domain.Configuration;
using InventoryManagement.Infrastructure.Extensions;
using Scalar.AspNetCore;
using Serilog;

namespace InventoryManagement.API;

/// <summary>
/// Application entry point. Configures structured logging, dependency injection, security, the HTTP
/// pipeline (in the correct order) and database seeding for the Inventory Management API.
/// </summary>
public class Program
{
    /// <summary>Builds, configures and runs the web application.</summary>
    public static async Task<int> Main(string[] args)
    {
        // Stage 1: a bootstrap logger captures failures during startup itself.
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

        try
        {
            Log.Information("Starting Inventory Management API");

            var builder = WebApplication.CreateBuilder(args);

            // Stage 2: full structured logging read from configuration (Serilog section).
            builder.Host.UseSerilog(
                (context, services, configuration) =>
                    configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services)
                        .Enrich.FromLogContext()
            );

            builder.Services.AddInfrastructureServices(builder.Configuration);
            builder.Services.AddApplicationServices();
            builder.Services.AddApiServices(builder.Configuration, builder.Environment);

            builder.Services.AddOpenApi();

            var app = builder.Build();

            ConfigurePipeline(app);

            await app.Services.SeedDatabaseAsync().ConfigureAwait(false);

            Log.Information("Inventory Management API started successfully");
            await app.RunAsync().ConfigureAwait(false);
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync().ConfigureAwait(false);
        }
    }

    /// <summary>Configures the HTTP request pipeline with correctly ordered middleware.</summary>
    private static void ConfigurePipeline(WebApplication app)
    {
        // Global exception handling first so every downstream fault is converted uniformly.
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }
        else
        {
            app.UseHsts();
        }

        app.UseSerilogRequestLogging();
        app.UseHttpsRedirection();

        app.UseRouting();

        // CORS is applied in all environments (configuration-driven).
        app.UseCors(CorsOptions.PolicyName);

        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        // Idempotency runs AFTER authentication so anonymous callers cannot populate the store.
        app.UseMiddleware<IdempotencyMiddleware>();

        app.MapControllers();
        app.MapHealthChecks("/health");
    }
}
