using System.Text;
using InventoryManagement.Application.Extensions;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

namespace InventoryManagement.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Configure Serilog first
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(
                new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile("appsettings.Development.json", optional: true)
                    .Build()
            )
            .CreateLogger();

        try
        {
            Log.Information("Starting Inventory Management API");

            var builder = WebApplication.CreateBuilder(args);

            // Use Serilog
            builder.Host.UseSerilog();

            // Add services to the container
            builder.Services.AddControllers();

            // Add CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(
                    "DevelopmentPolicy",
                    policy =>
                    {
                        var allowedOrigins =
                            builder.Configuration.GetSection("CORS:AllowedOrigins").Get<string[]>()
                            ?? [];
                        policy
                            .WithOrigins(allowedOrigins)
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials();
                    }
                );
            });

            // Add Infrastructure services (Database, Repositories)
            builder.Services.AddInfrastructureServices(builder.Configuration);

            // Add Application services
            builder.Services.AddApplicationServices();

            // Add JWT Authentication
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

            builder
                .Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false; // Only for development
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings["Issuer"],
                        ValidateAudience = true,
                        ValidAudience = jwtSettings["Audience"],
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                    };
                });

            // Add Authorization with policies
            builder.Services.AddAuthorization(options =>
            {
                // Admin only policy
                options.AddPolicy(
                    AuthConstants.Policies.AdminOnly,
                    policy => policy.RequireClaim(AuthConstants.Claims.IsAdmin, "True")
                );

                // Admin or Provider policy
                options.AddPolicy(
                    AuthConstants.Policies.AdminOrProvider,
                    policy =>
                        policy.RequireAssertion(context =>
                            context.User.HasClaim(AuthConstants.Claims.IsAdmin, "True")
                            || context.User.HasClaim(AuthConstants.Claims.IsProvider, "True")
                        )
                );

                // All authenticated users policy
                options.AddPolicy(
                    AuthConstants.Policies.AllRoles,
                    policy => policy.RequireAuthenticatedUser()
                );

                // Role-based policies
                options.AddPolicy(
                    "RequireAdminRole",
                    policy =>
                        policy.RequireClaim(AuthConstants.Claims.Role, AuthConstants.Roles.Admin)
                );

                options.AddPolicy(
                    "RequireProviderRole",
                    policy =>
                        policy.RequireClaim(
                            AuthConstants.Claims.Role,
                            AuthConstants.Roles.NursePractitioner
                        )
                );

                options.AddPolicy(
                    "RequireStaffRole",
                    policy =>
                        policy.RequireClaim(AuthConstants.Claims.Role, AuthConstants.Roles.Staff)
                );
            });

            // Add OpenAPI
            builder.Services.AddOpenApi();

            // Add problem details
            builder.Services.AddProblemDetails();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.MapOpenApi();
                app.MapScalarApiReference();
                app.UseCors("DevelopmentPolicy");
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            // Middleware pipeline
            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate =
                    "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                    diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                    diagnosticContext.Set(
                        "UserAgent",
                        httpContext.Request.Headers.UserAgent.ToString()
                    );
                    if (httpContext.User.Identity?.IsAuthenticated == true)
                    {
                        diagnosticContext.Set(
                            "UserId",
                            httpContext.User.FindFirst(AuthConstants.Claims.UserId)?.Value
                        );
                        diagnosticContext.Set(
                            "UserEmail",
                            httpContext.User.FindFirst(AuthConstants.Claims.Email)?.Value
                        );
                    }
                };
            });

            app.UseHttpsRedirection();
            app.UseRouting();

            // Authentication & Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // Map controllers
            app.MapControllers();

            // Seed database
            using (var scope = app.Services.CreateScope())
            {
                await scope.ServiceProvider.SeedDatabaseAsync();
            }

            Log.Information("Inventory Management API started successfully");
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
