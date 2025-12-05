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
        if (builder.Environment.IsEnvironment("Testing"))
        {
            // Use InMemory database for testing
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("TestDatabase"));
        }
        else
        {
            // Use SQL Server for development and production
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
        }

        // Configure Identity
        builder
            .Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = false;
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
                options.Cookie.HttpOnly = false;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
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
        app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

        // Apply migrations and seed data (skip in Testing environment)
        if (!app.Environment.IsEnvironment("Testing"))
        {
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();

                try
                {
                    var db = services.GetRequiredService<AppDbContext>();
                    await db.Database.MigrateAsync();
                    logger.LogInformation("Database migrations applied successfully");

                    var leagueCount = await db.Leagues.CountAsync();
                    logger.LogInformation("Current league count in database: {LeagueCount}", leagueCount);

                    // Seed roles and admin user
                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

                    // Create Admin role if it doesn't exist
                    if (!await roleManager.RoleExistsAsync("Admin"))
                    {
                        await roleManager.CreateAsync(new IdentityRole("Admin"));
                        logger.LogInformation("Admin role created");
                    }

                    // Create User role if it doesn't exist
                    if (!await roleManager.RoleExistsAsync("User"))
                    {
                        await roleManager.CreateAsync(new IdentityRole("User"));
                        logger.LogInformation("User role created");
                    }

                    // Create admin user if it doesn't exist
                    var adminEmail = "admin@hockeyrink.com";
                    var adminUser = await userManager.FindByEmailAsync(adminEmail);
                    if (adminUser == null)
                    {
                        adminUser = new ApplicationUser
                        {
                            UserName = adminEmail,
                            Email = adminEmail,
                            FirstName = "Admin",
                            LastName = "User",
                            EmailConfirmed = true
                        };

                        var result = await userManager.CreateAsync(adminUser, "Admin123!");
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(adminUser, "Admin");
                            logger.LogInformation("Admin user created: {Email}", adminEmail);
                        }
                        else
                        {
                            logger.LogError("Failed to create admin user: {Errors}",
                                string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    var log = services.GetRequiredService<ILogger<Program>>();
                    log.LogError(ex, "An error occurred while applying database migrations or seeding data");
                }
            }
        }

        app.Run();
    }
}