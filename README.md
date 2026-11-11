# School Admission System

A containerized, concurrent school seat admission and hold management system built with **.NET 10 Minimal APIs**, **PostgreSQL 18**, and **React 19 (TypeScript + Vite)**.

Designed for first-come, first-served rush hours with strict concurrency guarantees: **zero seat overselling**, dynamic seat expiry, and idempotent payment confirmation.

---

## 1. Quick Start

Run the complete stack with a single command (no local runtime installations required):

```bash
docker compose up --build -d
```

* **Frontend UI**: [http://localhost:3000](http://localhost:3000)
* **Backend API**: [http://localhost:8080](http://localhost:8080)
* **Interactive Swagger Docs**: [http://localhost:8080/swagger](http://localhost:8080/swagger)
* **Health Readiness**: [http://localhost:8080/health/ready](http://localhost:8080/health/ready)

To stop all containers:
```bash
docker compose down
```

---

## 2. Testing the UI

1. Open [http://localhost:3000](http://localhost:3000).
2. Enter a **17-digit Birth Registration Number** (e.g. `20150000000000001`).
3. Browse the live class list with real-time seat status counts (Total, Held, Confirmed, Available).
4. Click **Apply (আবেদন করুন)** to hold a seat — the hold window is `HOLD_DURATION_SECONDS` (10 minutes by default) and the countdown is shown live.
5. In **My Applications (আমার আবেদন)**, click **Pay Admission Fee** to confirm or **Cancel** to immediately return the seat.
6. Use the **Teacher Login (Demo)** button in the navbar to test administrative class creation.
7. Switch the interface language with the **English / বাংলা** toggle in the navbar — Bangla is the default, English is available for staff and reviewers (see [`ASSUMPTIONS.md`](ASSUMPTIONS.md) §8).

---

## 3. Automated Concurrency & Integration Tests

### Run Tests via Docker
```bash
docker compose --profile test build backend-tests
docker compose --profile test run --rm backend-tests
```
*(Or run `./scripts/test-e2e.sh`)*

### Run Tests Locally
```bash
dotnet test backend/SchoolAdmission.sln --logger:"console;verbosity=normal"
```

or

```bash
dotnet test backend/SchoolAdmission.sln
```

### Detailed Concurrency Test Output
When the concurrency tests execute, they output formatted benchmark metrics directly to the console:

```text
================================================================================
 [CONCURRENCY TEST RESULT: NO OVERSELLING]
 Total Concurrent Requests Sent : 1000 parallel requests
 Target Class Total Seats       : 20 seats
 Successfully Held Seats        : 20 requests (HTTP 201 Created)
 Rejected (Seats Full)          : 980 requests (HTTP 409 Conflict)
 Oversold Seats                 : 0 (PASSED - Strict capacity invariant held)
================================================================================

================================================================================
 [CONCURRENCY TEST RESULT: SINGLE APPLICATION PER STUDENT]
 Simultaneous Requests from Same ID : 30 parallel requests
 Granted Application (Hold)         : 1 request (HTTP 201 Created)
 Blocked Duplicate Attempts         : 29 requests (HTTP 409 Conflict)
 Invariant Verified                 : PASSED (No duplicate holds for same student)
================================================================================

================================================================================
 [CONCURRENCY TEST RESULT: IDEMPOTENT PAYMENT CONFIRMATION]
 Simultaneous Confirmations Sent : 20 parallel requests
 Successful Confirmations (200) : 20 requests
 Total Confirmed Seats Added     : 1 (Exactly 1, no duplicate counting)
 Invariant Verified              : PASSED (Idempotency under race conditions)
================================================================================
```

---

## 4. Key Architectural Guarantees

* **PostgreSQL Pessimistic Locking (`SELECT ... FOR UPDATE`)**: Eliminates race conditions during seat allocation.
* **Database Engine Clock Synchronization (`clock_timestamp()`)**: Eliminates clock drift issues between distributed application servers.
* **Partial Unique Indexes**: Enforces single active application per 17-digit student identifier across all classes.
* **Persistent Named Volumes**: Database state is preserved across container restarts (`docker compose down && docker compose up`).
