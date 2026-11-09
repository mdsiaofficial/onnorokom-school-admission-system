# Project: School Admission System

## 1. Overview & Requirements Cross-Match

### 1.1 Requirements Matrix (Brief vs PLAN.md)
| Requirement ID | Brief Requirement | PLAN.md Mapping | Coverage Status | Notes |
|---|---|---|---|---|
| REQ-01 | Tech stack: .NET 8+, Postgres, React 18+, Docker | .NET 10 LTS, Postgres 18, React 19 (Vite), Compose v2 | FULL | Postgres 18 volume at `/var/lib/postgresql` |
| REQ-02 | Ports: Backend 8080, Frontend 3000 | Backend 8080, Frontend 3000, Postgres internal 5432 | FULL | Verified in compose config |
| REQ-03 | Configurable env vars: DB connection & API base URL | `ConnectionStrings__Default`, `VITE_API_BASE_URL` | FULL | Configurable via `.env` / compose |
| REQ-04 | 1-command startup: `docker compose up` | `docker-compose.yml` with healthchecks & depends_on | FULL | Startup order: db -> backend -> frontend |
| REQ-05 | Data persistence across `compose down && compose up` | Named Docker volume `db-data` for Postgres | FULL | Persistence preserved on restart |
| REQ-06 | Admin class creation (name, totalSeats) | `POST /classes` with validation | FULL | 1-120 chars, seats > 0 |
| REQ-07 | Class listing with seat breakdown | `GET /classes` (total, held, confirmed, available) | FULL | Single query aggregation |
| REQ-08 | Single class query | `GET /classes/{classId}` | FULL | 200 or 404 CLASS_NOT_FOUND |
| REQ-09 | 17-digit student identifier (`X-Student-Id`) | Header check on holds/confirm/cancel/me | FULL | Validation regex `^[0-9]{17}$` |
| REQ-10 | Seat hold application (`POST /classes/{id}/holds`) | Row-level locking on class + DB clock expiry | FULL | 201 `{holdId, classId, studentId, status, expiresAt}` |
| REQ-11 | Hold status query (`GET /holds/{id}`) | Returns hold details without studentId | FULL | 200 `{holdId, classId, status, expiresAt}` |
| REQ-12 | Confirm admission (`POST /holds/{id}/confirm`) | Validates ownership, expiry; idempotent | FULL | 200 `{holdId, status: "CONFIRMED"}` |
| REQ-13 | Cancel hold (`DELETE /holds/{id}`) | Frees seat immediately, idempotent | FULL | 204 No Content |
| REQ-14 | Student holds query (`GET /me/holds`) | Returns all student applications with className | FULL | Joined with classes table |
| REQ-15 | Concurrency: no overselling (`held + confirmed <= total`) | `SELECT ... FOR UPDATE` on classes + DB clock | FULL | Strict serialization per class |
| REQ-16 | Concurrency: 1 active hold/confirmed per student/class | Partial unique index + pre-lock cleanup | FULL | 409 ALREADY_HELD / ALREADY_CONFIRMED |
| REQ-17 | Concurrency: confirm idempotency | Row lock + status check; confirmed once | FULL | Multiple confirms safe |
| REQ-18 | Dynamic expiry without page reload | Derived expiry on API + React timer countdown | FULL | Expired row frees seat instantly |
| REQ-19 | UI seat polling every few seconds | TanStack Query refetchInterval (~3s) | FULL | Auto-polls with background pause |
| REQ-20 | Student ID persistence in browser | Zustand `persist` to `localStorage` | FULL | Survives refresh & restart |
| REQ-21 | Localized UI & friendly error messages | Bangla default + English toggle, RFC 9457 codes | FULL | Central error-to-message mapping |
| REQ-22 | Automated testing (3+ backend, 1 concurrency) | 5 xUnit tests hitting real PostgreSQL + E2E suite | FULL | Concurrency test with 200 workers |
| REQ-23 | Project documentation & git commits | README.md, ASSUMPTIONS.md, AI_USAGE.md, .gitignore | FULL | Assignment brief excluded from git |

---

## 2. Architecture & System Design

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

## 3. Team Responsibilities & Delegation Matrix

