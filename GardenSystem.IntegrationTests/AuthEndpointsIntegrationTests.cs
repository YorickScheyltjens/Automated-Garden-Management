using System.Net;
using System.Net.Http.Json;
using GardenSystem.Application.Auth.Dtos;
using GardenSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace GardenSystem.IntegrationTests;

public sealed class AuthEndpointsIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("gardensystem_auth_tests")
        .WithUsername("gardensystem")
        .WithPassword("gardensystem")
        .Build();

    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        _factory = new TestApiFactory(_postgresContainer.GetConnectionString());
        _client = _factory.CreateClient();

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GardenDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_client is not null)
        {
            _client.Dispose();
        }

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task Register_CreatesUserWithHashedPasswordAndUnverifiedEmail()
    {
        var client = _client ?? throw new InvalidOperationException("Test client was not initialized.");
        var factory = _factory ?? throw new InvalidOperationException("Test factory was not initialized.");

        var plaintextPassword = "supersecret1";
        var registerRequest = new RegisterUserRequestDto
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane.doe.integration@gardensystem.local",
            Password = plaintextPassword
        };

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var registeredUser = await registerResponse.Content.ReadFromJsonAsync<UserResponseDto>();
        Assert.NotNull(registeredUser);
        Assert.False(registeredUser!.EmailVerified);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GardenDbContext>();

        var storedUser = await dbContext.Users.FirstOrDefaultAsync(user => user.UserId == registeredUser.UserId);

        Assert.NotNull(storedUser);
        Assert.False(storedUser!.EmailVerified);
        Assert.NotEqual(plaintextPassword, storedUser.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(plaintextPassword, storedUser.PasswordHash));
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyRegistered_ReturnsConflict()
    {
        var client = _client ?? throw new InvalidOperationException("Test client was not initialized.");

        var registerRequest = new RegisterUserRequestDto
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "duplicate.integration@gardensystem.local",
            Password = "supersecret1"
        };

        var firstResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var problem = await secondResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.Conflict, problem!.Status);
    }

    private sealed class TestApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<GardenDbContext>>();
                services.AddDbContext<GardenDbContext>(options => options.UseNpgsql(connectionString));
            });
        }
    }
}
