using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
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
    });
builder.Services.AddAuthorization();

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
}

// --- Middleware pipeline ---------------------------------------------------
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseCors(AngularDevCorsPolicy);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AlertsHub>("/hubs/alerts");
app.MapHealthChecks("/health");

app.Run();

// Exposed for WebApplicationFactory<Program> in integration tests.
public partial class Program;
