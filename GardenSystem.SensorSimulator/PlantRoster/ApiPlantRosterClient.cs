using System.Net.Http.Json;
using System.Text.Json;
using GardenSystem.Domain.Enums;

namespace GardenSystem.SensorSimulator.PlantRoster;

public sealed class ApiPlantRosterClient(HttpClient httpClient) : IPlantRosterClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<PlantRosterEntry>> GetPlantRosterAsync(CancellationToken cancellationToken)
    {
        var gardens = await httpClient.GetFromJsonAsync<List<GardenSummary>>("api/v1/gardens", JsonOptions, cancellationToken)
            ?? [];

        var roster = new List<PlantRosterEntry>();

        foreach (var garden in gardens)
        {
            var plants = await httpClient.GetFromJsonAsync<List<PlantSummary>>(
                $"api/v1/gardens/{garden.GardenId}/plants", JsonOptions, cancellationToken) ?? [];

            roster.AddRange(plants.Select(plant => new PlantRosterEntry(plant.PlantId, plant.PlantType)));
        }

        return roster;
    }

    private sealed record GardenSummary(Guid GardenId);

    private sealed record PlantSummary(Guid PlantId, PlantType PlantType);
}
