FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["GardenSystem.Api/GardenSystem.Api.csproj", "GardenSystem.Api/"]
RUN dotnet restore "GardenSystem.Api/GardenSystem.Api.csproj"

COPY . .
WORKDIR "/src/GardenSystem.Api"
RUN dotnet publish "GardenSystem.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GardenSystem.Api.dll"]