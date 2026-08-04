using GardenSystem.Application;
using GardenSystem.Application.Abstractions;
using GardenSystem.Api.Middleware;
using GardenSystem.Api.Security;
using GardenSystem.Infrastructure;
using GardenSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<GardenDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddScoped<ICurrentUserProvider, SeededCurrentUserProvider>();

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

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
