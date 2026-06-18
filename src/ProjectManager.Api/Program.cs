using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

var auth = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()!;

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
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
