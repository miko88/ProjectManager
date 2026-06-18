using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProjectManager.Api.Common;
using ProjectManager.Api.Endpoints;
using ProjectManager.Application;
using ProjectManager.Infrastructure;
using ProjectManager.Infrastructure.Auth;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configuration: add the XML config file required by the assignment.
builder.Configuration.AddXmlFile("config.xml", optional: false, reloadOnChange: true);

// Logging: Serilog, structured, console + rolling file.
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/projectmanager-.log", rollingInterval: RollingInterval.Day));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// JWT bearer options are bound from IOptions<AuthOptions> at the point the authentication
// handler is first activated (not snapshotted from builder.Configuration at startup), so
// config sources layered on afterwards (e.g. WebApplicationFactory's in-memory overrides
// in tests) are honored rather than a stale pre-Build() value.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<AuthOptions>>((options, authOptions) =>
    {
        var auth = authOptions.Value;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = auth.Issuer,
            ValidAudience = auth.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.SigningKey))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

const string ClientCors = "client";
builder.Services.AddCors(o => o.AddPolicy(ClientCors, p => p
    .WithOrigins(builder.Configuration["Cors:ClientOrigin"] ?? "http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseCors(ClientCors);
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapProjectEndpoints();

app.Run();

public partial class Program; // exposed for WebApplicationFactory
