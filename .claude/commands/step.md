---
description: Execute one step from docs/PLAN.md
argument-hint: <step letter, e.g. N>
---

Execute step $ARGUMENTS from `docs/PLAN.md`.

Before writing any code:

1. Read the entry for step $ARGUMENTS in `docs/PLAN.md`, including its
   acceptance criterion.
2. Read the `docs/ARCHITECTURE.md` sections it references.
3. Read the actual current state of the code you're about to build on —
   the repository is ground truth, not the docs. If they diverge, list the
   differences and stop for confirmation rather than reconciling silently.
4. State in one or two sentences what you're about to do and what is
   explicitly out of scope for this step.

Then implement it, following the standing rules in `CLAUDE.md`.

## Verification — you run all of it, I only read code

Run every check yourself. Do not ask me to run anything, and do not
describe what a check "would" show — run it and paste what it actually
printed.

1. `dotnet build` — paste the result line.
2. `dotnet test` — paste the raw summary line (passed/failed/skipped
   counts), not a paraphrase. If anything failed, paste the failing test
   names and assertion messages in full.
3. The step's acceptance check from `docs/PLAN.md` (curl, psql,
   `docker compose logs`, whatever it specifies) — run it, paste the raw
   output. If it needs containers, start them, verify, and leave them in a
   clean state.

## Then report, in this order

1. **Test files first** — list every test you added or changed, and for
   each, one line on what behaviour it actually asserts. I review tests
   before implementation, so lead with these.
2. **Implementation** — what changed and why, flagging anything you did
   that the step didn't explicitly ask for.
3. **Anything you're unsure about** — a guess you made, an ambiguity in
   the plan, a shortcut you took. Say it plainly; don't leave it for me to
   find in the diff.
4. `git status` and `git diff`.

Then stop. Do not commit — I review and commit myself.

## TDD phases

If the step is a **red** phase: write only tests, create nothing that makes
them compile, and show the failure output with explicit confirmation that
it fails for the intended reason (missing implementation) and not because
of a typo, a missing using, or a broken pre-existing test. A failing
`dotnet test` is the correct outcome here — do not "fix" it.

If the step is a **green** phase: write the minimum needed to pass, nothing
the tests don't require, and never edit the tests. If a test seems wrong,
stop and tell me instead of changing it.
