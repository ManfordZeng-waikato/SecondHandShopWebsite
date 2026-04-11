using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SecondHandShop.Application.Security;
using SecondHandShop.Application.UseCases.Admin.Login;
using SecondHandShop.Application.UseCases.Admin.RefreshSession;
using SecondHandShop.Infrastructure;
using SecondHandShop.Infrastructure.Persistence;
using SecondHandShop.Infrastructure.Services;
using SecondHandShop.WebApi.Authentication;
using SecondHandShop.WebApi.Controllers;
using SecondHandShop.WebApi.Filters;
using SecondHandShop.WebApi.Middleware;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
var builder = WebApplication.CreateBuilder(args);
const string FrontendCorsPolicy = "FrontendCorsPolicy";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();
allowedOrigins = allowedOrigins
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim().TrimEnd('/'))
    .ToArray();

// Fail-fast: a silently-empty CORS policy leads to confusing "request blocked" symptoms in the
// browser and tempts maintainers to "fix" it with AllowAnyOrigin() + AllowCredentials() — a
// combination ASP.NET rejects at runtime, which then tempts a regression to SetIsOriginAllowed(_ => true).
// Both are insecure. Require the operator to configure Cors:AllowedOrigins explicitly instead.
if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException(
        "Cors:AllowedOrigins is not configured. Set at least one explicit origin (e.g. \"https://localhost:5173\"). " +
        "DO NOT use AllowAnyOrigin() with AllowCredentials(), and DO NOT use SetIsOriginAllowed(_ => true).");
}

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<AdminAuthCookieOptions>(
    builder.Configuration.GetSection(AdminAuthCookieOptions.SectionName));
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<LoginAdminCommand>());

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue(AdminAuthController.CookieName, out var token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                if (context.SecurityToken is not JwtSecurityToken jwt)
                {
                    return;
                }

                var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                var accessMinutes = config.GetValue("Jwt:AccessTokenMinutes", 20.0);
                var fraction = Math.Clamp(config.GetValue("Jwt:SlidingRenewalFraction", 0.5), 0.05, 0.95);
                var total = TimeSpan.FromMinutes(accessMinutes);
                var threshold = TimeSpan.FromTicks((long)(total.Ticks * fraction));
                var remaining = jwt.ValidTo - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero || remaining >= threshold)
                {
                    return;
                }

                var sub = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                if (!Guid.TryParse(sub, out var adminId))
                {
                    return;
                }

                var mediator = context.HttpContext.RequestServices.GetRequiredService<IMediator>();
                var cookieOpts = context.HttpContext.RequestServices.GetRequiredService<IOptions<AdminAuthCookieOptions>>().Value;

                try
                {
                    var result = await mediator.Send(
                        new RefreshAdminSessionCommand(adminId),
                        context.HttpContext.RequestAborted);
                    AdminAuthCookies.AppendAuthTokenCookie(
                        context.HttpContext.Response,
                        result.Token,
                        result.ExpiresAt,
                        cookieOpts);
                    AdminAuthCookies.AppendSessionExpiresHeader(context.HttpContext.Response, result.ExpiresAt);
                }
                catch (UnauthorizedAccessException)
                {
                    // Account deactivated: do not extend; existing JWT expires naturally.
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Any valid admin JWT (including restricted first-login tokens).
    options.AddPolicy("AdminSession", policy => policy.RequireRole("Admin"));
    // Full back-office: rejects tokens carrying pwd_chg_req (issued while MustChangePassword is true).
    options.AddPolicy("AdminFullAccess", policy =>
        policy.RequireRole("Admin").RequireAssertion(context =>
            context.User.FindFirst(AdminJwtClaimTypes.PasswordChangeRequired)?.Value != "true"));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("LoginRateLimit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("SearchRateLimit", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 3,
                QueueLimit = 0
            }));
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 2;

    var trustAllProxies = builder.Configuration.GetValue<bool>("ReverseProxy:TrustAllProxies");
    if (trustAllProxies)
    {
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();
    }
    else
    {
        var knownProxies = builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>();
        if (knownProxies is { Length: > 0 })
        {
            options.KnownProxies.Clear();
            foreach (var proxy in knownProxies)
            {
                if (IPAddress.TryParse(proxy, out var ip))
                    options.KnownProxies.Add(ip);
            }
        }

        var knownNetworks = builder.Configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>();
        if (knownNetworks is { Length: > 0 })
        {
            options.KnownIPNetworks.Clear();
            foreach (var network in knownNetworks)
            {
                if (System.Net.IPNetwork.TryParse(network, out var parsed))
                    options.KnownIPNetworks.Add(parsed);
            }
        }
    }
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
});
builder.Services.AddResponseCaching();
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("CategoriesList", builder => builder.Expire(TimeSpan.FromMinutes(5)));
    options.AddPolicy("CategoriesTree", builder => builder.Expire(TimeSpan.FromMinutes(5)));
});
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    // SECURITY: this policy uses AllowCredentials() because the admin session cookie is HttpOnly
    // and must be forwarded from the SPA origin. As a consequence:
    //   - NEVER combine this with AllowAnyOrigin() — ASP.NET will throw at runtime.
    //   - NEVER replace WithOrigins(...) with SetIsOriginAllowed(_ => true) as a "quick fix";
    //     that is equivalent to allow-any-origin + credentials and exposes the site to CSRF
    //     from arbitrary origins.
    // To permit a new origin, add it to Cors:AllowedOrigins in configuration.
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders(AdminAuthCookies.SessionExpiresHeaderName));
});
builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = builder.Configuration.GetValue<int?>("HttpsPort");
});
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(180);
    options.IncludeSubDomains = true;
});

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "SecondHandShop.WebApi");
});

var app = builder.Build();

app.Services.GetRequiredService<IOptions<AdminAuthCookieOptions>>().Value.Validate();

var applyMigrationsOnStartup = builder.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true);
if (applyMigrationsOnStartup)
{
    await using var migrateScope = app.Services.CreateAsyncScope();
    var db = migrateScope.ServiceProvider.GetRequiredService<SecondHandShopDbContext>();
    await db.Database.MigrateAsync();
}

var seedAdminOnStartup = builder.Configuration.GetValue("Database:SeedAdminOnStartup", true);
if (seedAdminOnStartup)
{
    await AdminSeedService.SeedAdminUserAsync(app.Services);
}

var seedCatalogOnStartup = builder.Configuration.GetValue("Database:SeedCatalogOnStartup", true);
if (seedCatalogOnStartup)
{
    await CatalogSeedService.SeedDefaultCategoriesIfEmptyAsync(app.Services);
}

app.UseForwardedHeaders();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, _, ex) =>
    {
        if (ex is not null)
        {
            return LogEventLevel.Error;
        }

        var status = httpContext.Response.StatusCode;
        if (status >= StatusCodes.Status500InternalServerError)
        {
            return LogEventLevel.Error;
        }

        if (status >= StatusCodes.Status400BadRequest)
        {
            return LogEventLevel.Warning;
        }

        return LogEventLevel.Information;
    };
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.UseResponseCaching();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();
app.MapControllers();
app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
