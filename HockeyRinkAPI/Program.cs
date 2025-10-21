using HockeyRinkAPI.Data;
using HockeyRinkAPI.Models;
using HockeyRinkAPI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;

namespace HockeyRinkAPI;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Serilog for logging
        builder.Host.UseSerilog(
            (context, services, configuration) =>
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
        );

        builder.Services.AddControllers();
        builder.Services.AddApplicationInsightsTelemetry();

        // Configure database context
        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString =
                Environment.GetEnvironmentVariable("DefaultConnection")
                ?? builder.Configuration.GetConnectionString("DefaultConnection");

            options.UseSqlServer(
                connectionString,
                sqlServerOptions =>
                    sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null
                    )
            );
        });

        // Configure Identity
        builder
            .Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = false; // Set to false for MVP
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // Configure authentication
        builder
            .Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/api/auth/login";
                options.AccessDeniedPath = "/api/auth/access-denied";
                options.Cookie.HttpOnly = false; // Allow JavaScript access for development
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Enforce HTTPS in production
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromHours(24);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = 401;
                        context.Response.Headers["Content-Type"] = "application/json";
                        return context.Response.WriteAsync("{\"error\": \"Unauthorized\"}");
                    }
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = 403;
                        context.Response.Headers["Content-Type"] = "application/json";
                        return context.Response.WriteAsync("{\"error\": \"Forbidden\"}");
                    }
                    return Task.CompletedTask;
                };
            });

        builder.Services.AddTransient<MockStripeService>();

        // Configure Swagger
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "HockeyRinkApi", Version = "v1" });
            c.AddSecurityDefinition(
                "CookieAuth",
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Cookie,
                    Name = ".AspNetCore.Cookies",
                    Description = "Cookie-based authentication using ASP.NET Identity",
                }
            );
            c.AddSecurityRequirement(
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "CookieAuth",
                            },
                        },
                        new string[] { }
                    },
                }
            );
        });

        // Configure CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(
                "AllowAngularDevServer",
                policy =>
                {
                    policy
                        .WithOrigins("http://localhost:4200")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                }
            );

            options.AddPolicy(
                "AllowProduction",
                policy =>
                {
                    policy
                        .WithOrigins("https://lively-river-0c3237510.1.azurestaticapps.net")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .SetPreflightMaxAge(TimeSpan.FromMinutes(30));
                }
            );
        });

        var app = builder.Build();

        // Configure the middleware pipeline
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\": \"Internal Server Error\"}");
            });
        });

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "HockeyRinkApi v1");
                c.RoutePrefix = string.Empty;
            });
        }

        app.UseSerilogRequestLogging();

        // Use different CORS policies based on environment
        if (app.Environment.IsDevelopment())
        {
            app.UseCors("AllowAngularDevServer");
        }
        else
        {
            app.UseCors("AllowProduction");
        }

        app.UseRouting();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        // Add a simple health check endpoint
        app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

        // Apply migrations automatically (optional - good for development)
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            try
            {
                await db.Database.MigrateAsync();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Database migrations applied successfully");

                // Log league count for verification
                var leagueCount = await db.Leagues.CountAsync();
                logger.LogInformation("Current league count in database: {LeagueCount}", leagueCount);
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while applying database migrations");
            }
        }

        app.Run();
    }
}