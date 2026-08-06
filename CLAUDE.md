# GardenSystem — working rules

Backend case for In The Pocket. Evaluated on thought process and incremental
work, not on speed. `docs/ARCHITECTURE.md` is the design spec.
`docs/PLAN.md` is the ordered step list.

## Stack

.NET 10 (current LTS), ASP.NET Core Web API with controllers (not minimal
APIs), EF Core 10 + Npgsql, MediatR, FluentValidation, xUnit +
FluentAssertions + Moq, Testcontainers for integration tests, RabbitMQ,
Docker Compose. Everything runs locally — nothing is deployed.

## Standing rules — apply to every step, without being reminded

1. **One step at a time.** Do only the step you were asked for. Do not
   start the next one, do not "while I'm here" adjacent files.
2. **Never commit.** Finish by showing `git status` and `git diff`, then
   stop and wait for review. The human commits.
3. **Never edit a test to make it pass.** If a test looks wrong or
   ambiguous, stop and say so. Changing the test to fit the implementation
   defeats the point.
4. **No speculative code.** No packages, projects, interfaces, methods, or
   config that the current step doesn't actually use. Projects get created
   in the step that first needs them.
5. **Ground truth is the repository, not the docs.** Read the actual
   current code before building on it. If it diverges from
   `docs/ARCHITECTURE.md`, list the differences and stop — don't silently
   reconcile.
6. **Run the tests.** End every step with `dotnet test` and report the
   result. If the step has a manual verification (curl, psql,
   docker-compose), run that too and show the output.
7. **Report deviations.** If you had to do something the step didn't
   mention, say so explicitly in your summary. Don't bury it in the diff.

## Layering (per ARCHITECTURE.md section 3.1)

- `Domain` — no dependencies on anything. Plain POCOs, enums, pure business
  logic. No EF Core attributes, no data annotations.
- `Application` — MediatR commands/queries + handlers, DTOs, FluentValidation
  validators, repository interfaces. Talks to repositories, never to
  DbContext directly.
- `Infrastructure` — EF Core, DbContext, repository implementations, message
  broker clients, email sender.
- `Api` — thin controllers dispatching to MediatR. No business logic, no
  manual ModelState checks (validation runs as a pipeline behavior).
- `SensorSimulator` / `TelemetryConsumer` — worker services, own containers.

## Conventions

- Conventional Commits for messages: `feat:`, `test:`, `fix:`, `refactor:`,
  `chore:`. Scoped to one step.
- `decimal` for surface areas and humidity levels — never `double`/`float`.
- Soft delete via `DeletedAtUtc` + EF Core global query filters.
- `///` XML doc comments on controllers and DTOs; OpenAPI generates from
  code, never hand-maintained.
- Errors surface as RFC 7807 ProblemDetails via the global middleware.

## Deliberately deferred (do not add early)

- `IrrigationEvent` entity — added in the reporting step.
- `User.PasswordHash` / `User.EmailVerified` — added in the auth step.
- Kubernetes, live cloud deployment, rate limiting, roles beyond owner.
