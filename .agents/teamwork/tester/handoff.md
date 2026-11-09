# QA Test Engineer Handoff Report

## 1. Observation
- **Test Suite Location**: `backend/tests/SchoolAdmission.Tests/`
- **Test Runner Scripts**: `scripts/test-e2e.sh`, `scripts/run-tests.sh`
- **Docker Test Service**: `backend-tests` in `docker-compose.yml`
- **Compilation Output**:
  ```bash
  $ dotnet build backend/SchoolAdmission.sln
  SchoolAdmission.Api -> /home/ashiq/work/school-admission-system/backend/src/SchoolAdmission.Api/bin/Debug/net10.0/SchoolAdmission.Api.dll
  SchoolAdmission.Tests -> /home/ashiq/work/school-admission-system/backend/tests/SchoolAdmission.Tests/bin/Debug/net10.0/SchoolAdmission.Tests.dll
  Build succeeded.
      0 Warning(s)
      0 Error(s)
  Time Elapsed 00:00:08.30
  ```
- **Automated Container Test Run Execution (`scripts/test-e2e.sh` / `docker compose --profile test run --rm backend-tests`)**:
  ```
  Test Run Successful.
  Total tests: 30
       Passed: 30
   Total time: 20.0576 Seconds

    SchoolAdmission.Tests net10.0 Testing (20.8s)
    SchoolAdmission.Tests test net10.0 succeeded (20.8s)

  Test summary: total: 30, failed: 0, succeeded: 30, skipped: 0, duration: 20.7s
  Build succeeded in 42.8s
  ==========================================================
    [SUCCESS] All automated tests passed successfully!     
  ==========================================================
  ```
- **Automated Local Test Run Execution (`bash scripts/test-e2e.sh --local`)**:
  ```
  Test Run Successful.
  Total tests: 30
       Passed: 30
   Total time: 12.7145 Seconds
  ```
- **Breakdown of Test Coverage (30 Tests across 6 Suites)**:
  1. `ConcurrencyOversellingTests.cs`:
     - `Concurrency_NoOverselling_200ConcurrentRequests_For20Seats_Exactly20Succeed_180Conflict` [PASSED]
     - Creates 20-seat class, sends 200 concurrent `POST /classes/{id}/holds` from 200 distinct 17-digit student IDs.
     - Confirms exactly 20 return HTTP 201 Created, 180 return HTTP 409 Conflict.
     - Confirms `GET /classes/{id}` reports `heldSeats=20`, `availableSeats=0`, `confirmedSeats=0`, `totalSeats=20`. Zero overselling.
  2. `ConcurrencySingleActiveApplicationTests.cs`:
     - `Concurrency_SingleActiveApplication_30ParallelRequests_SameStudent_ExactlyOneSucceeds_29Conflict` [PASSED]
     - 30 parallel requests from the same student ID for the same class. Exactly 1 returns HTTP 201 Created, 29 return HTTP 409 Conflict.
  3. `ConcurrencyIdempotentConfirmTests.cs`:
     - `Concurrency_IdempotentConfirm_20ConcurrentRequests_AllReturn200Confirmed_ConfirmedSeatsIncrementsBy1` [PASSED]
     - Creates 1 hold, triggers 20 concurrent `POST /holds/{id}/confirm` requests.
     - All 20 return HTTP 200 OK `status: CONFIRMED`. Class confirmedSeats increments by exactly 1.
  4. `HoldExpiryTests.cs`:
     - `Expiry_HoldWithShortDuration_ExpiredWithoutApiCalls_SeatReturnedToPool_ConfirmReturns409` [PASSED]
     - Configures hold duration = 2s via `ExpiryTestApiFactory`. Applies hold, waits 3.5s with zero intervening API calls.
     - Queries `GET /classes/{id}`: seat is returned to available pool (`availableSeats=1`, `heldSeats=0`).
     - Queries `GET /holds/{id}`: derives status `EXPIRED`.
     - Calls `POST /holds/{id}/confirm`: returns HTTP 409 Conflict with code `HOLD_EXPIRED`.
  5. `ApiContractTests.cs` (Validates all 8 endpoints and RFC 9457 error contracts):
     - `POST /classes` (Endpoint 1): Valid creation, checks title/detail/code on invalid name and seat count. [PASSED]
     - `GET /classes` (Endpoint 2): Array response format, checks exact camelCase field names. [PASSED]
     - `GET /classes/{id}` (Endpoint 3): Detailed summary response with availableSeats calculation. [PASSED]
     - `POST /classes/{id}/holds` (Endpoint 4): Correct hold response and missing/invalid header validation. [PASSED]
     - `GET /holds/{id}` (Endpoint 5): Correct hold status, 404 on nonexistent hold. [PASSED]
     - `POST /holds/{id}/confirm` (Endpoint 6): Changes status to CONFIRMED. [PASSED]
     - `DELETE /holds/{id}` (Endpoint 7): Cancels hold immediately, releases seat back to available, idempotent second DELETE returns 409. [PASSED]
     - `GET /me/holds` (Endpoint 8): Returns list of student holds, missing header returns 400. [PASSED]
  6. `ValidationTests.cs`:
     - 12 unit tests validating 17-digit student IDs, whitespace handling, negative seats, seat overflows, and name length limits. [ALL PASSED]

