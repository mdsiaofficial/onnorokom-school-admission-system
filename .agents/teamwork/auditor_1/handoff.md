# Forensic Audit Handoff Report

## 1. Observation

### 1.1 Anti-Cheating & Source Code Verification
- Searched codebase for prohibited patterns (facades, mocks, dummies, stubs, hardcoded returns):
  ```bash
  $ grep -rnEi --exclude-dir={bin,obj,node_modules,.git} "NotImplemented|TODO|FIXME|return Task\.FromResult|mock|fake|dummy" backend/src/ frontend/src/ backend/tests/
  # Exit code 1 (zero matches found)
  ```
- Searched for pre-populated result artifacts, mock outputs, or fabricated test logs:
  ```bash
  $ find . -maxdepth 4 -name '*.log' -o -name '*result*' -o -name '*output*'
  # Result: Only ./frontend/node_modules/postcss-js/process-result.js was found. Zero pre-populated test artifacts.
  ```

### 1.2 Concurrency Engine & Database Clock Inspection
- Inspected `backend/src/SchoolAdmission.Api/Application/Services/HoldService.cs`:
  - Lines 43–55:
    ```csharp
    lockCmd.CommandText = "SELECT id, total_seats, clock_timestamp() AS now FROM classes WHERE id = @id FOR UPDATE;";
    lockCmd.Parameters.Add(new NpgsqlParameter("id", classId));
    await using var reader = await lockCmd.ExecuteReaderAsync(cancellationToken);
    ...
    totalSeats = reader.GetInt32(1);
    now = reader.GetFieldValue<DateTimeOffset>(2);
    ```
    Verified: Pessimistic row-level locking on the `classes` table is genuinely executed within an active transaction. The PostgreSQL server engine timestamp (`clock_timestamp()`) is captured and used for all subsequent temporal calculations.
  - Lines 58–71: Stale `HELD` rows for the applicant student in the class are cleaned up (`expires_at <= @now`) inside the locked transaction prior to active capacity checks.
  - Lines 105–121: Capacity calculation strictly counts `COUNT(*) FILTER (WHERE status = 'HELD' AND expires_at > @now) + COUNT(*) FILTER (WHERE status = 'CONFIRMED')` against `total_seats`.
  - Lines 252–256: Idempotent confirmation returns `200 OK CONFIRMED` without duplicate seat decrements.
  - Lines 368–373 & 390–401: Cancellation frees held seats immediately, returning `204 NoContent`.

### 1.3 Database Schema & Invariants
- Inspected `backend/src/SchoolAdmission.Api/Infrastructure/Persistence/Configurations/HoldConfiguration.cs`:
  - Lines 14–15: Check constraints `ck_holds_student_id_17_digits` (`student_id ~ '^[0-9]{17}$'`) and `ck_holds_status_valid` (`status IN ('HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED')`).
  - Lines 64–67: Partial unique index `ux_holds_active_student_class` on `(student_id, class_id)` with filter `status IN ('HELD', 'CONFIRMED')`.
- Inspected `backend/src/SchoolAdmission.Api/Infrastructure/Persistence/Configurations/ClassConfiguration.cs` line 13: Check constraint `ck_classes_total_seats_positive` (`total_seats > 0`).
- Inspected `backend/src/SchoolAdmission.Api/Infrastructure/Persistence/Migrations/20261109000001_InitialCreate.cs`: All constraints, foreign keys, and indexes are generated in SQL migration.
- Inspected `docker-compose.yml` line 12: Persistent volume `db-data:/var/lib/postgresql` correctly targets PostgreSQL 18 volume directory.

### 1.4 API Contract & RFC 9457 ProblemDetails
- Inspected all 8 endpoints in `ClassEndpoints.cs`, `HoldEndpoints.cs`, and `StudentEndpoints.cs`:
  1. `POST /classes` (201 `{id, name, totalSeats}`, 400)
  2. `GET /classes` (200 array of `{id, name, totalSeats, heldSeats, confirmedSeats, availableSeats}`)
  3. `GET /classes/{classId}` (200 summary, 404 `CLASS_NOT_FOUND`)
  4. `POST /classes/{classId}/holds` (201 `{holdId, classId, studentId, status, expiresAt}`, 400, 404, 409 `SEATS_FULL`, `ALREADY_HELD`, `ALREADY_CONFIRMED`)
  5. `GET /holds/{holdId}` (200 `{holdId, classId, status, expiresAt}`, 404 `HOLD_NOT_FOUND`)
  6. `POST /holds/{holdId}/confirm` (200 `{holdId, status: "CONFIRMED"}`, 400, 403 `NOT_HOLD_OWNER`, 404, 409 `HOLD_EXPIRED`, `HOLD_CANCELLED`)
  7. `DELETE /holds/{holdId}` (204, 400, 403, 404, 409 `HOLD_ALREADY_CONFIRMED`)
  8. `GET /me/holds` (200 array of `{holdId, classId, className, status, expiresAt}`, 400)
