# Backend Engineer Dispatch

## 2026-11-09T03:53:21Z
You are the Backend Engineer for the School Admission System project.
Your working directory is: /home/ashiq/work/school-admission-system/.agents/teamwork/backend
Read the following files before starting:
- /home/ashiq/work/school-admission-system/.agents/teamwork/ORIGINAL_REQUEST.md
- /home/ashiq/work/school-admission-system/PLAN.md (especially §2, §4, §5, §6, §7)
- /home/ashiq/work/school-admission-system/.agents/teamwork/PROJECT.md
- /home/ashiq/work/school-admission-system/.agents/teamwork/backend/DISPATCH.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Your exclusive responsibilities:
1. Initialize .NET 10 solution `backend/SchoolAdmission.sln` and ASP.NET Core Minimal API project `backend/src/SchoolAdmission.Api`:
   - Packages: Npgsql.EntityFrameworkCore.PostgreSQL, EF Core 10, FluentValidation (or endpoint filters), Swagger/OpenAPI
2. Domain & Infrastructure:
   - PostgreSQL schema with tables `classes` and `holds` (snake_case column mapping, UUID PKs)
   - Partial unique index on `(student_id, class_id) WHERE status IN ('HELD', 'CONFIRMED')`
   - Startup automatic migration runner with Postgres advisory lock
   - Seeder: 12 classes with realistic seat distributions per PLAN.md §10.4 when `classes` table is empty and `SEED_DEMO_DATA=true`
3. Core Concurrency & Business Rules Engine:
   - Apply, confirm, cancel transactions use row-level locking: `SELECT id, total_seats, clock_timestamp() AS now FROM classes WHERE id = @id FOR UPDATE`
   - Database clock `clock_timestamp()` read inside the lock transaction
   - Invariants strictly enforced: `held + confirmed <= totalSeats`
   - Single active application per student per class
   - Idempotent confirm (confirming multiple times increments confirmedSeats once)
   - Derived expiry (seat available as soon as clock passes `expires_at` without waiting for sweeper)
   - Background periodic sweeper (`BackgroundService` with `PeriodicTimer`) to tidy status to `EXPIRED`
4. API Endpoints (exact matching routes, casing, headers, and status codes):
   - `POST /classes` (201 `{id, name, totalSeats}`, 400)
   - `GET /classes` (200 array with `id, name, totalSeats, heldSeats, confirmedSeats, availableSeats`) via single aggregation query
   - `GET /classes/{classId}` (200, 404 `CLASS_NOT_FOUND`)
   - `POST /classes/{classId}/holds` with `X-Student-Id` header (201 `{holdId, classId, studentId, status, expiresAt}`, 400, 404, 409 `SEATS_FULL` / `ALREADY_HELD` / `ALREADY_CONFIRMED`)
   - `GET /holds/{holdId}` (200 `{holdId, classId, status, expiresAt}`, 404 `HOLD_NOT_FOUND`)
   - `POST /holds/{holdId}/confirm` with `X-Student-Id` header (200 `{holdId, status: "CONFIRMED"}`, 400, 403 `NOT_HOLD_OWNER`, 404, 409 `HOLD_EXPIRED` / `HOLD_CANCELLED`)
   - `DELETE /holds/{holdId}` with `X-Student-Id` header (204, 400, 403, 404, 409 `HOLD_ALREADY_CONFIRMED`)
   - `GET /me/holds` with `X-Student-Id` header (200 array with `holdId, classId, className, status, expiresAt`, 400)
   - Health endpoints: `GET /health/live`, `GET /health/ready` (verifies DB)
   - CORS enabled for `http://localhost:3000`
   - RFC 9457 ProblemDetails with stable `code` property
5. Periodically update your `progress.md` in your working directory.
6. Verify with `dotnet build backend/SchoolAdmission.sln`.
7. Write `handoff.md` with build verification results and message parent.

## 2026-11-09T04:04:24Z
**Context**: Post-restart resumption of Backend implementation
**Content**: A brief system restart occurred and all subagents are being resumed. Please continue implementing the .NET 10 Minimal API solution, EF Core models & migrations, concurrency engine (`SELECT ... FOR UPDATE` with DB clock), seed data, derived expiry, and all 8 REST endpoints. Verify with `dotnet build` and submit your `handoff.md` when done.
**Action**: Resume work from your current progress point and report when complete.
