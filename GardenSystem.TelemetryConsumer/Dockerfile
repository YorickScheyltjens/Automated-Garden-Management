FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["GardenSystem.TelemetryConsumer/GardenSystem.TelemetryConsumer.csproj", "GardenSystem.TelemetryConsumer/"]
COPY ["GardenSystem.Application/GardenSystem.Application.csproj", "GardenSystem.Application/"]
COPY ["GardenSystem.Infrastructure/GardenSystem.Infrastructure.csproj", "GardenSystem.Infrastructure/"]
COPY ["GardenSystem.Domain/GardenSystem.Domain.csproj", "GardenSystem.Domain/"]
RUN dotnet restore "GardenSystem.TelemetryConsumer/GardenSystem.TelemetryConsumer.csproj"

COPY . .
WORKDIR "/src/GardenSystem.TelemetryConsumer"
RUN dotnet publish "GardenSystem.TelemetryConsumer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GardenSystem.TelemetryConsumer.dll"]