## 2. Logic Chain
1. From Observation 1: The test suite was constructed using xUnit with `WebApplicationFactory` pointing to the real PostgreSQL database in the Compose environment (`Host=db` or container bridge IP `172.18.0.2`).
2. From Observation 2: Under burst concurrency (200 concurrent HTTP requests firing simultaneously), PostgreSQL's default `max_connections=100` coupled with default Npgsql pool size (100) led to client connection exhaustion.
3. From Observation 3: Updating `docker-compose.yml` to set PostgreSQL `command: ["postgres", "-c", "max_connections=250"]` and setting `Maximum Pool Size=60;Timeout=60;Command Timeout=60;` in the test connection string provided sufficient headroom for Postgres while bounding test worker pool contention.
4. From Observation 4: In ASP.NET Core top-level statements, reading `builder.Configuration.GetConnectionString("Default")` eagerly before `ConfigureWebHost` prevented test factory overrides from taking effect. Modifying `Program.cs` to resolve `IConfiguration` via `sp.GetRequiredService<IConfiguration>()` inside `AddDbContext((sp, options) => ...)` ensured dynamic configuration override, allowing `ExpiryTestApiFactory` to isolate the 2-second hold duration while `SchoolAdmissionApiFactory` isolates the 600-second hold duration for concurrency tests.
5. From Observation 5: In `docker compose --profile test run --rm backend-tests` and `scripts/test-e2e.sh`, all 30 tests ran and succeeded in 20.05 seconds, with zero overselling, zero race conditions, and complete API contract conformity.

## 3. Caveats
- Tests hit the real PostgreSQL database container `school_admission_db`. Although tests generate unique GUID class names and unique student IDs (`10000000000000001`+), running tests leaves these historical rows in the DB volume. If an exact pristine state is desired before running demo data, run `docker compose down -v && docker compose up -d` to re-seed.
- Tests disable parallel execution across test classes via `[assembly: CollectionBehavior(DisableTestParallelization = true)]` to prevent multi-suite connection starvation on PostgreSQL. Tests within concurrency test cases run with genuine asynchronous parallelism (`Task.WhenAll`).

## 4. Conclusion
All requirements outlined in the QA Test Engineer Dispatch and PLAN.md (§1.1, §6, §11) are fully implemented and verified:
- Genuine xUnit automated suite in `backend/tests/SchoolAdmission.Tests/` covering Concurrency 1 (No Overselling under 200 concurrent requests), Concurrency 2 (Single Active Application under 30 parallel requests), Concurrency 3 (Idempotent Confirm under 20 parallel requests), Expiry Test (dynamic expiration without API calls), and API Contract Tests (all 8 endpoints + RFC 9457 ProblemDetails).
- Standalone test runner script `scripts/test-e2e.sh` and symlink `scripts/run-tests.sh` created and verified for both Docker container test profile (`backend-tests`) and local execution.
- 100% test pass rate (30/30 passed) against real PostgreSQL database.

## 5. Verification Method
To independently verify the test suite:
1. Ensure the docker stack is running:
   ```bash
   docker compose up -d db backend
   ```
2. Run the test runner script:
   ```bash
   ./scripts/test-e2e.sh
   ```
   Or directly via Docker Compose:
   ```bash
   docker compose --profile test run --rm backend-tests
   ```
   Or locally (if .NET 10 SDK is installed):
   ```bash
   ./scripts/test-e2e.sh --local
   ```
3. Invalidation condition: Any test failure in the 30-test suite, any HTTP 500 error during concurrent holds, or any condition where `heldSeats + confirmedSeats > totalSeats`.
