# GardenSystem

An Automated Garden Management System — backend case study for the .NET
Backend Developer role at In The Pocket, built by Yorick Scheyltjens.

Garden owners register, verify their email, and manage gardens and plants
through a REST API. Behind that, a simulated irrigation system (a separate
worker process standing in for real hardware) reports plant humidity over
MQTT, a telemetry consumer decides when to water and logs every irrigation
event, and reporting endpoints turn that log into answers ("how often was
this plant watered this month?"). Everything runs locally via
`docker-compose up` — nothing is deployed live.

`docs/ARCHITECTURE.md` is the full design spec, including the reasoning
behind every non-obvious decision (why RabbitMQ over Kafka, why MQTT *and*
AMQP, why the simulator — not the API — owns the irrigation state machine,
what would change for a real cloud deployment, and more). `docs/RABBITMQ.md`
goes one level deeper on RabbitMQ specifically: exactly how the MQTT and
AMQP sides are bridged through the `amq.topic` exchange. This README is
the shorter entry point: what's here, why it's shaped this way, and how to
run it.

## Architecture

Six moving parts, dependencies pointing inward within the .NET solution:

| Component | What it is | Talks to |
|---|---|---|
| **Api** | ASP.NET Core Web API — auth, CRUD, reporting | Users (HTTP), Postgres |
| **TelemetryConsumer** | Worker Service — consumes sensor telemetry, decides watering, publishes commands | RabbitMQ (AMQP), Postgres |
| **SensorSimulator** | Worker Service — plays the physical irrigation unit | RabbitMQ (MQTT), Api (HTTP, `X-Api-Key`) |
| **RabbitMQ** | Broker (MQTT + AMQP plugin) — the only channel between cloud logic and the "device" | Everything above |
| **Postgres** | Data store | Api, TelemetryConsumer |
| **Domain** (shared library, not a container) | Pure business rules (overcrowding, decay/recovery rates, watering decisions), referenced directly by both the API side and the simulator so they can never drift out of sync | — |

```
GardenSystem.Api ──────┐
                        ├──▶ GardenSystem.Application ──▶ GardenSystem.Domain
GardenSystem.Infrastructure ─┘        ▲
        ▲                             │
        └── GardenSystem.TelemetryConsumer

GardenSystem.SensorSimulator ──▶ GardenSystem.Domain   (own container, own protocol)
```

- **Domain** — no dependencies on anything. Entities, enums, and pure
  calculation/decision logic (`IrrigationRates`, `TelemetryEvaluator`,
  `Garden.CanFitPlant`). The most heavily unit-tested code in the
  solution, because it's pure and it's where a wrong answer would be a
  silent bug rather than a crash.
- **Application** — CQRS-lite via MediatR: one command/query + handler per
  use case, FluentValidation validators running as a pipeline behavior
  ahead of every handler, repository *interfaces* only.
- **Infrastructure** — EF Core (`GardenDbContext`), repository
  implementations, the RabbitMQ publisher/consumer client, the email
  sender (Mailhog locally).
- **Api** — thin controllers dispatching to MediatR. No business logic, no
  manual `ModelState` checks — validation happens in the pipeline.
- **TelemetryConsumer** — its own container, deliberately not inside the
  Api process, so a spike in telemetry volume can't starve user-facing
  HTTP traffic (or vice versa) for the same thread pool and DB connection
  pool.
- **SensorSimulator** — its own container. Owns the humidity decay/
  recovery state machine, because the cloud side should *observe* that a
  plant got watered via telemetry, not unilaterally decide that it did.

Why not Minimal APIs for the Api project? For a domain with this many
cross-cutting concerns (validation, auth, ownership checks, pagination),
controllers + MediatR keep the HTTP layer thin and the business rules
testable without spinning up the ASP.NET pipeline. See
`docs/ARCHITECTURE.md` §3 for the deeper reasoning, including the
RabbitMQ-vs-Kafka and MQTT-vs-AMQP decisions.

## Key assumptions

The assignment leaves some points open. Rather than guess silently, here's
what was decided and why (full table in `docs/ARCHITECTURE.md` §1):

- **Auth gates everything** except `POST /auth/register` and
  `POST /auth/login` — the domain model ties gardens to a user, so without
  auth that link has no enforcement mechanism.
- **`PlantState` (current snapshot) + `IrrigationEvent` (append-only log)**
  instead of a single mutable metric — the reporting bonus needs watering
  *frequency*, which is impossible to reconstruct from a single
  last-known-state row.
- **Soft delete** (`DeletedAtUtc`) on `User`, `Garden`, and `Plant` — the
  reporting bonus needs "plants deleted since date X," which a hard
  delete would destroy.
- **Simulated time is compressed**: 1 tick = 1 simulated minute = 5 real
  seconds by default, so a reviewer running this locally doesn't wait a
  real hour to see a full watering cycle.
- **.NET 10**, not .NET 8 — the vacancy asks for ".NET 6+," but .NET 8 and
  9 both reach end-of-support November 2026; starting a new project three
  months before its runtime loses support was an avoidable risk.
- **Device-initiated MQTT**, not push or polling, for the cloud-to-device
  link — a real home irrigation controller sits behind NAT with no public
  inbound IP, so the device has to be the one holding the connection open.
- **A separate shared-secret (`X-Api-Key`) scheme, not the user JWT flow,
  authenticates `SensorSimulator`'s roster-polling calls to the Api** — it's
  a machine, not a user, and giving it a login/password would blur that
  line. `docs/ARCHITECTURE.md` §7 doesn't cover this case explicitly, so
  it's called out here.

## API overview

All routes under `/api/v1`. Everything except `/auth/register`,
`/auth/verify-email`, `/auth/login`, and `/auth/refresh` requires
`Authorization: Bearer <jwt>`.

```
POST   /auth/register            → 201, sends a verification email
POST   /auth/verify-email        → 200
POST   /auth/login               → 200, { accessToken, refreshToken }
POST   /auth/refresh             → 200
DELETE /auth/me                  → 204 (soft-deletes the account, cascades)

GET    /gardens                  → paginated list, current user's gardens only
POST   /gardens                  → 201
GET    /gardens/{id}             → 200 | 404  (cached, 30s)
PUT    /gardens/{id}              → 200 | 404
DELETE /gardens/{id}              → 204 (soft delete)

GET    /gardens/{gardenId}/plants → paginated list
POST   /gardens/{gardenId}/plants → 201 | 409 (overcrowding)
GET    /plants/{id}               → 200 | 404  (cached, 30s)
PUT    /plants/{id}               → 200 | 404 | 409 (overcrowding)
DELETE /plants/{id}               → 204

GET /reports/watering-summary?from=&to=
GET /reports/watering-frequency/{plantId}?period=30m|1h
GET /reports/plant-changes?since=2026-08-01
```

List endpoints accept `skip`/`take` (default `take=20`, capped at 100).
Every error returns RFC 7807 `ProblemDetails`. Full interactive docs (via
Scalar, generated from code — never hand-maintained) are available at
`http://localhost:8080/scalar` when running the Api locally with
`dotnet run` (Development environment). The `docker-compose` stack runs
the Api in `Production` by design, so `/scalar` and `/openapi/v1.json`
aren't mapped there — use the Postman collection below, or `curl`, against
the containerized Api instead.

## Running it locally

**Prerequisites**: Docker Desktop, and the .NET 10 SDK (only needed to run
the database migration once, and to run the test suites).

1. **Clone and start the stack**

   ```
   git clone <repo-url>
   cd AGMS
   docker compose up -d
   ```

   No `.env` file is required — every setting in `docker-compose.yml` has
   a working default. Copy `.env.example` to `.env` first only if you want
   different credentials.

2. **Apply the database migration** (once, against a fresh volume —
   nothing does this automatically on container start):

   ```
   dotnet ef database update --project GardenSystem.Infrastructure --startup-project GardenSystem.Api --connection "Host=localhost;Port=5433;Database=gardensystem;Username=gardensystem;Password=gardensystem"
   ```

3. **Try it**

   - API: `http://localhost:8080` (Scalar docs at `/scalar` only when run
     via `dotnet run`, not in this containerized stack — see above)
   - RabbitMQ management UI: `http://localhost:15673` (`gardensystem` /
     `gardensystem`) — AMQP on `5673`, MQTT on `1884`
   - Mailhog (catches verification emails): `http://localhost:8025`
   - Postgres: `localhost:5433` (`gardensystem` / `gardensystem`)

   A minimal end-to-end flow:

   ```
   curl -X POST http://localhost:8080/api/v1/auth/register -H "Content-Type: application/json" \
     -d '{"firstName":"A","lastName":"B","email":"you@example.com","password":"supersecret1"}'
   # read the 6-digit code from http://localhost:8025

   curl -X POST http://localhost:8080/api/v1/auth/verify-email -H "Content-Type: application/json" \
     -d '{"email":"you@example.com","code":"<code>"}'

   curl -X POST http://localhost:8080/api/v1/auth/login -H "Content-Type: application/json" \
     -d '{"email":"you@example.com","password":"supersecret1"}'
   # use the returned accessToken as a Bearer token from here on
   ```

   A ready-made Postman collection covering gardens/plants lives at
   `docs/postman/GardenSystem.StepI.Gardens.postman_collection.json`, and
   `docs/sql/seed-step-i-gardens.sql` seeds every plant species from the
   assignment directly via SQL if you'd rather not create them one by one.

## Running the tests

```
dotnet test GardenSystem.Domain.Tests GardenSystem.Application.Tests   # unit tests, no Docker needed
dotnet test GardenSystem.IntegrationTests                              # needs Docker running (Testcontainers)
```

| Layer | Tool | Covers |
|---|---|---|
| Domain | xUnit + FluentAssertions | Overcrowding math, decay/recovery rates, watering decisions, edge cases |
| Application | xUnit + Moq | Command/query handlers against mocked repositories, validators |
| Integration | xUnit + `WebApplicationFactory` + Testcontainers (real Postgres/RabbitMQ containers) | Full HTTP round trips: auth flow, garden/plant CRUD, overcrowding, account deletion cascade, a published telemetry message reacted to end-to-end by the consumer |

TDD (red → green → refactor, one commit per phase) was used specifically
for the three places where a wrong answer is a silent bug rather than a
crash: the overcrowding rule, the decay/recovery rate table, and the
watering decision logic. Everything else — CRUD plumbing, controllers, EF
configuration — was written test-after; applying strict TDD there would
have been process theater, not engineering judgment. See
`docs/ARCHITECTURE.md` §6 and §13 for the full reasoning and the exact
prompt sequence used to keep AI-assisted TDD honest (see also "AI usage"
below).

## What's implemented

Everything in the assignment's core requirement, plus all four bonus
areas:

- **Garden & plant CRUD**, scoped per user, with an overcrowding rule
  (`409 Conflict` with the exact shortfall in m²) enforced on create and
  update.
- **Auth**: registration with hashed passwords (BCrypt), email
  verification via a hashed 6-digit code, JWT access tokens + rotating
  opaque refresh tokens, per-row ownership checks, account deletion that
  cascades to a user's gardens and plants.
- **Simulated irrigation loop**: `SensorSimulator` polls its plant roster
  from the Api (authenticated via a shared `X-Api-Key`, separate from the
  user-facing JWT flow), publishes humidity telemetry over MQTT, and reacts
  to watering commands; `TelemetryConsumer` evaluates each reading, decides
  when to water, publishes commands over AMQP, and logs every irrigation
  event.
- **Reporting**: watering summary, watering frequency per plant, and
  plant additions/deletions, all backed by indexes chosen to match those
  exact queries.
- **Performance pass**: `AsNoTracking()` on every read-only query,
  pagination (capped at 100) on list endpoints, a 30-second `IMemoryCache`
  on the two single-item GETs with write-through invalidation, and
  batched `PlantState` writes in the telemetry consumer (a 500ms flush
  window instead of one DB round trip per message).

## Known limitations

Documented rather than silently fixed or hidden, in the spirit of the
assignment's "thoughtful decisions when facing constraints":

- **Re-registering a soft-deleted user's email returns a raw `500`**, not
  a clean `409` or success. The `Email` unique index isn't scoped to
  `DeletedAtUtc IS NULL`, so the (correct) application-level check finds
  no conflict, but the database-level constraint does. Fix would be a
  Postgres partial unique index; not applied since it wasn't part of any
  single step's explicit scope, but the failure mode is confirmed and
  reproducible.
- **No token revocation on account deletion.** A still-valid access token
  issued before deletion keeps validating at the JWT layer (though every
  resource it could reach is also gone). Refresh is blocked, since that
  lookup respects the same soft-delete query filter.
- **One active refresh-token session per user** — logging in on a second
  device invalidates the first. A deliberate, minimal-scope choice, not a
  bug.
- Also out of scope, and why, in `docs/ARCHITECTURE.md` §11: a managed
  IoT platform in place of self-hosted RabbitMQ, offline-device/
  unconfirmed-command handling, Kubernetes, and multi-tenancy.

## On AI usage

Used throughout, disclosed rather than hidden: for the initial
architecture write-up (including working through the push-vs-poll and
device-simulation design across several iterations), boilerplate
scaffolding (EF Core configurations, controller skeletons,
`docker-compose.yml`, the RabbitMQ client wiring), and first-pass unit
tests for the decay/recovery math — all reviewed and adjusted by hand,
particularly the telemetry state machine and the overcrowding edge cases,
since those are the parts that actually determine whether the solution is
correct.

For the three TDD steps specifically, an AI assistant's default of writing
test and implementation together in one pass would have defeated the
point — there'd be no red phase proving the test could actually fail. Each
of those steps was instead driven as three separate, explicit prompts
(write only the failing tests; now write the minimal implementation;
now refactor with tests staying green), each ending in its own commit, so
`git log` shows the cycle rather than asserting it happened. The exact
prompt sequence is in `docs/ARCHITECTURE.md` §13.

Every step in this repository's history followed the same pattern: one
step at a time, tests and implementation reviewed before committing, and
every deviation from what was literally asked flagged explicitly rather
than folded in silently — including the limitations listed above, all of
which were found and reported by the process itself, not after the fact.

## Repository layout

```
GardenSystem.Domain/              entities, enums, pure business rules — no dependencies
GardenSystem.Application/         MediatR commands/queries, DTOs, validators, repository interfaces
GardenSystem.Infrastructure/      EF Core, repositories, RabbitMQ client, email sender
GardenSystem.Api/                 controllers, auth, OpenAPI
GardenSystem.SensorSimulator/     worker service — plays the physical irrigation unit
GardenSystem.TelemetryConsumer/   worker service — reacts to telemetry, decides watering
GardenSystem.Domain.Tests/        unit tests
GardenSystem.Application.Tests/   unit tests (mocked repositories)
GardenSystem.IntegrationTests/    Testcontainers-backed integration tests
docs/ARCHITECTURE.md              full design spec and reasoning
docs/RABBITMQ.md                  RabbitMQ specifics: MQTT<->AMQP bridging, delivery guarantees
docs/PLAN.md                      the ordered step-by-step implementation log
```
