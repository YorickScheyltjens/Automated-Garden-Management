# GardenSystem

## Project Overview

GardenSystem is an Automated Garden Management System built with .NET 10.

The current solution contains four projects:
- GardenSystem.Api: ASP.NET Core Web API with controller-based endpoints.
- GardenSystem.Domain: dependency-free domain model (entities and enum).
- GardenSystem.Application: application layer placeholder for upcoming use cases.
- GardenSystem.Infrastructure: persistence layer with EF Core and PostgreSQL mapping.

The goal of this repository is to evolve in granular implementation steps, where each step introduces a small and reviewable slice of functionality.

## Architecture Choice Explanation

The architecture follows a layered approach with clear separation of concerns:

- Domain layer
  - Contains core business entities and enums.
  - Intentionally has no external dependencies.
  - Keeps business concepts stable and reusable.

- Application layer
  - Intended for use cases, orchestration, and business workflows.
  - Is currently scaffolded and ready for future steps.

- Infrastructure layer
  - Contains technical implementations, currently focused on data access.
  - Uses Entity Framework Core 10 and Npgsql for PostgreSQL.
  - Holds the DbContext, Fluent entity configurations, and migration files.

- API layer
  - Exposes HTTP endpoints using MVC controllers (not minimal APIs).
  - Hosts dependency injection wiring and runtime configuration.
  - Currently includes a health endpoint and OpenAPI endpoint mapping in development.

Why this structure:
- It supports maintainability by isolating business logic from framework details.
- It supports testability by making domain and application logic independent from transport and persistence.
- It supports incremental growth because new services (worker processes, telemetry handling, integrations) can be added without collapsing responsibilities into one project.

## Current Feature Status

### Implemented

- Solution and project structure
  - GardenSystem.sln with Api, Domain, Application, and Infrastructure projects.

- API basics
  - Controller-based API setup.
  - GET /health endpoint returning 200 OK with JSON status payload.

- Containerization
  - Multi-stage Dockerfile for GardenSystem.Api (SDK build stage, ASP.NET runtime final stage).
  - .dockerignore included.

- Local database orchestration
  - docker-compose.yml with:
    - db service using postgres:16-alpine
    - api service using the existing Api Dockerfile
    - db healthcheck and depends_on condition for api
  - .env.example with PostgreSQL connection settings.

- Persistence foundation
  - GardenDbContext in Infrastructure.
  - DbSet entries for currently existing entities:
    - User
    - Garden
    - Plant
  - Fluent API configurations via IEntityTypeConfiguration for all three entities.

- Database migrations
  - EF Core migration scaffolded and present in source control area.
  - Model snapshot present.

- Domain model currently in code
  - Entities:
    - User
    - Garden (including TargetHumidityLevel)
    - Plant
  - Enum:
    - PlantType (Vegetable, Fruit, Flower)

### Not Implemented Yet

- Additional domain entities planned in architecture but not currently present in Domain:
  - PlantState
  - IrrigationEvent

- Authentication and authorization workflows.
- Garden and plant CRUD endpoints.
- Reporting endpoints.
- Telemetry ingestion and irrigation decision workflow.
- RabbitMQ integration.
- Sensor simulator and telemetry consumer worker services.
- Repository abstractions/implementations beyond DbContext and mapping.
- Production deployment setup and cloud-specific infrastructure.
- End-to-end integration and domain-focused test coverage beyond foundational scaffolding.

## Run Notes (Current)

- Database only:
  - docker-compose up -d db

- API natively:
  - dotnet run --project GardenSystem.Api

- Health check:
  - GET http://localhost:5076/health

- API in container:
  - docker build -t gardensystem-api -f GardenSystem.Api/Dockerfile .
  - docker run -p 8080:8080 gardensystem-api

## Repository Context

This README reflects the current implementation state of the repository at this point in the step-by-step plan. As new steps are completed, this file should be updated to keep architecture and feature status accurate.
