using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Auth.Dtos;
using GardenSystem.Application.Gardens.Dtos;
using GardenSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace GardenSystem.IntegrationTests;

public sealed class LoginIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("gardensystem_login_tests")
        .WithUsername("gardensystem")
        .WithPassword("gardensystem")
        .Build();

    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private CapturingEmailSender? _emailSender;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        _factory = new TestApiFactory(_postgresContainer.GetConnectionString());
        _client = _factory.CreateClient();
        _emailSender = (CapturingEmailSender)_factory.Services.GetRequiredService<IEmailSender>();

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GardenDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task RegisterVerifyLoginRefresh_FullFlow_WorksAndRotatesRefreshToken()
    {
        var client = _client ?? throw new InvalidOperationException("Test client was not initialized.");
        var emailSender = _emailSender ?? throw new InvalidOperationException("Email sender was not initialized.");

        const string password = "supersecret1";
        var registerRequest = new RegisterUserRequestDto
        {
            FirstName = "Login",
            LastName = "Flow",
            Email = "login.flow.integration@gardensystem.local",
            Password = password
        };

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var code = Regex.Match(emailSender.LastBody ?? string.Empty, @"\b\d{6}\b").Value;
        Assert.NotEmpty(code);

        var verifyResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/verify-email",
            new VerifyEmailRequestDto { Email = registerRequest.Email, Code = code });
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequestDto { Email = registerRequest.Email, Password = password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginTokens = await loginResponse.Content.ReadFromJsonAsync<AuthTokensResponseDto>();
        Assert.NotNull(loginTokens);
        Assert.NotEmpty(loginTokens!.AccessToken);
        Assert.NotEmpty(loginTokens.RefreshToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginTokens.AccessToken);
        var authorizedGardensResponse = await client.GetAsync("/api/v1/gardens");
        Assert.Equal(HttpStatusCode.OK, authorizedGardensResponse.StatusCode);

        var gardens = await authorizedGardensResponse.Content.ReadFromJsonAsync<List<GardenResponseDto>>();
        Assert.NotNull(gardens);
        Assert.Empty(gardens!);

        var refreshResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshRequestDto { RefreshToken = loginTokens.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshedTokens = await refreshResponse.Content.ReadFromJsonAsync<AuthTokensResponseDto>();
        Assert.NotNull(refreshedTokens);
        Assert.NotEmpty(refreshedTokens!.AccessToken);
        Assert.NotEqual(loginTokens.RefreshToken, refreshedTokens.RefreshToken);

        var reusedOldTokenResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshRequestDto { RefreshToken = loginTokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reusedOldTokenResponse.StatusCode);

        var reuseNewTokenResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshRequestDto { RefreshToken = refreshedTokens.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, reuseNewTokenResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = _client ?? throw new InvalidOperationException("Test client was not initialized.");

        var registerRequest = new RegisterUserRequestDto
        {
            FirstName = "Wrong",
            LastName = "Password",
            Email = "wrong.password.integration@gardensystem.local",
            Password = "supersecret1"
        };

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequestDto { Email = registerRequest.Email, Password = "not-the-right-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Login_BeforeEmailVerified_ReturnsUnauthorized()
    {
        var client = _client ?? throw new InvalidOperationException("Test client was not initialized.");

        var registerRequest = new RegisterUserRequestDto
        {
            FirstName = "Not",
            LastName = "Verified",
            Email = "not.verified.integration@gardensystem.local",
            Password = "supersecret1"
        };

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequestDto { Email = registerRequest.Email, Password = "supersecret1" });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    private sealed class TestApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<GardenDbContext>>();
                services.AddDbContext<GardenDbContext>(options => options.UseNpgsql(connectionString));

                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender, CapturingEmailSender>();
            });
        }
    }

    private sealed class CapturingEmailSender : IEmailSender
    {
        public string? LastBody { get; private set; }

        public Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
        {
            LastBody = body;
            return Task.CompletedTask;
        }
    }
}
