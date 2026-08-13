using System.Linq;
using System.Threading.RateLimiting;
using InventoryManagement.API.Infrastructure.ErrorHandling;
using InventoryManagement.Domain.Configuration;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace InventoryManagement.API.Infrastructure;

/// <summary>Composition-root registrations for the API/presentation layer.</summary>
public static class ApiServiceCollectionExtensions
{
    /// <summary>Rate-limiter policy name applied to authentication endpoints.</summary>
    public const string AuthRateLimitPolicy = "auth";

    /// <summary>
    /// Binds and validates presentation options, and registers controllers, validation, exception
    /// handling, CORS, JWT authentication/authorization, rate limiting and health checks.
    /// </summary>
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<IdempotencyOptions>()
            .Bind(configuration.GetSection(IdempotencyOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(CorsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddControllers();

        // Unify model-validation failures onto the ApiResponse contract.
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context
                    .ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .ToList();

                return new BadRequestObjectResult(
                    ApiResponse<object>.Failure("Validation failed", errors)
                );
            };
        });

        // Centralized exception handling → ApiResponse.
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        AddCors(services, configuration);
        AddJwtAuthentication(services, configuration, environment);
        AddAuthorizationPolicies(services);
        AddRateLimiting(services, configuration);

        services.AddHealthChecks();

        return services;
    }

    private static void AddCors(IServiceCollection services, IConfiguration configuration)
    {
        var corsOptions =
            configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();

        services.AddCors(options =>
        {
            options.AddPolicy(
                CorsOptions.PolicyName,
                policy =>
                {
                    policy.WithOrigins(corsOptions.AllowedOrigins).AllowAnyMethod().AllowAnyHeader();
                    if (corsOptions.AllowCredentials && corsOptions.AllowedOrigins.Length > 0)
                    {
                        policy.AllowCredentials();
                    }
                }
            );
        });
    }

    private static void AddJwtAuthentication(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        var jwt =
            configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        // Configure the bearer options with the DI-resolved signing-key provider (no second
        // container). The key is resolved and cached on first use, shared with token generation.
        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IJwtSigningKeyProvider>(
                (options, keyProvider) =>
                {
                    options.RequireHttpsMetadata = !environment.IsDevelopment();
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = keyProvider
                            .GetValidationKeyAsync()
                            .GetAwaiter()
                            .GetResult(),
                        ValidateIssuer = true,
                        ValidIssuer = jwt.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwt.Audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds),
                    };
                }
            );
    }

    private static void AddAuthorizationPolicies(IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(
                AuthConstants.Policies.AdminOnly,
                policy => policy.RequireClaim(AuthConstants.Claims.IsAdmin, "True")
            )
            .AddPolicy(
                AuthConstants.Policies.AdminOrProvider,
                policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(AuthConstants.Claims.IsAdmin, "True")
                        || context.User.HasClaim(AuthConstants.Claims.IsProvider, "True")
                    )
            )
            .AddPolicy(
                AuthConstants.Policies.AllRoles,
                policy => policy.RequireAuthenticatedUser()
            );
    }

    private static void AddRateLimiting(IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue<int?>("RateLimiting:Auth:PermitLimit") ?? 10;
        var windowSeconds = configuration.GetValue<int?>("RateLimiting:Auth:WindowSeconds") ?? 60;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(
                AuthRateLimitPolicy,
                context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = permitLimit,
                            Window = TimeSpan.FromSeconds(windowSeconds),
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        }
                    )
            );
        });
    }
}
