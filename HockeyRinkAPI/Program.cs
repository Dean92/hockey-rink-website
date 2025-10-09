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
        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            if (
                !string.IsNullOrEmpty(
                    builder.Configuration.GetConnectionString("DefaultConnection")
                )
            )
            {
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    sqlServerOptions =>
                        sqlServerOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null
                        )
                );
            }
            else
            {
                options.UseInMemoryDatabase("TestDatabase"); // Fallback for tests
            }
        });

        builder
            .Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        builder
            .Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/api/auth/login";
                options.AccessDeniedPath = "/api/auth/access-denied";
                options.Cookie.HttpOnly = false; // Allow JavaScript access for development
                options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Allow HTTP for development
                options.Cookie.SameSite = SameSiteMode.Lax; // More compatible than None
                options.ExpireTimeSpan = TimeSpan.FromHours(24);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context =>
                {
                    // Return 401 instead of redirecting for API calls
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

        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "HockeyRinkApi", Version = "v1" });
            c.AddSecurityDefinition(
                "CookieAuth",
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Cookie,
                    Name = "ASP.NET_SessionId",
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

        // Add CORS configuration
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(
                "AllowAngularDevServer",
                policy =>
                {
                    policy
                        .WithOrigins("http://localhost:4200") // Angular dev server
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials(); // Required for cookies
                }
            );

            options.AddPolicy(
                "AllowProduction",
                policy =>
                {
                    policy
                        .WithOrigins(
                            "https://hockey-rink-api-bbhch3gwgzedc9e3.centralus-01.azurewebsites.net"
                        ) // Replace with your deployed URL
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                }
            );
        });

        var app = builder.Build();

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
        app.UseRouting();
        app.UseCors("AllowProduction"); // Use "AllowProduction" in production
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // Skip migrations for in-memory database (tests)
            if (
                !string.IsNullOrEmpty(
                    builder.Configuration.GetConnectionString("DefaultConnection")
                )
            )
            {
                db.Database.Migrate();
            }
            if (!db.Leagues.Any())
            {
                db.Leagues.AddRange(
                    new League { Name = "Leisure", Description = "Beginner league" },
                    new League { Name = "Bronze", Description = "Intermediate league" },
                    new League { Name = "Silver", Description = "Intermediate league" },
                    new League { Name = "Gold", Description = "Advanced league" },
                    new League { Name = "Platinum", Description = "Elite league" },
                    new League { Name = "Diamond", Description = "Pro league" }
                );
                await db.SaveChangesAsync();
            }
        }

        app.Run();
    }
}