- Inspected `Program.cs` line 22: `options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase`.
- Inspected `ProblemDetailsExceptionHandler.cs`: Compliant RFC 9457 payload with `type`, `title`, `status`, `code`, and `detail`, suppressing stack traces.

### 1.5 Frontend Implementation
- Inspected `frontend/package.json`: React `^19.0.0`, React DOM `^19.0.0`, React Router `^7.1.1`, Zustand `^5.0.2`, TanStack Query `^5.62.8`.
- Inspected `frontend/src/components/StudentIdBar.tsx` and `studentStore.ts`: 17-digit numeric restriction, live counter (`12/17`), and `localStorage` persistence.
- Inspected `frontend/src/components/HoldItem.tsx` and `countdown.ts`: 1-second live countdown ticker with server skew compensation, flipping to `EXPIRED` locally at 0 without page reload and triggering TanStack Query invalidation.
- Inspected `frontend/src/i18n/bn.json` & `en.json`, and `frontend/src/utils/formatters.ts`: Bangla default with verbatim labels ("আবেদন করুন", "ভর্তি ফি দিন"), Latin numerals (0–9), and `Asia/Dhaka` time zone formatting.
- Built frontend production assets cleanly: `npm run build` completed with 0 errors.

### 1.6 Independent Test Execution
- Executed local automated test runner:
  ```bash
  $ ./scripts/test-e2e.sh --local
  Passed: 30, Failed: 0, Total: 30, Duration: 9.0157 Seconds.
  ```
- Executed container automated test runner:
  ```bash
  $ ./scripts/test-e2e.sh
  # (Runs docker compose --profile test run --rm backend-tests)
  Passed: 30, Failed: 0, Total: 30, Duration: 14.0472 Seconds.
  ```
- All 3 concurrency tests (200 concurrent requests against 20 seats with 0 overselling; 30 parallel requests from same student with exactly 1 granted; 20 parallel confirms with exactly 1 increment) passed without error.

### 1.7 Docker Startup Verification (FAILING CHECK)
- Executed `docker compose up -d` from a clean state:
  ```bash
  $ docker compose up -d
  ```
  Result:
  ```
  ✔ Container school_admission_db           Healthy                          5.7s
  ✘ Container school_admission_backend      Error dependency backend f...   61.8s
  ✔ Container school_admission_frontend     Created                          0.1s
  dependency failed to start: container school_admission_backend is unhealthy
  ```
  Exit code: 1.
- Inspected Docker healthcheck logs for `school_admission_backend`:
  ```bash
  $ docker inspect --format='{{json .State.Health}}' school_admission_backend
  ```
  Output:
  ```json
  {
    "Status": "unhealthy",
    "FailingStreak": 10,
    "Log": [
      {
        "ExitCode": 1,
        "Output": "http://localhost:8080/health/ready:\nRemote file does not exist -- broken link!!!\n"
      }
    ]
  }
  ```
- Root Cause Analysis:
  1. In `docker-compose.yml` line 42:
     ```yaml
     healthcheck:
       test: ["CMD-SHELL", "wget --no-verbose --tries=1 --spider http://localhost:8080/health/ready || exit 1"]
     ```
  2. The `wget --spider` flag issues an HTTP `HEAD` request.
  3. In `backend/src/SchoolAdmission.Api/Endpoints/HealthEndpoints.cs` line 16:
     ```csharp
     group.MapGet("/ready", async (AppDbContext context, CancellationToken cancellationToken) => ...
     ```
     ASP.NET Core Minimal APIs mapped with `MapGet` do not match HTTP `HEAD` requests by default, returning `HTTP/1.1 405 Method Not Allowed`.
  4. Executed verbatim command inside container:
     ```bash
     $ docker compose exec backend wget --no-verbose --tries=1 --spider http://localhost:8080/health/ready
     # Exit code 8: Remote file does not exist -- broken link!!!
     ```
     Whereas sending an HTTP `GET` request succeeds:
     ```bash
     $ docker compose exec backend wget -qO- http://localhost:8080/health/ready
     {"status":"Healthy"}
     # Exit code 0
     ```
  5. Because every probe fails with exit code 1/8, Docker marks `school_admission_backend` as `unhealthy` after 10 retries.
  6. In `docker-compose.yml` lines 56–58, `frontend` has:
     ```yaml
     depends_on:
       backend:
         condition: service_healthy
     ```
     Docker Compose refuses to start `school_admission_frontend`, aborting startup with `dependency failed to start: container school_admission_backend is unhealthy`.
  7. This directly violates the Acceptance Criteria in `ORIGINAL_REQUEST.md`:
     `Running docker compose up starts PostgreSQL, the backend API at http://localhost:8080, and the frontend UI without manual intervention.`

