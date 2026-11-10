# Technology Stack — School Admission System

A reference for *what* we use and *why* each piece is here. Version numbers are the ones
pinned in `backend/src/SchoolAdmission.Api/SchoolAdmission.Api.csproj`,
`backend/tests/SchoolAdmission.Tests/SchoolAdmission.Tests.csproj` and
`frontend/package.json`.

---

## 1. The one requirement everything serves

> **Never oversell a class seat, no matter how many students click "Apply" at the same second.**

Almost every decision below traces back to that. A seat is a finite, contended resource, so
the stack needs real ACID transactions, real row-level locking, and tests that reproduce
concurrency against a real database — not a mock.

---

## 2. Backend

| Technology | Version | Why |
|---|---|---|
| **.NET / ASP.NET Core** | `net10.0` | Current LTS with the fastest minimal-API startup and native `System.Text.Json` source generators. Minimal APIs let the five endpoint groups live in a few lines each (`Endpoints/*.cs`) instead of MVC controller + model + DI ceremony. |
| **C# with nullable reference types** | `Nullable=enable` | The compiler proves null-safety across the service and entity layers instead of leaving it to runtime `NullReferenceException`s. |
| **Entity Framework Core** | `10.0.4` | Migrations, a strongly-typed `DbContext`, and `ExecuteSqlRawAsync` for the atomic seat-count SQL that runs inside the hold transaction. |
| **Npgsql** | `9.0.4` | The official PostgreSQL ADO.NET provider. Needed for `SELECT … FOR UPDATE`, `COUNT(*) FILTER (WHERE …)`, and `pg_advisory_lock`, none of which EF expresses cleanly. |
| **PostgreSQL 18** | `18-alpine` | Real transactions, row-level locks, and advisory locks — the only way to make the seat count correct under concurrency. The Alpine variant keeps the image small. |
| **FluentValidation** | `12.0.0` | Declarative request validation that runs before the handler opens a transaction, so malformed payloads never reach the database. |
| **Swashbuckle / OpenAPI** | `9.0.5` | Auto-generated `/swagger` — the live contract for the frontend and for reviewers, without maintaining a separate spec file. |
| **ProblemDetails (RFC 7807)** | built-in | One standard error envelope (`type` / `title` / `status` / `detail` + `code`) so the frontend renders errors generically instead of per-endpoint special cases. |
| **System.Text.Json + camelCase** | built-in | Fast, reflection-light serialization; camelCase makes the JSON match the TypeScript interfaces on the frontend one-for-one. |
| **EFCore.NamingConventions** | `1.0.0` | `UseSnakeCaseNamingConvention()` maps `TotalSeats` → `total_seats`, keeping the C# model and the hand-written SQL in `HoldService` on the same naming. |
| **Options pattern + `ValidateOnStart`** | built-in | `HoldOptions` binds `HOLD_DURATION_SECONDS` once at startup and validates it there, so a bad value is a **boot-time** failure with a readable message instead of a per-request crash. |
| **BackgroundService + PeriodicTimer** | built-in | `ExpirySweeper` flips `HELD` → `EXPIRED` on an interval (`EXPIRY_SWEEP_INTERVAL_SECONDS`, default 5s) so abandoned holds release their seats without an external cron. |
| **Postgres advisory lock** | built-in | `DatabaseMigrator` takes `pg_advisory_lock` before migrating, so N containers starting at once cannot race on schema changes. |
| **Guid v7** (`Guid.CreateVersion7()`) | .NET 9+ | Time-ordered UUIDs. UUIDv7 inserts roughly sequentially, so the primary key index does not fragment the way random v4 UUIDs do. |
| **xUnit** | `2.9.3` | The de-facto .NET test framework; pairs with `WebApplicationFactory` for true integration tests. |
| **`Microsoft.AspNetCore.Mvc.Testing`** | `10.0.14` | Boots the *real* `Program` in-memory, so tests exercise the actual middleware, DI, options validation and endpoints — not a re-implementation. |
| **`Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio`** | `17.14.1` | Standard `dotnet test` integration, so CI needs no extra tooling. |

### Backend architecture

