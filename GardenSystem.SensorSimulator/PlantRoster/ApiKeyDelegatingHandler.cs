using GardenSystem.SensorSimulator.Configuration;
using Microsoft.Extensions.Options;

namespace GardenSystem.SensorSimulator.PlantRoster;

/// <summary>
/// Attaches the shared service API key to every outgoing request to the Api.
/// </summary>
public sealed class ApiKeyDelegatingHandler(IOptions<ApiKeyOptions> apiKeyOptions) : DelegatingHandler
{
    private const string HeaderName = "X-Api-Key";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Add(HeaderName, apiKeyOptions.Value.Key);
        return base.SendAsync(request, cancellationToken);
    }
}