| Subagent Role | Primary Ownership | Key Artifacts | Dependencies | Status |
|---|---|---|---|---|
| **DevOps** | Infrastructure, Dockerfiles, docker-compose.yml, .env.example, .gitignore, startup verification | `docker-compose.yml`, `backend/Dockerfile`, `frontend/Dockerfile`, `.env.example`, `.gitignore`, `README.md` | None | DONE |
| **Backend** | .NET 10 solution, Postgres EF migrations, concurrency engine (row locks, DB clock), all 8 REST endpoints, seeder, background sweeper | `backend/src/SchoolAdmission.Api/`, `backend/tests/` | DevOps Dockerfile/compose | DONE |
| **Frontend** | React 19 + Vite + TypeScript application, Zustand stores, TanStack Query, HeroUI v3 + Tailwind v4, i18n (Bangla default), student ID bar, countdown timer | `frontend/src/`, `frontend/package.json` | Backend API contract | DONE |
| **QA Tester** | Test harnesses: xUnit concurrency & contract tests in backend/tests, plus standalone integration test script for running containers | `backend/tests/`, `scripts/test-e2e.sh` | Backend & DevOps running services | DONE |
| **Security** | Security audit, input sanitization, 17-digit validation, CORS, rate limits/burst tests, error masking (no stack traces) | `SECURITY_AUDIT.md`, security fixes | Backend & Frontend codebases | DONE |

---

## 4. Feature Inventory & Milestone Mapping

| # | Feature | Description | Milestone | Owner | Status |
|---|---|---|---|---|---|
| 1 | Docker Compose & Container Infra | Postgres 18 with persistent volume, backend and frontend services, healthchecks | M1 | DevOps | DONE |
| 2 | Solution & Project Scaffolding | .NET 10 Minimal API project & React 19 Vite TS setup | M1 | DevOps / Backend / Frontend | DONE |
| 3 | Database Schema & Migrations | PostgreSQL tables (classes, holds), constraints, partial unique index | M2 | Backend | DONE |
| 4 | Concurrency Row-Lock Engine | `SELECT ... FOR UPDATE` with DB clock for apply, confirm, cancel | M2 | Backend | DONE |
| 5 | Class Management Endpoints | `POST /classes`, `GET /classes`, `GET /classes/{id}` with seat calculations | M2 | Backend | DONE |
| 6 | Hold & Application Endpoints | `POST /classes/{id}/holds`, `GET /holds/{id}`, `POST /holds/{id}/confirm`, `DELETE /holds/{id}`, `GET /me/holds` | M2 | Backend | DONE |
| 7 | Derived Expiry & Sweeper | Status derived at query time; background cleanup service | M2 | Backend | DONE |
| 8 | Demo Data Seeder | 12 classes with realistic seat distributions (active on clean DB) | M2 | Backend | DONE |
| 9 | Student ID Management | Top navigation bar with 17-digit validation and persistent storage | M3 | Frontend | DONE |
| 10 | Class List & Live Polling | Class cards/table with live counts (held, confirmed, available) and Apply button | M3 | Frontend | DONE |
| 11 | My Applications & Expiry Countdown | Application list, live countdown ticker, simulated payment, cancellation | M3 | Frontend | DONE |
| 12 | Admin Class Creation UI | Form with validation for adding new class/shift | M3 | Frontend | DONE |
| 13 | Localisation (Bangla/English) | Bangla by default, English toggle, Latin numerals, Dhaka time zone | M3 | Frontend | DONE |
| 14 | Error Handling & Toast Feedback | RFC 9457 error code translation to user-friendly messages | M3 | Frontend | DONE |
| 15 | Automated Concurrency Test Suite | xUnit tests for overselling prevention, 1 active hold per student, idempotency | M4 | QA Tester | DONE |
| 16 | Automated Contract Test Suite | Full suite testing all 8 endpoints against specification | M4 | QA Tester | DONE |
| 17 | Security Hardening & Input Audits | Validation of student ID, XSS prevention, injection safety, CORS configuration | M5 | Security | DONE |
| 18 | E2E Stack Verification & Persistence | Verification of one-command startup, data survival across compose down/up | M6 | Remediation / DevOps | DONE |
| 19 | Forensic Integrity Certification | Independent anti-cheating, code authenticity, and container audit | M7 | Forensic Auditor | DONE (CLEAN) |
| 20 | Project Documentation | README.md, ASSUMPTIONS.md, AI_USAGE.md | M6 | DevOps / Backend | DONE |