```
Domain/          Entities, enums, exceptions            (no infrastructure dependencies)
Application/     Services (HoldService, ClassService)    (business rules + transactions)
                 Options (HoldOptions)                   (validated configuration)
Infrastructure/  DbContext, migrations, migrator,        (EF Core, seeding, background jobs)
                 seeder, ExpirySweeper
Endpoints/       Minimal-API route groups                (HTTP shape only)
Middleware/       ProblemDetailsExceptionHandler         (cross-cutting error translation)
Validation/      FluentValidation validators
Contracts/       Request/response DTOs                  (the API surface)
```

The layering is deliberate: `Application/Services` owns the transactional business rules,
`Endpoints` only maps HTTP to a service call, and `Infrastructure` owns everything that
knows PostgreSQL exists.

---

## 3. Frontend

| Technology | Version | Why |
|---|---|---|
| **React 19** | `^19.0.0` | Component model that maps cleanly to the list / detail / modal UI, with concurrent rendering for smooth countdown updates. |
| **TypeScript** | `^5.7.2` | The API contract is checked at compile time. If the backend renames a field, `tsc -b` fails the build instead of the page breaking at runtime. |
| **Vite** | `^6.0.5` | Sub-second HMR and a fast production build; the Compose `develop.watch` action then syncs changed files straight into the running container. |
| **Tailwind CSS** | `^3.4.17` | Utility-first styling keeps the design system consistent without a CSS-in-JS runtime, and avoids shipping a large component library. |
| **@tanstack/react-query** | `^5.62.8` | Class lists, seat counts and hold statuses are *server* state. React Query owns caching, background refetching, and the loading/error state machine, so components stay declarative. |
| **Zustand** | `^5.0.2` | A tiny store for the few genuinely *client* concerns (selected student id, language, server clock). Lighter than Context and avoids re-render storms. |
| **react-i18next / i18next** | `^15.4.0` | Bengali + English (`src/i18n/bn.json`, `en.json`). All copy lives outside components, so the language can be switched at runtime — essential for the target audience. |
| **axios** | `^1.7.9` | Interceptors give one place to inject the base URL and to normalize ProblemDetails errors into a consistent shape for the UI. |
| **react-router-dom** | `^7.1.1` | Client-side routing between the class list, admin class creation, and "my applications" — no full page reloads, no server round-trip for a route change. |
| **lucide-react** | `^0.469.0` | Tree-shakeable SVG icons: no icon font, no extra HTTP request, no PNG assets. |
| **clsx + tailwind-merge** | `^2.1.1` / `^2.6.0` | Conditional class composition where later Tailwind utilities correctly override earlier ones — needed for component variants (e.g. a badge that changes colour). |
| **@fontsource-variable/noto-sans-bengali** | `^5.1.0` | Bengali is a core requirement, and a self-hosted variable font means the UI renders correctly with no Google Fonts CDN dependency (works offline, no privacy leak). |

### Frontend structure

```
api/       axios client, typed endpoints, React Query hooks
components/  reusable UI (Modal, Badge, Header, Toast, skeletons)
pages/     route-level screens
stores/    Zustand stores (auth, student, language, clock)
i18n/      bn.json / en.json + i18next setup
utils/     countdown, formatters, error mapping
```

---

## 4. Infrastructure & DevOps

| Technology | Why |
|---|---|
| **Docker (multi-stage build)** | The backend compiles in the `sdk:10.0` stage and ships only the published output on `aspnet:10.0-alpine` — no SDK in the production image, small attack surface. |
| **Docker Compose** | One command brings up db + backend + frontend. `depends_on: { condition: service_healthy }` guarantees the API only starts once Postgres accepts connections. |
| **Compose `develop.watch`** | Rebuilds the backend on C# changes and syncs frontend sources into the container, so the edit–reload loop needs no manual restarts. |
| **Health endpoints** (`/health/live`, `/health/ready`) | Compose gates the frontend on the backend being *ready* (database reachable), not merely *running* — no crash-looping on a slow database start. |
| **`.env` + `${VAR:-default}`** | Layered configuration: `.env` → compose default → `appsettings.json` → code default. Ops can retune `HOLD_DURATION_SECONDS` with a container recreate, no rebuild. |
| **Non-root container user** (`USER $APP_UID`) | The runtime stage runs as UID 1654 — least privilege inside the container. |
| **ICU libraries + `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false`** | Alpine ships without full ICU; without this, culture-aware formatting and Bengali text handling break. |
| **`wget` in the runtime image** | Backs the Compose healthcheck without adding curl as a second HTTP client. |

