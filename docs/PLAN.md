# Implementation plan — ordered steps

Each step is one prompt, one review, one commit. Read `CLAUDE.md` for the
standing rules that apply to all of them. `docs/ARCHITECTURE.md` is the
design spec — section references below point into it.

Mark steps done by changing `[ ]` to `[x]` after committing.

---

## Done

- [x] **A** — git init, .gitignore, solution, empty `GardenSystem.Api`
- [x] **B** — `GET /health` endpoint via controller
- [x] **C** — multi-stage Dockerfile for Api, `.dockerignore`
- [x] **D** — `GardenSystem.Domain` + entities + smoke tests
- [x] **E** — `Infrastructure` + `GardenDbContext` + docker-compose with Postgres
- [x] **F** — EF Core migration applied, seeded test user verified
- [x] **G** — repository interfaces + implementations + Testcontainers integration test
- [x] **H** — MediatR, FluentValidation pipeline, ProblemDetails middleware, OpenAPI generation
- [x] **I** — Garden CRUD + list-by-user
- [x] **J** — Plant CRUD + list-by-garden (no overcrowding rule yet)
- [x] **K** — [TDD red] failing tests for `Garden.CanFitPlant`
- [x] **L** — [TDD green] minimal `CanFitPlant` implementation
- [ ] **M** — [TDD refactor] + wire into plant create/update, 409 on overflow
  *(implemented and passing in the working tree, uncommitted — see report)*

*(Core requirement of the assignment is complete after M. Everything below
is bonus work — sections 7-9 of the architecture doc.)*

---

## Messaging & irrigation (ARCHITECTURE.md section 8)

- [ ] **N** — RabbitMQ added to docker-compose with the MQTT plugin enabled
  (AMQP 5672 + MQTT 1883), healthcheck, credentials in `.env.example`.
  Acceptance: the management UI loads and both listeners are up.
- [ ] **O** — [TDD red] failing tests for `IrrigationRates.GetDecayRate` /
  `GetRecoveryRate` for all three `PlantType` values (section 8 rate table).
  No implementation.
- [ ] **P** — [TDD green] minimal `IrrigationRates` implementation.
- [ ] **Q** — [TDD refactor] clean up (e.g. dictionary lookup over if/else),
  tests stay green.
- [ ] **R** — `GardenSystem.SensorSimulator` worker + container: owns the
  decay/recovery state machine (section 8.3), publishes telemetry to
  `sensors/{plantId}/telemetry` over MQTT via MQTTnet, subscribes to
  `irrigation/{plantId}/commands`, polls the Api for its plant roster
  (section 8.4 — low-frequency reference data only, never for watering
  decisions). References `Domain` directly so it shares `IrrigationRates`.
  Acceptance: `docker-compose up` and watch humidity readings tick down in
  the logs.
- [ ] **S** — [TDD red] failing tests for
  `TelemetryEvaluator.ShouldStartWatering`: below threshold + idle → start;
  below threshold + already irrigating → no duplicate command; at or above
  threshold → no-op.
- [ ] **T** — [TDD green] minimal `TelemetryEvaluator` implementation.
- [ ] **U** — [TDD refactor], tests stay green.
- [ ] **V** — `GardenSystem.TelemetryConsumer` worker + own container
  (deliberately not inside the Api — section 3.1): consumes telemetry over
  AMQP, evaluates, publishes watering commands, updates `PlantState`.
  Integration test with Testcontainers RabbitMQ + Postgres proving the full
  round trip. Acceptance: a plant crosses below its ideal humidity and the
  logs show command sent → simulator waters → humidity rises over two ticks.

## Reporting (ARCHITECTURE.md section 8.5)

- [ ] **W** — Add the `IrrigationEvent` entity + migration + index on
  `(PlantId, StartTimeUtc)`. Wire `TelemetryConsumer` to open an event when
  it sends a command and close it (`EndTimeUtc`, `HumidityAfter`) when
  telemetry confirms the recovery completed.
- [ ] **X** — Reporting endpoints: `/reports/watering-summary`,
  `/reports/watering-frequency/{plantId}`, `/reports/plant-changes`.
  Acceptance: run the system for a few minutes, then confirm the numbers
  the reports return match what the logs show actually happened.

## Auth (ARCHITECTURE.md section 7)

- [ ] **Y** — Add `PasswordHash` + `EmailVerified` to `User`, migration,
  BCrypt hashing, `POST /auth/register`. Acceptance: register a user via
  the endpoint, confirm the row exists with a hashed (not plaintext)
  password and `EmailVerified = false`.
- [ ] **Z** — Email verification: 6-digit code stored hashed with expiry,
  `IEmailSender` abstraction, Mailhog added to docker-compose,
  `POST /auth/verify-email`. Acceptance: register, read the code from the
  Mailhog UI, verify, confirm `EmailVerified = true`.
- [ ] **AA** — JWT issuance on `POST /auth/login` (15 min access token) +
  opaque refresh token (7 days, hashed in DB, rotated on use) +
  `POST /auth/refresh`. Add `[Authorize]` to Garden/Plant controllers and
  swap `ICurrentUserProvider` to read the JWT `sub` claim instead of the
  seeded Guid. Add per-row ownership checks in handlers. Acceptance:
  requests without a token return 401; a user cannot read another user's
  gardens (403/404).
- [ ] **AB** — `DELETE /auth/me`, soft-deleting the user and cascading to
  their gardens and plants.

## Polish (ARCHITECTURE.md sections 9, 12)

- [ ] **AC** — Performance pass: `AsNoTracking()` on reads, pagination with
  a capped page size on all list endpoints, `IMemoryCache` on single-item
  gets with write invalidation, batched `PlantState` writes in the
  consumer.
- [ ] **AD** — README finalization (architecture choices, assumptions, AI
  usage per section 12, how to run) + full `docker-compose up` smoke test
  from a clean clone with an empty volume.
