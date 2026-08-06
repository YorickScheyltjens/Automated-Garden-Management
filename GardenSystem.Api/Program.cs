using System.Text;
using GardenSystem.Application;
using GardenSystem.Application.Abstractions;
using GardenSystem.Api.Middleware;
using GardenSystem.Api.Security;
using GardenSystem.Infrastructure;
using GardenSystem.Infrastructure.Configuration;
using GardenSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<GardenDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserProvider, JwtCurrentUserProvider>();

builder.Services.AddOptions<SmtpOptions>().Bind(builder.Configuration.GetSection("Smtp"));

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Configuration key 'Jwt:Secret' was not found.");

builder.Services.AddOptions<JwtOptions>().Bind(builder.Configuration.GetSection("Jwt"));
builder.Services.AddOptions<GardenSystem.Api.Security.ApiKeyOptions>().Bind(builder.Configuration.GetSection("ApiKey"));

const string AuthSchemeSelectorName = "AuthSchemeSelector";

builder.Services.AddAuthentication(AuthSchemeSelectorName)
    .AddPolicyScheme(AuthSchemeSelectorName, AuthSchemeSelectorName, options =>
    {
        options.ForwardDefaultSelector = context =>
            context.Request.Headers.ContainsKey(ApiKeyAuthenticationHandler.HeaderName)
                ? ApiKeyAuthenticationHandler.SchemeName
                : JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    })
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, _ => { });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