---

## 2. Logic Chain

1. Per `ORIGINAL_REQUEST.md` line 8 ("Integrity mode: development") and Acceptance Criteria:
   - "Running `docker compose up` starts PostgreSQL, the backend API at `http://localhost:8080`, and the frontend UI without manual intervention."
2. Observation 1.1–1.6 proves that the codebase is completely genuine: zero dummy/mock facades, genuine `SELECT ... FOR UPDATE` row locks with PostgreSQL `clock_timestamp()`, valid partial unique index, compliant RFC 9457 ProblemDetails, full React 19 UI with dynamic countdown, and 30/30 passed integration/concurrency tests.
3. However, Observation 1.7 demonstrates empirically that running `docker compose up` fails to start the stack without manual intervention.
4. The failure is directly traced to a method mismatch between `wget --spider` (which sends HTTP `HEAD`) and ASP.NET Core Minimal API `MapGet` (which returns HTTP `405 Method Not Allowed`).
5. Per the Integrity Forensics instruction: "Trust NOTHING — verify EVERYTHING. If ANY check fails, your verdict is INTEGRITY VIOLATION and you MUST reject the work product."
6. Because Check 6 ("Docker & Persistence verification: Verify `docker compose config`, `docker compose up` readiness, and persistence across restart") failed on `docker compose up` readiness, the work product cannot be certified as clean in its current state.

---

## 3. Caveats
- No logic, security, or concurrency flaws were detected in the backend C# source code, PostgreSQL schema, or React 19 frontend application.
- The failure is isolated entirely to the Docker Compose healthcheck definition (or alternatively the Minimal API `HEAD` mapping).
- Once the healthcheck test command is updated or `HealthEndpoints.cs` maps `HEAD`, the stack will achieve 100% compliance.

---

## 4. Conclusion

## Forensic Audit Report

**Work Product**: Entire School Admission System Codebase & Docker Environment  
**Profile**: General Project  
**Integrity Mode**: Development Mode  
**Verdict**: **INTEGRITY VIOLATION**

### Phase Results
- [Anti-Cheating / Facade Detection]: PASS — No mock/dummy/facade implementations or hardcoded test returns.
- [Concurrency & Database Clock]: PASS — Genuine `SELECT ... FOR UPDATE` with `clock_timestamp()` inside transaction.
- [Database Invariants & Indexes]: PASS — `ux_holds_active_student_class`, check constraints, and `/var/lib/postgresql` volume mount verified.
- [API Contract & RFC 9457]: PASS — All 8 REST endpoints and ProblemDetails formatting verified.
- [Frontend React 19 & UX]: PASS — 17-digit header, live 1s countdown, dynamic expiry without reload, and Bangla localization verified.
- [Automated Test Suite]: PASS — 30/30 tests passed in container and local execution.
- [Docker Startup Readiness]: **FAIL** — `docker compose up` fails because `wget --spider` healthcheck returns HTTP 405 Method Not Allowed, causing Docker to mark `backend` as unhealthy and blocking `frontend` startup.

### Remediation Guidance for Orchestrator
To clear the integrity violation, dispatch subagent **devops** or **backend** to apply one of the following two one-line fixes:
- **Option 1 (DevOps — Recommended in `docker-compose.yml`)**:
  Change line 42 of `docker-compose.yml` from:
  ```yaml
  test: ["CMD-SHELL", "wget --no-verbose --tries=1 --spider http://localhost:8080/health/ready || exit 1"]
  ```
  to:
  ```yaml
  test: ["CMD-SHELL", "wget -qO- http://localhost:8080/health/ready > /dev/null || exit 1"]
  ```
- **Option 2 (Backend — In `HealthEndpoints.cs`)**:
  Change line 16 of `backend/src/SchoolAdmission.Api/Endpoints/HealthEndpoints.cs` to map both `GET` and `HEAD`:
  ```csharp
  group.MapMethods("/ready", new[] { "GET", "HEAD" }, async (AppDbContext context, CancellationToken cancellationToken) => ...
  ```

---

## 5. Verification Method

To independently reproduce the failure and confirm the finding:
1. Stop any existing containers:
   ```bash
   docker compose down
   ```
2. Run standard one-command startup:
   ```bash
   docker compose up -d
   ```
3. Observe command failure with exit code 1:
   `dependency failed to start: container school_admission_backend is unhealthy`
4. Inspect container health log:
   ```bash
   docker inspect --format='{{json .State.Health}}' school_admission_backend
   ```
5. Confirm HTTP 405 response:
   ```bash
   curl -I http://localhost:8080/health/ready
   # Output: HTTP/1.1 405 Method Not Allowed
   ```
