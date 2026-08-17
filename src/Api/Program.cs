using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using TaskFlow.Api.Middleware;
using TaskFlow.Application;
using TaskFlow.Infrastructure;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Realtime;
using TaskFlow.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Logging -----------------------------------------------------------
builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

// --- Services ------------------------------------------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Enums (TaskState, TaskPriority, AlertSeverity, ...) must round-trip as strings —
// the Angular client sends/expects e.g. "Medium", not the numeric default System.Text.Json
// would otherwise use. Without this, requests with an enum body field fail deserialization
// and every enum field in a response comes back as a number the frontend never matches.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TaskFlow API",
        Version = "v1",
        Description = "Task management API with real-time workload anomaly detection."
    });
});

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")!, name: "postgres");

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

// The default signing key ships in appsettings.json (and in .env.example) so the Docker demo
// works with zero configuration — which also means it's public. Fail fast rather than run a
// real deployment where anyone can mint a valid token for any user.
const string DemoJwtSecret = "dev-only-secret-change-me-in-any-real-deployment-32chars+";
if (builder.Environment.IsProduction()
    && (jwtOptions.Secret == DemoJwtSecret || jwtOptions.Secret.Length < 32))
{
    throw new InvalidOperationException(
        "Jwt:Secret is the built-in demo key or shorter than 32 characters. Set a real one "
        + "(env var Jwt__Secret, or JWT_SECRET in .env) before running in Production.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep claim names exactly as issued (e.g. "sub"), instead of ASP.NET Core's default
        // remapping of "sub" -> ClaimTypes.NameIdentifier, so ICurrentUserService and
        // JwtTokenGenerator agree on the same claim name.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // Browsers can't set an Authorization header on a WebSocket handshake, so SignalR's
        // client sends the token as a query string param instead — accept it there, but only
        // for the hub path, so REST endpoints still require a real Authorization header.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// Throttle login/register per client IP so a brute-force credential-stuffing script can't
// hammer these anonymous endpoints — every other endpoint already requires a valid JWT.
// Limit is configurable (see "RateLimiting:Auth" in appsettings) so integration tests, which
// legitimately register far more than a real client would in a minute, can relax it. Read via
// httpContext.RequestServices (not the `builder.Configuration` captured above) so this reflects
// the fully-built configuration, including any overrides WebApplicationFactory adds in tests.
const string AuthRateLimiterPolicy = "auth";
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(AuthRateLimiterPolicy, httpContext =>
    {
        var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var section = config.GetSection("RateLimiting:Auth");
        var permitLimit = section.GetValue("PermitLimit", 10);
        var windowSeconds = section.GetValue("WindowSeconds", 60);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromSeconds(windowSeconds),
                PermitLimit = permitLimit,
                QueueLimit = 0
            });
    });
});

const string AngularDevCorsPolicy = "AngularDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularDevCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()); // required for SignalR
});

var app = builder.Build();

// --- Apply EF Core migrations automatically on startup --------------------
// Keeps the "docker compose up" one-command demo self-contained: no manual
// `dotnet ef database update` step required for reviewers trying the project.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();
    await db.Database.MigrateAsync();

    // Populates a demo workspace when enabled (Seed:Enabled) AND the database is empty —
    // see DemoDataSeeder. Off by default; docker-compose.yml turns it on for the demo.
    await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
}

// --- Middleware pipeline ---------------------------------------------------
// Serilog request logging is outermost so it records the FINAL response status. If it sat
// inside ExceptionHandlingMiddleware, an expected exception (e.g. a 400 validation failure)
// would still be propagating when Serilog logged it, so it would be recorded as a 500 even
// though the client correctly receives a 400.
app.UseSerilogRequestLogging();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(AngularDevCorsPolicy);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHub<AlertsHub>("/hubs/alerts");
app.MapHealthChecks("/health");

app.Run();

// Exposed for WebApplicationFactory<Program> in integration tests.
public partial class Program;
