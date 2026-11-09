# BRIEFING — 2026-11-09T04:10:00Z

## Mission
Implement .NET 10 Minimal API solution for School Admission System with robust concurrency control, row-level locking, PostgreSQL EF Core migrations, derived expiry, background sweeper, demo data seeder, and RFC 9457 endpoints.

## 🔒 My Identity
- Archetype: Backend Engineer
- Roles: implementer, qa, specialist
- Working directory: /home/ashiq/work/school-admission-system/.agents/teamwork/backend
- Original parent: 885af30f-9a40-40c5-80f8-cb2ddba7d44b
- Milestone: M2 - Backend Implementation

## 🔒 Key Constraints
- Solution: backend/SchoolAdmission.sln
- Project: backend/src/SchoolAdmission.Api
- PostgreSQL schema: `classes` and `holds` with snake_case mapping, UUIDv7 PKs
- Partial unique index on `(student_id, class_id) WHERE status IN ('HELD', 'CONFIRMED')`
- Concurrency: `SELECT id, total_seats, clock_timestamp() AS now FROM classes WHERE id = @id FOR UPDATE`
- Strict invariants: held + confirmed <= total_seats
- Derived expiry at query time; background sweeper for status cleanup
- Exact endpoints, HTTP status codes, error codes (RFC 9457 ProblemDetails with `code` extension)
- No fake/mock implementations or shortcuts

## Current Parent
- Conversation ID: 885af30f-9a40-40c5-80f8-cb2ddba7d44b
- Updated: 2026-11-09T04:10:00Z

## Task Summary
- **What to build**: ASP.NET Core .NET 10 Minimal API backend with PostgreSQL persistence, EF Core 10 migrations, advisory-locked startup seeder, row-locked transactional business logic, and background expiry cleaner.
- **Success criteria**: Clean compilation with `dotnet build`, exact REST contract compliance, concurrency safety under high contention.
- **Interface contracts**: PROJECT.md §5 and PLAN.md §6
- **Code layout**: backend/SchoolAdmission.sln, backend/src/SchoolAdmission.Api/

## Key Decisions Made
- Used .NET 10 LTS Minimal APIs with route groups for `/classes`, `/holds`, `/me`, `/health`.
- Explicit EF Core entity mappings with snake_case naming conventions.
- UUIDv7 for time-ordered primary keys (`Guid.CreateVersion7()`).
- Database clock timestamp `clock_timestamp()` inside lock transaction `SELECT ... FOR UPDATE` to prevent clock-skew races.
- Pre-lock cleanup of stale holds for student and partial unique index on active holds.
- RFC 9457 ProblemDetails responses with custom `code` field and sanitized exception handling.
- BackgroundService with PeriodicTimer for continuous expiry sweeping.

## Artifact Index
- `.agents/teamwork/backend/DISPATCH.md` — Backend dispatch instructions & resumption notices
- `.agents/teamwork/backend/BRIEFING.md` — Situational awareness and working memory
- `.agents/teamwork/backend/progress.md` — Liveness heartbeat and milestone tracking
- `.agents/teamwork/backend/handoff.md` — Final handoff report
- `backend/SchoolAdmission.sln` — Complete solution file
- `backend/src/SchoolAdmission.Api/` — API implementation
- `backend/tests/SchoolAdmission.Tests/` — Unit and validation tests

## Change Tracker
- **Files modified**:
  - `backend/SchoolAdmission.sln`
  - `backend/src/SchoolAdmission.Api/SchoolAdmission.Api.csproj`
  - `backend/src/SchoolAdmission.Api/Program.cs`
  - `backend/src/SchoolAdmission.Api/appsettings.json`
  - `backend/src/SchoolAdmission.Api/appsettings.Development.json`
  - `backend/src/SchoolAdmission.Api/Domain/Entities/Class.cs`
  - `backend/src/SchoolAdmission.Api/Domain/Entities/Hold.cs`
  - `backend/src/SchoolAdmission.Api/Domain/Enums/HoldStatus.cs`
  - `backend/src/SchoolAdmission.Api/Domain/Exceptions/ProblemException.cs`
  - `backend/src/SchoolAdmission.Api/Infrastructure/Persistence/AppDbContext.cs`
  - `backend/src/SchoolAdmission.Api/Infrastructure/Persistence/Configurations/ClassConfiguration.cs`
  - `backend/src/SchoolAdmission.Api/Infrastructure/Persistence/Configurations/HoldConfiguration.cs`
  - `backend/src/SchoolAdmission.Api/Infrastructure/Persistence/Migrations/20261109000001_InitialCreate.cs`
  - `backend/src/SchoolAdmission.Api/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
  - `backend/src/SchoolAdmission.Api/Infrastructure/Database/DatabaseMigrator.cs`
  - `backend/src/SchoolAdmission.Api/Infrastructure/Database/DemoDataSeeder.cs`
  - `backend/src/SchoolAdmission.Api/Infrastructure/Background/ExpirySweeper.cs`
  - `backend/src/SchoolAdmission.Api/Application/Services/IClassService.cs`
  - `backend/src/SchoolAdmission.Api/Application/Services/ClassService.cs`
  - `backend/src/SchoolAdmission.Api/Application/Services/IHoldService.cs`
  - `backend/src/SchoolAdmission.Api/Application/Services/HoldService.cs`
  - `backend/src/SchoolAdmission.Api/Contracts/Requests/CreateClassRequest.cs`
  - `backend/src/SchoolAdmission.Api/Contracts/Responses/ClassResponses.cs`
  - `backend/src/SchoolAdmission.Api/Contracts/Responses/HoldResponses.cs`
  - `backend/src/SchoolAdmission.Api/Validation/CreateClassRequestValidator.cs`
  - `backend/src/SchoolAdmission.Api/Validation/StudentIdValidation.cs`
  - `backend/src/SchoolAdmission.Api/Endpoints/ClassEndpoints.cs`
  - `backend/src/SchoolAdmission.Api/Endpoints/HoldEndpoints.cs`
  - `backend/src/SchoolAdmission.Api/Endpoints/StudentEndpoints.cs`
  - `backend/src/SchoolAdmission.Api/Endpoints/HealthEndpoints.cs`
  - `backend/src/SchoolAdmission.Api/Middleware/ProblemDetailsExceptionHandler.cs`
  - `backend/tests/SchoolAdmission.Tests/SchoolAdmission.Tests.csproj`
  - `backend/tests/SchoolAdmission.Tests/ValidationTests.cs`
- **Build status**: PASS (0 warnings, 0 errors)
- **Pending issues**: None

## Quality Status
- **Build/test result**: PASS (19 tests passed, 0 failed)
- **Lint status**: Clean
- **Tests added/modified**: 19 unit & contract validation tests covering input boundaries, student ID extraction, and constraint checks.
