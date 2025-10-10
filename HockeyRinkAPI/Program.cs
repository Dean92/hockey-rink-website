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
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "DefaultConnection string is missing or empty."
                );
            }
            options.UseSqlServer(
                connectionString,
                sqlServerOptions =>
                    sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null
                    )
            );
        });

        // Configure Identity
        builder
            .Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
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
                        .AllowCredentials();
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
        app.UseRouting();
        app.UseCors("AllowProduction");
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();

        // Handle OPTIONS requests for CORS
        app.Use(
            async (context, next) =>
            {
                if (context.Request.Method == "OPTIONS")
                {
                    context.Response.Headers["Access-Control-Allow-Origin"] =
                        "https://lively-river-0c3237510.1.azurestaticapps.net";
                    context.Response.Headers["Access-Control-Allow-Methods"] =
                        "GET, POST, PUT, DELETE, OPTIONS";
                    context.Response.Headers["Access-Control-Allow-Headers"] =
                        "Content-Type, Authorization";
                    context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
                    context.Response.StatusCode = 200;
                    return;
                }
                await next();
            }
        );

        app.MapControllers();

        // Apply migrations and seed data
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
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
