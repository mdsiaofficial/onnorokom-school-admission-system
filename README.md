# School Admission System (অনলাইন স্কুল ভর্তি সিস্টেম)

A robust, high-concurrency School Admission System built with **.NET 10 (LTS)**, **PostgreSQL 18**, and **React 19 (TypeScript + Vite)**, fully containerized with **Docker Compose**.

Designed for first-come, first-served admission rush hours with strict concurrency guarantees: **no seat overselling under any load**, dynamic seat expiry, and multi-process safety.

> **Technology reference:** see [`TECH_STACK.md`](TECH_STACK.md) for every library used and the reasoning behind each choice.

---

## 1. Quick Start (One Command)

The entire application runs with a single command on any machine with Docker installed. No local .NET, Node.js, or PostgreSQL installations are required.

```bash
docker compose up --build
```

Once all services are healthy:
- **Frontend UI**: [http://localhost:3000](http://localhost:3000)
- **Backend API**: [http://localhost:8080](http://localhost:8080)
- **Interactive Swagger Docs**: [http://localhost:8080/swagger](http://localhost:8080/swagger)
- **Health Checks**:
  - Liveness: [http://localhost:8080/health/live](http://localhost:8080/health/live)
  - Readiness: [http://localhost:8080/health/ready](http://localhost:8080/health/ready)

To run the stack in detached mode:
```bash
docker compose up -d --build
```

To stop all services:
```bash
docker compose down
```

---

## 2. Testing the UI

1. Open [http://localhost:3000](http://localhost:3000) in your browser.
2. In the top navigation bar, enter a **17-digit Student ID** (Birth Registration Number / জন্মনিবন্ধন নম্বর).
   - Sample IDs:
     - `20150000000000001`
     - `20150000000000002`
     - `20150000000000003`
3. Browse the **Class List** with live seat counts (total, held, confirmed, and available).
4. Click **"আবেদন করুন" (Apply)** to hold a seat. A 10-minute hold window starts immediately.
5. Navigate to **"আমার আবেদন" (My Applications)** to view your hold status and live countdown ticker.
6. Click **"ভর্তি ফি দিন" (Pay Admission Fee)** to confirm your admission, or **"বাতিল করুন" (Cancel)** to immediately release the held seat.
7. Switch between **Bangla (বাংলা)** and **English** using the language toggle in the header.

---

## 3. Data Persistence Across Restarts

All database records, admissions, and holds are stored in a Docker named volume (`school_admission_db_data`).

Verify data persistence across restarts:
```bash
# 1. Stop containers
docker compose down

# 2. Restart containers
docker compose up -d

# 3. Check http://localhost:8080/classes or http://localhost:3000
# All classes, holds, and confirmed admissions remain intact!
```

To wipe the database volume and re-seed from scratch:
```bash
docker compose down -v
```

---

## 4. Automated Tests

### 4.1 Running Automated Tests via Docker (Recommended)
You can run the automated xUnit integration and concurrency test suite inside a dedicated Docker container against the real PostgreSQL service:

```bash
# The test image is a separate Docker stage, so rebuild it after changing backend code:
docker compose --profile test build backend-tests
docker compose --profile test run --rm backend-tests
```

> `docker compose up --build` only rebuilds the `backend` service. The `backend-tests` service
> targets the `test-runner` stage of the same Dockerfile and must be rebuilt explicitly,
> otherwise you will run the previously compiled test assembly.

### 4.2 Running Tests Locally (Developers)
If you have .NET 10 SDK and PostgreSQL installed locally:
```bash
dotnet test backend/SchoolAdmission.sln --logger:console;verbosity=normal
```

### 4.3 What the Automated Tests Cover
1. **Concurrency — No Overselling**: 200 parallel requests compete for 20 seats; exactly 20 succeed with `201 Created` and 180 receive `409 Conflict`. Zero overselling.
2. **Concurrency — One Active Application per Student**: 30 simultaneous requests for the same student on one class; exactly one succeeds, 29 fail with `409 Conflict`.
3. **Concurrency — Idempotent Confirmation**: 20 concurrent confirm calls for the same hold; all return `200 OK CONFIRMED`, and `confirmedSeats` increments by exactly 1.
4. **Dynamic Expiry**: Applies for a seat with a short hold duration (2s), waits past expiry without calling any API; confirms the seat is immediately available again, status derives to `EXPIRED`, and confirmation fails with `409 Conflict`.
5. **Contract & Error Verification**: Validates all 8 endpoints, status codes, JSON response shapes, RFC 9457 error formats, and `X-Student-Id` header validation.

---

## 5. System Architecture

```
+-----------------------------------------------------------+
|                     Browser / Client                      |
|  React 19 + TypeScript + Vite + Zustand + TanStack Query  |
|  - Student ID Bar (17-digit persisted in localStorage)    |
|  - Class Listing (Live seat counts polling ~3s)           |
|  - Admin Class Creation Form                              |
|  - My Applications (Live countdown, confirm, cancel)      |
+-----------------------------+-----------------------------+
                              | HTTP (Port 8080)
                              | Header: X-Student-Id
                              v
+-----------------------------------------------------------+
|                  ASP.NET Core Minimal API                 |
|  - Endpoints: /classes, /holds, /me/holds, /health        |
|  - Application Services: ClassService, HoldService        |
|  - Concurrency Lock: Row-level SELECT ... FOR UPDATE      |
|  - Clock: Database clock clock_timestamp()                |
|  - Background Expiry Sweeper (PeriodicTimer)              |
+-----------------------------+-----------------------------+
                              | Npgsql / EF Core 10
                              v
+-----------------------------------------------------------+
|                      PostgreSQL 18                        |
|  - Classes (id, name, total_seats, created_at)            |
|  - Holds (id, class_id, student_id, status, expires_at,   |
|           confirmed_at, cancelled_at, updated_at)         |
|  - Partial Unique Index: (student_id, class_id)           |
|    WHERE status IN ('HELD', 'CONFIRMED')                  |
+-----------------------------+-----------------------------+
```

---

## 6. Environment Configuration

Default values are pre-configured in `docker-compose.yml`. You can copy `.env.example` to `.env` to override:

| Variable | Default | Description |
|---|---|---|
| `POSTGRES_USER` | `postgres` | Database superuser |
| `POSTGRES_PASSWORD` | `postgres` | Database password |
| `POSTGRES_DB` | `school_admission` | Database name |
| `HOLD_DURATION_SECONDS` | `600` | Seat hold duration (10 minutes). Must be a positive integer — the API refuses to start otherwise. |
| `SEED_DEMO_DATA` | `true` | Seed 12 standard classes on clean DB |
| `CORS_ALLOWED_ORIGINS` | `http://localhost:3000` | Allowed browser origins (comma-separated). Requests from any other origin are rejected. |
| `VITE_API_BASE_URL` | `http://localhost:8080` | Backend API URL reachable by browser |

> `.env` is read by Docker Compose, not by the app — after editing it, recreate the container
> (`docker compose up -d --force-recreate backend`); a plain `restart` keeps the old values.
> See [`TECH_STACK.md`](TECH_STACK.md) for the full configuration precedence chain.

---

## 7. Pre-Seeded Demo Classes

When starting with an empty database and `SEED_DEMO_DATA=true`, the system automatically provisions 12 classes with realistic seat distributions:
- **Class 1 to Class 5** (Morning & Day shifts)
- **Class 6** (Morning & Day shifts)
- **Class 7 to Class 10** (Including full and almost-full states to test UI boundaries)
- **Demo - Only 2 Seats** (Quick class with 2 seats for manual concurrency testing)
