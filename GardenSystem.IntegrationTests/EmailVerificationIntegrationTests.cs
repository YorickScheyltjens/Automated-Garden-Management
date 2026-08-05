using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using GardenSystem.Application.Auth.Dtos;
using GardenSystem.Infrastructure.Configuration;
using GardenSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace GardenSystem.IntegrationTests;

public sealed class EmailVerificationIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("gardensystem_email_verification_tests")
        .WithUsername("gardensystem")
        .WithPassword("gardensystem")
        .Build();

    private readonly IContainer _mailhogContainer = new ContainerBuilder("mailhog/mailhog")
        .WithPortBinding(1025, true)
        .WithPortBinding(8025, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(8025).ForPath("/api/v2/messages")))
        .Build();

    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private HttpClient? _mailhogClient;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgresContainer.StartAsync(), _mailhogContainer.StartAsync());

        _factory = new TestApiFactory(
            _postgresContainer.GetConnectionString(),
            _mailhogContainer.Hostname,
            _mailhogContainer.GetMappedPublicPort(1025));

        _client = _factory.CreateClient();
        _mailhogClient = new HttpClient
        {
            BaseAddress = new Uri($"http://{_mailhogContainer.Hostname}:{_mailhogContainer.GetMappedPublicPort(8025)}")
        };

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GardenDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _mailhogClient?.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _mailhogContainer.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task Register_ReadCodeFromMailhog_Verify_MarksEmailVerified()
    {
        var client = _client ?? throw new InvalidOperationException("Test client was not initialized.");
        var mailhogClient = _mailhogClient ?? throw new InvalidOperationException("Mailhog client was not initialized.");
        var factory = _factory ?? throw new InvalidOperationException("Test factory was not initialized.");

        var email = "verify.integration@gardensystem.local";
        var registerRequest = new RegisterUserRequestDto
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = email,
            Password = "supersecret1"
        };

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var registeredUser = await registerResponse.Content.ReadFromJsonAsync<UserResponseDto>();
        Assert.NotNull(registeredUser);
        Assert.False(registeredUser!.EmailVerified);

        var code = await ReadVerificationCodeFromMailhogAsync(mailhogClient, email);

        var verifyResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/verify-email",
            new VerifyEmailRequestDto { Email = email, Code = code });

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GardenDbContext>();
        var storedUser = await dbContext.Users.FirstOrDefaultAsync(user => user.Email == email);

        Assert.NotNull(storedUser);
        Assert.True(storedUser!.EmailVerified);
        Assert.Null(storedUser.EmailVerificationCodeHash);
        Assert.Null(storedUser.EmailVerificationCodeExpiresAtUtc);
    }

    private static async Task<string> ReadVerificationCodeFromMailhogAsync(HttpClient mailhogClient, string toEmail)
    {
        for (var attempt = 0; attempt < 40; attempt++)
        {
            var response = await mailhogClient.GetAsync("/api/v2/messages");
            var json = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var to = item.GetProperty("To")[0];
                    var toAddress = $"{to.GetProperty("Mailbox").GetString()}@{to.GetProperty("Domain").GetString()}";

                    if (!string.Equals(toAddress, toEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var body = item.GetProperty("Content").GetProperty("Body").GetString() ?? string.Empty;
                    var match = Regex.Match(body, @"\b\d{6}\b");

                    if (match.Success)
                    {
                        return match.Value;
                    }
                }
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"No verification email for '{toEmail}' arrived in Mailhog in time.");
    }

    private sealed class TestApiFactory(string connectionString, string smtpHost, int smtpPort) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<GardenDbContext>>();
                services.AddDbContext<GardenDbContext>(options => options.UseNpgsql(connectionString));

                services.RemoveAll<IConfigureOptions<SmtpOptions>>();
                services.AddOptions<SmtpOptions>().Configure(options =>
                {
                    options.Host = smtpHost;
                    options.Port = smtpPort;
                });
            });
        }
    }
}