---

## 5. Interface Contracts & API Definitions

### 5.1 Endpoints
1. `POST /classes`
   - Request Body: `{ "name": string (1-120), "totalSeats": int (>0) }`
   - Response: `201 Created` -> `{ "id": string, "name": string, "totalSeats": int }`
   - Errors: `400 Bad Request`

2. `GET /classes`
   - Response: `200 OK` -> `[ { "id": string, "name": string, "totalSeats": int, "heldSeats": int, "confirmedSeats": int, "availableSeats": int } ]`

3. `GET /classes/{classId}`
   - Response: `200 OK` -> `{ "id": string, "name": string, "totalSeats": int, "heldSeats": int, "confirmedSeats": int, "availableSeats": int }`
   - Errors: `404 Not Found` -> `{ "code": "CLASS_NOT_FOUND", ... }`

4. `POST /classes/{classId}/holds`
   - Header: `X-Student-Id: <17-digit string>`
   - Response: `201 Created` -> `{ "holdId": string, "classId": string, "studentId": string, "status": "HELD", "expiresAt": "<UTC ISO-8601>" }`
   - Errors:
     - `400 Bad Request` -> `INVALID_STUDENT_ID`
     - `404 Not Found` -> `CLASS_NOT_FOUND`
     - `409 Conflict` -> `SEATS_FULL`, `ALREADY_HELD`, `ALREADY_CONFIRMED`

5. `GET /holds/{holdId}`
   - Response: `200 OK` -> `{ "holdId": string, "classId": string, "status": "HELD" | "CONFIRMED" | "EXPIRED" | "CANCELLED", "expiresAt": "<UTC ISO-8601>" }`
   - Errors: `404 Not Found` -> `HOLD_NOT_FOUND`

6. `POST /holds/{holdId}/confirm`
   - Header: `X-Student-Id: <17-digit string>`
   - Response: `200 OK` -> `{ "holdId": string, "status": "CONFIRMED" }` (idempotent)
   - Errors:
     - `400 Bad Request` -> `INVALID_STUDENT_ID`
     - `403 Forbidden` -> `NOT_HOLD_OWNER`
     - `404 Not Found` -> `HOLD_NOT_FOUND`
     - `409 Conflict` -> `HOLD_EXPIRED`, `HOLD_CANCELLED`

7. `DELETE /holds/{holdId}`
   - Header: `X-Student-Id: <17-digit string>`
   - Response: `204 No Content` (idempotent if already cancelled)
   - Errors:
     - `400 Bad Request` -> `INVALID_STUDENT_ID`
     - `403 Forbidden` -> `NOT_HOLD_OWNER`
     - `404 Not Found` -> `HOLD_NOT_FOUND`
     - `409 Conflict` -> `HOLD_ALREADY_CONFIRMED`

8. `GET /me/holds`
   - Header: `X-Student-Id: <17-digit string>`
   - Response: `200 OK` -> `[ { "holdId": string, "classId": string, "className": string, "status": string, "expiresAt": "<UTC ISO-8601>" } ]`
   - Errors: `400 Bad Request` -> `INVALID_STUDENT_ID`

---

## 6. Code Layout & Exclusive File Ownership

- **DevOps**:
  - `docker-compose.yml`
  - `.env.example`
  - `.gitignore`
  - `backend/Dockerfile`
  - `backend/.dockerignore`
  - `frontend/Dockerfile`
  - `frontend/.dockerignore`
  - `README.md`
  - `ASSUMPTIONS.md`
  - `AI_USAGE.md`
- **Backend**:
  - `backend/SchoolAdmission.sln`
  - `backend/src/SchoolAdmission.Api/**/*`
  - `backend/tests/SchoolAdmission.Tests/**/*`
- **Frontend**:
  - `frontend/**/*` (except `frontend/Dockerfile` and `frontend/.dockerignore`)
- **QA Tester**:
  - `backend/tests/**/*` (collaboration with Backend), `scripts/test-e2e.sh`, `scripts/run-tests.sh`
- **Security**:
  - Validation reviews, security reports in `.agents/teamwork/security/`