### Configuration precedence (single source of truth per key)

For `HOLD_DURATION_SECONDS`, resolved by `HoldOptions`:

```
shell export
  → .env
    → docker-compose.yml   ${HOLD_DURATION_SECONDS:-600}
      → container env var
        → ASP.NET Core config: env var  >  appsettings.json  >  appsettings.{Environment}.json
          → HoldOptions.DurationSeconds   (validated > 0 at startup)
```

The environment variable name is stable across environments and is what compose, `.env` and
the test fixtures all set. `appsettings.json` keeps the local-run default. A non-numeric or
non-positive value now **fails the process at startup** with an explicit message instead of
silently falling back to 600 or throwing on a live request.

---

## 5. Testing

| Aspect | Approach |
|---|---|
| **Framework** | xUnit + `WebApplicationFactory<Program>` — boots the real API in-process. |
| **Database** | The real PostgreSQL from Compose, not an in-memory substitute. In-memory providers do not model `FOR UPDATE`, `FILTER (WHERE …)` or unique-violation races, which is exactly what this app is about. |
| **Concurrency** | `ConcurrencyOversellingTests` — many parallel applications against a 1-seat class; exactly one must win. |
| **Idempotency** | `ConcurrencyIdempotentConfirmTests` — a repeated confirm must not consume a second seat. |
| **Single active application** | `ConcurrencySingleActiveApplicationTests` — one student cannot hold two live holds in one class. |
| **Expiry** | `HoldExpiryTests` — a 2-second hold (`ExpiryTestApiFactory` overrides the config) expires on its own, the seat returns to the pool, and confirming afterwards returns `409 HOLD_EXPIRED`. |
| **Validation** | `ValidationTests` — malformed payloads are rejected before any transaction opens. |
| **Contract** | `ApiContractTests` — status codes and ProblemDetails payloads match the documented contract. |
| **Runner** | `docker compose --profile test run --rm backend-tests` — the same image and database as production, so the environment under test is the environment that ships. |

---

## 6. Deliberate non-choices

| Not used | Why |
|---|---|
| **Redis / Memcached** | The app is a single service with a single database; the seat state *must* live transactionally in Postgres. A cache would be a second source of truth for the most contested resource in the system. |
| **Authentication / JWT** | Out of scope for this assignment — identity is carried by the `X-Student-Id` header. The header is validated (exactly 17 digits) and the hold ownership check is enforced server-side, so hold duration and expiry are never client-controlled. |
| **MediatR / CQRS** | Four endpoints do not justify the ceremony, the extra mapping layer, or the debugging overhead. Plain service classes are easier to read and to test. |
| **AutoMapper** | Small, explicit DTOs with records are clearer than reflection-based mapping, and the compiler catches mismatches. |
| **MongoDB / other NoSQL** | The core problem is a contended, transactional counter. A document store cannot express `SELECT … FOR UPDATE` on a shared seat pool. |
| **CSS-in-JS** | Tailwind covers the design needs with zero runtime cost. |
| **A component library** (MUI, Ant) | A dependency-light custom UI keeps the bundle small and the look consistent with the brief. |

---

## 7. Quick reference

```bash
# Start everything
docker compose up -d

# Live-reload development
docker compose watch

# Run the test suite against the real database
# (the test image is a separate Docker stage — rebuild it after backend changes)
docker compose --profile test build backend-tests
docker compose --profile test run --rm backend-tests

# Change the hold duration (then recreate the container — .env is read by Compose, not the app)
echo "HOLD_DURATION_SECONDS=60" >> .env
docker compose up -d --force-recreate backend

# Inspect the effective config inside the container
docker exec school_admission_backend printenv HOLD_DURATION_SECONDS

# API docs
open http://localhost:8080/swagger
```
