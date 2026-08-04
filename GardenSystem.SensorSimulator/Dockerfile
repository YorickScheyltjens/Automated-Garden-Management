FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["GardenSystem.SensorSimulator/GardenSystem.SensorSimulator.csproj", "GardenSystem.SensorSimulator/"]
COPY ["GardenSystem.Domain/GardenSystem.Domain.csproj", "GardenSystem.Domain/"]
RUN dotnet restore "GardenSystem.SensorSimulator/GardenSystem.SensorSimulator.csproj"

COPY . .
WORKDIR "/src/GardenSystem.SensorSimulator"
RUN dotnet publish "GardenSystem.SensorSimulator.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GardenSystem.SensorSimulator.dll"]
