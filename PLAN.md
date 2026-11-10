# PLAN.md — School Admission System

> Planning document only. No code yet.

---

## 0. How to read this plan

| Section | Purpose |
|---|---|
| 1 | What is being built and how it will be exercised |
| 2 | Tech stack with reasons |
| 3 | Repo layout |
| 4 | The hard part: concurrency & expiry design |
| 5 | Data model |
| 6 | API contract and behaviour rules |
| 7 | Backend architecture |
| 8 | Frontend architecture |
| 9 | Bangladesh-specific localisation |
| 10 | Docker and runtime configuration |
| 11 | Testing (minimal) |

---

## 1. Goals and priorities

### 1.1 How the system will be exercised
1. Run `docker compose up` on a machine that has only Docker.
2. Click through the UI (create class → apply → pay → cancel → expire).
3. Run **automated tests directly against the API** at `http://localhost:8080`, so the contract must match **exactly** (paths, field names, status values, status codes).
4. Hammer the seat-hold endpoint concurrently and check `held + confirmed <= totalSeats`.
5. Restart with `docker compose down && docker compose up` and check data survived.
6. Read the project docs, commit history and tests.

### 1.2 Priority order
1. **Correctness under concurrency** (never oversell a seat).
2. **Exact API contract.**
3. **One-command Docker startup + persistence.**
4. **UI requirements 1–5** (expiry without reload, polling, persistence across refresh/browser close, friendly errors).
5. Tests, docs, clean history.
6. Bonus (multi-process safety). It falls out of the chosen design almost for free.
7. Visual polish (explicitly not required).

### 1.3 Non-goals
- Authentication/authorization (no login, by design), real payment gateway, email/SMS, admin editing/deleting classes, pagination, multi-tenancy.

---

## 2. Tech stack

### 2.1 Backend
| Concern | Choice | Reason |
|---|---|---|
| Runtime | **.NET 10 (LTS)**, ASP.NET Core **Minimal APIs** with endpoint groups | Any .NET 8+ works; .NET 10 LTS chosen. Minimal APIs keep the contract obvious and the code small. |
| DB | **PostgreSQL 18** | Row-level locks, partial unique indexes, `clock_timestamp()`. |
| ORM | **EF Core 10** + **Npgsql EF provider (10.x)** | Migrations, typed queries. Raw SQL only for the lock query. |
| Naming | snake_case columns via `EFCore.NamingConventions` (if it supports EF 10 at scaffold time; otherwise map explicitly) | Matches your usual convention. |
| Validation | **FluentValidation** (or hand-written validators + endpoint filter) | Predictable 400 responses. |
| Errors | **RFC 9457 ProblemDetails** + stable machine-readable `code` extension | UI maps `code` → Bangla message. |
| API docs | Built-in **Microsoft.AspNetCore.OpenApi** document + **Swagger UI** (or Scalar as fallback) | Swashbuckle is no longer in the default template; verify package compatibility with .NET 10 at install time. |
| Background work | `BackgroundService` + `PeriodicTimer` | Expiry sweeper (cosmetic, not correctness-critical). |
| Logging | Built-in `ILogger`, JSON console output (Serilog optional) | Container-friendly. |
| Health | `/health/live`, `/health/ready` (checks DB) | Compose healthchecks. |
| Tests | **xUnit** + `WebApplicationFactory` + real PostgreSQL | Concurrency tests must hit real Postgres, never in-memory. |

> Pin exact package versions when scaffolding. I have not verified current minor versions, so run `dotnet list package --outdated` / `npm outdated` on day 1.

### 2.2 Frontend
| Concern | Choice | Reason |
|---|---|---|
| Build/dev server | **Vite** (current major) + **React 19** + **TypeScript** (strict), run on **Node 26** | Chosen stack. The Vite dev server is what runs inside Docker (see §10). |
| Routing | **React Router 7** (declarative/data router mode, SPA) | Chosen stack. |
| Client state | **Zustand 5** | Student ID, language, UI flags. `persist` middleware to `localStorage`. |
| Server state | **TanStack Query v5** | Polling, caching, retries, invalidation. Rule: *server data → TanStack Query; client-only data → Zustand.* |
| HTTP | **Axios** | Instance with base URL, `X-Student-Id` interceptor, error normaliser. |
| Forms/validation | **React Hook Form + Zod 4** | Matches your Zod preference; schemas shared by forms. |
| UI library | **HeroUI v3** (`@heroui/react` + `@heroui/styles`) on **Tailwind CSS v4** | Built on React Aria (accessible) and made for React 19. v3 needs no provider and no Framer Motion. Use its Table, Card, Button, Input, Modal and Alert (plus toast if the installed version has it). In the main CSS import `tailwindcss` first, then `@heroui/styles`. Do **not** use v2, which needs the old Tailwind plugin setup. |
| i18n | **react-i18next** (bn default, en secondary) | Bangladesh context, see §9. |
| Font | `@fontsource-variable/noto-sans-bengali` (self-hosted) | Works offline inside Docker; no Google Fonts call. |
| Lint/format | ESLint + Prettier, `@/` alias → `src/` | Your usual convention. |

### 2.3 Infrastructure
- **Docker** multi-stage builds, **Docker Compose v2** (`docker compose`, no `version:` key).
- Images: `mcr.microsoft.com/dotnet/sdk:10.0` (backend build), `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` (backend runtime, includes busybox `wget` for healthchecks), **`node:26-alpine`** (frontend, runs the Vite dev server), `postgres:18-alpine`.
- No nginx and no custom entrypoint scripts. Keep Docker as simple as possible.
- **PostgreSQL 18 image gotcha:** the volume must mount at `/var/lib/postgresql` (not `/var/lib/postgresql/data` as in ≤17). Getting this wrong can break persistence or startup.

---

## 3. Repository layout

```
school-admission/
├─ backend/
│  ├─ Dockerfile
│  ├─ .env              (backend + database configuration)
│  ├─ .env.example
│  ├─ SchoolAdmission.sln
│  ├─ src/
│  │  └─ SchoolAdmission.Api/
│  │     ├─ Program.cs
│  │     ├─ Endpoints/        (classes, holds, me)
│  │     ├─ Domain/           (entities, enums, errors)
│  │     ├─ Application/      (services: ClassService, HoldService)
│  │     ├─ Infrastructure/   (DbContext, migrations, seeder, config, sweeper)
│  │     ├─ Contracts/        (request/response DTOs)
│  │     └─ Validation/
│  └─ tests/
│     └─ SchoolAdmission.Tests/
├─ frontend/
│  ├─ Dockerfile
│  ├─ .dockerignore
│  ├─ .env              (VITE_API_BASE_URL)
│  ├─ .env.example
│  ├─ index.html
│  └─ src/
│     ├─ app/            (router, providers, layout)
│     ├─ features/       (classes, applications, admin, student-id)
│     ├─ lib/            (axios, query client, time/clock)
│     ├─ stores/         (zustand)
│     ├─ i18n/           (bn.json, en.json)
│     └─ components/
├─ docker-compose.yml
├─ .gitignore          (must ignore private docs/notes)
├─ README.md
├─ ASSUMPTIONS.md
├─ AI_USAGE.md
└─ PLAN.md
```

> Keep private documents and notes out of the repo: add them to `.gitignore` before the first commit.

---

## 4. Core design: concurrency and expiry

This is the core technical challenge.

### 4.1 Invariants (must hold at every instant, across any number of processes)
- **I1:** for each class, `active_held + confirmed <= total_seats`.
- **I2:** a student has at most one `HELD`(unexpired) or `CONFIRMED` application per class.
- **I3:** confirming the same hold N times increments `confirmedSeats` exactly once.
- **I4:** an expired hold frees its seat with no API call and no dependency on a background job.

### 4.2 Key idea: expiry is *derived*, not *mutated*
A hold counts toward `heldSeats` only if `status = 'HELD' AND expires_at > now`. A hold whose time has passed stops counting the instant the clock passes `expires_at`, whether or not any job has updated its row. The background sweeper only tidies `status` from `HELD` to `EXPIRED` for cleanliness. **Every read also reports `EXPIRED` for a stale `HELD` row**, so API output is right even if the sweeper is late.

This makes I4 true by construction and avoids a whole class of timer bugs, including lost timers after restart.

### 4.3 Serialising seat-affecting operations: per-class row lock
For **apply**, **confirm** and **cancel**, run one short transaction:

1. `BEGIN` (Read Committed).
2. `SELECT id, total_seats, clock_timestamp() AS now FROM classes WHERE id = @id FOR UPDATE;`
   - Takes a row lock on that class → all seat-affecting work for the class is serialised, **across processes**.
   - The timestamp is read **after** the lock is acquired, from the **database clock**.
3. Do the work (details in §6.3).
4. `COMMIT`.

Why this works:
- Transactions on *different* classes never block each other.
- Within a class the critical section is a few indexed statements, so throughput is fine even for thousands of simultaneous applicants (queued, not failed).
- Using the DB clock *inside* the lock removes the subtle race I found while designing this: if `now` were taken before the lock (or from the app server), a confirm computed at `t2 < expires_at` could commit after an apply that saw the same hold as expired at `t1 > expires_at`, granting the same seat twice. With lock-then-read-clock, whoever gets the lock second observes the first one's committed result and a later clock reading. It also makes the two-process bonus safe despite clock skew between app instances.

### 4.4 Defence in depth (DB-level guards)
- `CHECK (total_seats > 0)`.
- `CHECK (student_id ~ '^[0-9]{17}$')`.
- **Partial unique index** on `(student_id) WHERE status IN ('HELD','CONFIRMED')` (I2 backstop) — one active application per student across the whole system. To make this compatible with derived expiry, the apply transaction first flips this student's stale-`HELD` rows for that class to `EXPIRED` (inside the lock) before inserting.
- A violation of this index is mapped to the same `409` as the application-level check, so even a logic bug cannot create a duplicate.

### 4.5 Alternatives considered (to put in ASSUMPTIONS.md)
| Option | Verdict |
|---|---|
| In-process `lock`/`SemaphoreSlim` | Fails the bonus (two processes). Rejected. |
| Optimistic concurrency (`xmin`/row version) with retry | Retry storms under heavy contention; more complex. Rejected. |
| Atomic counter `UPDATE ... WHERE held+confirmed < total` | Fast, but expiry needs a reaper to decrement counters, so correctness depends on a job. Rejected. |
| `SERIALIZABLE` + retry | Works, but serialisation failures leak into client-visible latency. Rejected. |
| Redis lock/queue | Extra infra, extra failure modes, not needed. Rejected. |
| **Row lock on class + derived expiry (chosen)** | Simple, correct, multi-process safe, restart safe. |

### 4.6 Expiry sweeper
- `BackgroundService` with `PeriodicTimer` (e.g. every 5 s): `UPDATE holds SET status='EXPIRED', updated_at=... WHERE status='HELD' AND expires_at <= clock_timestamp()`.
- Idempotent, no lock needed (it only changes the label of rows that already don't count), safe with multiple instances running.

### 4.7 Restart safety
- All state lives in Postgres on a named volume. Nothing in memory is authoritative.
- Migrations on startup (see §10.4).

---

## 5. Data model (PostgreSQL, snake_case)

### 5.1 `classes`
| column | type | notes |
|---|---|---|
| id | uuid PK | UUIDv7 (`Guid.CreateVersion7()`), time-ordered |
| name | varchar(120) NOT NULL | trimmed; duplicates allowed |
| total_seats | integer NOT NULL | `CHECK > 0` |
| created_at | timestamptz NOT NULL | |

### 5.2 `holds`
| column | type | notes |
|---|---|---|
| id | uuid PK | UUIDv7 |
| class_id | uuid FK → classes | `ON DELETE RESTRICT` |
| student_id | char(17) NOT NULL | digits only |
| status | text NOT NULL | `HELD` / `CONFIRMED` / `EXPIRED` / `CANCELLED` (string-converted enum, with a `CHECK`) |
| created_at | timestamptz NOT NULL | |
| expires_at | timestamptz NOT NULL | set from DB clock at creation |
| confirmed_at | timestamptz NULL | |
| cancelled_at | timestamptz NULL | |
| updated_at | timestamptz NOT NULL | |

### 5.3 Indexes
- `ix_holds_class_status_expires (class_id, status, expires_at)` → seat counting.
- `ix_holds_student_created (student_id, created_at DESC)` → `/me/holds`.
- `ux_holds_active_student (student_id) WHERE status IN ('HELD','CONFIRMED')` (unique, partial) — enforces one active application per student.

### 5.4 Seat counts (computed, never stored)
```
heldSeats      = count(holds where status='HELD' and expires_at > now)
confirmedSeats = count(holds where status='CONFIRMED')
availableSeats = totalSeats - heldSeats - confirmedSeats      (never negative)
```
`GET /classes` uses a **single aggregate query** (`LEFT JOIN` + `GROUP BY`, or conditional counts), not N+1.

---

## 6. API specification and behaviour

Base URL `http://localhost:8080`. JSON everywhere. Routes follow the specified contract exactly (no `/api` prefix). IDs are opaque strings (UUIDv7). Extra response fields are allowed but I will add very few, and never rename or drop required ones.

### 6.1 Endpoint summary
| # | Method & path | Header | Success | Notable errors |
|---|---|---|---|---|
| 1 | `POST /classes` | – | 201 `{id,name,totalSeats}` | 400 validation |
| 2 | `GET /classes` | – | 200 array with seat counts | – |
| 3 | `GET /classes/{classId}` | – | 200 | 404 `CLASS_NOT_FOUND` |
| 4 | `POST /classes/{classId}/holds` | `X-Student-Id` | 201 `{holdId,classId,studentId,status,expiresAt}` | 400, 404, **409** `SEATS_FULL`, **409** `ALREADY_HELD` / `ALREADY_CONFIRMED` |
| 5 | `GET /holds/{holdId}` | – | 200 `{holdId,classId,status,expiresAt}` | 404 `HOLD_NOT_FOUND` |
| 6 | `POST /holds/{holdId}/confirm` | `X-Student-Id` | 200 `{holdId,status:"CONFIRMED"}` | 400, 403, 404, 409 `HOLD_EXPIRED` / `HOLD_CANCELLED` |
| 7 | `DELETE /holds/{holdId}` | `X-Student-Id` | 204 | 400, 403, 404, 409 `HOLD_ALREADY_CONFIRMED` / `HOLD_EXPIRED` |
| 8 | `GET /me/holds` | `X-Student-Id` | 200 array | 400 |

Also: `GET /health/live`, `GET /health/ready`, `/swagger`.

### 6.2 Validation rules
- `X-Student-Id`: required where listed; must match `^[0-9]{17}$` (ASCII digits). Otherwise `400 INVALID_STUDENT_ID`.
  - **Intentionally not checking the year prefix or any registration-office structure.** Automated API tests may use arbitrary 17-digit values, and being stricter than the spec is a risk with no upside. (UI-side digit normalisation is described in §9.)
- `POST /classes`: `name` required, trimmed, 1–120 chars; `totalSeats` integer, `1 ≤ n ≤ 100000`. Non-integers/strings → 400 (no silent coercion). Unknown JSON fields ignored.
- Route ids that aren't valid GUIDs → `404` (not 500).
- Malformed JSON → 400 ProblemDetails.

### 6.3 Behaviour of the three state-changing operations (inside the §4.3 transaction)

**Apply (`POST /classes/{id}/holds`)**
1. Lock the class row; read DB `now`. Class missing → 404.
2. Mark this student's stale `HELD` rows in this class as `EXPIRED` (those with `expires_at <= now`).
3. If student has a `CONFIRMED` row here → `409 ALREADY_CONFIRMED`; if an unexpired `HELD` row → `409 ALREADY_HELD`.
4. Count active held + confirmed; if `>= total_seats` → `409 SEATS_FULL`.
5. Insert `HELD` with `expires_at = now + HOLD_DURATION_SECONDS`. Return 201.

**Confirm (`POST /holds/{id}/confirm`)**
1. Load hold → 404 if missing. Lock its class row; read DB `now`; re-read hold.
2. Different student → `403 NOT_HOLD_OWNER`.
3. `CONFIRMED` already → **200 `CONFIRMED` (idempotent, no change)**.
4. `HELD` and `expires_at > now` → set `CONFIRMED`, `confirmed_at = now`. 200.
5. `HELD` but expired → mark `EXPIRED`, `409 HOLD_EXPIRED`. `EXPIRED` → same 409. `CANCELLED` → `409 HOLD_CANCELLED`.

**Cancel (`DELETE /holds/{id}`)**
1. Same load/lock/ownership check.
2. `HELD` (expired or not) → `CANCELLED` (or leave `EXPIRED` if already past, but still return 204 so the UI is simple). 204.
3. Already `CANCELLED` → 204 (idempotent).
4. `CONFIRMED` → `409 HOLD_ALREADY_CONFIRMED` (refunds/withdrawals are out of scope).

> `403` vs `404` for someone else's hold is an assumption; document it. `GET /holds/{id}` needs no header per the spec; ids are unguessable UUIDs and the response contains no personal data (no `studentId`).

### 6.4 Error body format
```
{
  "type": "https://httpstatuses.com/409",
  "title": "Conflict",
  "status": 409,
  "code": "SEATS_FULL",
  "detail": "No seats are available in this class."
}
```
- `code` is the stable contract for the UI; `detail` is English, for developers.
- No stack traces ever leave the server (global exception handler → 500 ProblemDetails with a correlation id).

### 6.5 Time format
All timestamps are **UTC ISO-8601 with `Z`** (as in the spec's examples). Responses also carry the standard HTTP `Date` header, which the UI uses to estimate clock skew (§8.6).

---

## 7. Backend architecture

### 7.1 Layers (pragmatic, not over-engineered)
- **Endpoints** → thin; bind, validate, call service, map result.
- **Services** (`ClassService`, `HoldService`) → own the transactions and rules in §6.3.
- **Infrastructure** → `AppDbContext`, EF configurations, migrations, `ExpirySweeper`, startup migrator and `DemoDataSeeder`.
- **Contracts** → DTO records matching the spec literally (use `JsonPropertyName`/camelCase policy; enums serialised as upper-case strings).
- Result-style returns (`Result<T, ErrorCode>`) or domain exceptions mapped by a single exception handler → one place decides status codes.

### 7.2 Configuration (environment variables)
| Variable | Default | Notes |
|---|---|---|
| `ASPNETCORE_URLS` / `ASPNETCORE_HTTP_PORTS` | `8080` | |
| `ConnectionStrings__Default` (or `DATABASE_URL`-style) | compose value | Required: DB connection configurable via env var. |
| `HOLD_DURATION_SECONDS` | `600` | Validated on start (positive int); fail fast otherwise. |
| `CORS_ALLOWED_ORIGINS` | `http://localhost:3000` | Comma-separated. |
| `EXPIRY_SWEEP_INTERVAL_SECONDS` | `5` | |
| `SEED_DEMO_DATA` | `true` | Seeds demo classes only when the database has no classes (§10.4). Set `false` for a clean start. |
| `ASPNETCORE_ENVIRONMENT` | `Production` in compose | Swagger stays enabled regardless, for easy API exploration. |

### 7.3 Performance and robustness details
- Npgsql pool: set `Maximum Pool Size` sensibly (e.g. 50–100) and keep transactions short so lock queues drain quickly.
- `NoTracking` for reads; compiled/aggregate queries for `/classes`.
- `CommandTimeout`/lock timeout set (e.g. `SET LOCAL lock_timeout = '5s'`) → surfaces as `503` with a friendly code rather than hanging forever.
- Response compression optional; **no rate limiter** enabled by default (automated tests fire bursts).
- JSON: camelCase, UTC `DateTimeOffset`, reject nothing extra.

---

## 8. Frontend architecture

### 8.1 Routes (React Router)
| Path | Screen |
|---|---|
| `/` | Class list (default) |
| `/my-applications` | আমার আবেদন |
| `/admin/classes/new` | Create class (school admin) |
| `*` | Friendly "not found" page |

Top of every page: **Student ID bar** (the required input box at the top of the page) + language switch + nav.

### 8.2 Screens and behaviour
**Student ID bar**
- 17-digit input, numeric keypad hint (`inputMode="numeric"`), live counter "12/17", inline validation message.
- Saved to Zustand `persist` (localStorage) → survives refresh and browser close (requirement 4).
- Changing the ID changes all query keys → data for the new student loads.

**Class list**
- Table/cards: name, total, held, confirmed, available.
- "আবেদন করুন" button. Disabled (with reason tooltip/text) when no valid student ID, no seats, or the student already holds/has confirmed this class (derived from `/me/holds`). The server stays the source of truth, so 409s are still handled gracefully.
- Polls `GET /classes` every ~3 s (requirement 3) via TanStack Query `refetchInterval`; pauses when the tab is hidden and refetches on focus.
- Shows "last updated" indicator and a non-blocking "connection lost, retrying…" banner when polling fails.

**My applications**
- Rows: class name, status badge (HELD/CONFIRMED/EXPIRED/CANCELLED in Bangla), time remaining for HELD, actions.
- HELD: **"ভর্তি ফি দিন"** (calls confirm, "payment" is simulated) + **"আবেদন বাতিল করুন"** (with confirm dialog).
- **Live countdown** (1 s tick, `useSyncExternalStore`-style shared ticker or one `setInterval` in a hook). At zero: row flips to `EXPIRED` locally, pay button disabled, then an immediate refetch reconciles with the server (requirement 2, no reload).
- Polls `GET /me/holds` (every ~5 s) so changes made elsewhere show up.
- Empty state: "আপনার কোনো আবেদন নেই।" with link to the class list.

**Create class (admin)**
- Zod-validated form: name (1–120), total seats (positive integer). Success toast + redirect/link to list; field-level errors from 400s.

### 8.3 State management split
| Data | Where |
|---|---|
| Student ID, language | Zustand (persisted) |
| Class list, my holds | TanStack Query (cache keyed by studentId) |
| Mutations (apply/confirm/cancel/create) | TanStack Query mutations → `invalidateQueries` on success **and** on error (a 409 means data was stale) |
| Clock skew offset | Zustand/module singleton |

### 8.4 HTTP layer (Axios)
- One instance; `baseURL` from `import.meta.env.VITE_API_BASE_URL` (§10.5), timeout ~10 s.
- Request interceptor adds `X-Student-Id` when present.
- Response interceptor: record `Date` header → clock offset; normalise errors to `{ kind: 'api' | 'network' | 'timeout', code?, status?, message }`.
- No retries on POST/DELETE (avoid surprise duplicates; the server is idempotent where it matters anyway). GET retried by TanStack Query.

### 8.5 Error handling (requirement 5)
Central `errorToMessage(error)` maps `code` → i18n key:

| Situation | Bangla message (draft) |
|---|---|
| `SEATS_FULL` | "দুঃখিত, এই ক্লাসে আর কোনো আসন খালি নেই।" |
| `ALREADY_HELD` | "এই ক্লাসে আপনার একটি আবেদন ইতিমধ্যে চলমান আছে।" |
| `ALREADY_CONFIRMED` | "এই ক্লাসে আপনার ভর্তি ইতিমধ্যে নিশ্চিত হয়েছে।" |
| `HOLD_EXPIRED` | "আবেদনের মেয়াদ শেষ হয়ে গেছে। অনুগ্রহ করে নতুন করে আবেদন করুন।" |
| `INVALID_STUDENT_ID` | "জন্মনিবন্ধন নম্বর অবশ্যই 17 অঙ্কের হতে হবে।" |
| network/timeout | "সার্ভারের সাথে সংযোগ করা যাচ্ছে না। একটু পরে আবার চেষ্টা করুন।" |
| 5xx / unknown | "কিছু একটা সমস্যা হয়েছে। আবার চেষ্টা করুন।" |

Plus: a top-level React **Error Boundary** (never a blank page), loading skeletons, and empty states. Never rely on console-only errors. (Final wording to be reviewed by a native reader before release.)

### 8.6 Expiry accuracy
`remaining = expiresAt − (Date.now() + serverOffset)`, where `serverOffset` comes from the HTTP `Date` header. This keeps the countdown correct even if the parent's laptop clock is wrong, which is common. The server remains the final authority at confirm time.

### 8.7 UX details worth the few minutes
- Disable buttons while a mutation is pending (no double submit).
- Optimistic UI is **not** used for apply/confirm; wait for the server, because seats are contested.
- Accessible labels, focus states, `aria-live` for toasts and errors.

---

## 9. Bangladesh-specific considerations

1. **Language:** UI text in **Bangla (default)** with an **English** toggle (`react-i18next`, `i18n/bn.json` and `i18n/en.json`). The choice is saved in Zustand (`persist`). Button labels from the spec ("আবেদন করুন", "ভর্তি ফি দিন") are used verbatim.
2. **Digits are always English (0–9)** everywhere: seat counts, countdown, Student ID input, dates, and inside Bangla sentences (e.g. "17 অঙ্কের").
   - Numbers: `Intl.NumberFormat('en-US')` (or plain numbers).
   - Dates/times: Bangla month/day names but Latin digits via the locale `bn-BD-u-nu-latn`, with `timeZone: 'Asia/Dhaka'`. In English mode use `en-GB` with the same time zone.
   - The Student ID input accepts only `0-9`; any other typed character is stripped.
3. **Font:** Noto Sans Bengali, self-hosted through `@fontsource-variable/noto-sans-bengali`, so Bangla renders correctly with no external request.
4. **Birth registration number (জন্মনিবন্ধন নম্বর):** exactly 17 digits. Helper text explains this. No deeper validation (see §6.2 for why).
5. **Time zone:** store and transmit UTC; display in **`Asia/Dhaka` (UTC+6, no DST)**. The countdown is relative, so it is zone-independent.
6. **Terminology:** "Class" = class + shift (e.g. "Class 6 - Morning Shift", "Class 6 - Day Shift"). The create-class form hints at this naming.
7. **Fee:** the spec has no fee amount, so none is modelled. "ভর্তি ফি দিন" just calls confirm.
8. **Mobile-first layout:** many guardians will apply from phones.

---

## 10. Docker and runtime

### 10.1 Compose services
| Service | Image/build | Port | Notes |
|---|---|---|---|
| `db` | `postgres:18-alpine` | internal only | Named volume mounted at `/var/lib/postgresql`; `pg_isready` healthcheck |
| `backend` | `./backend` | **8080:8080** | `depends_on: db (service_healthy)`; healthcheck on `/health/ready` |
| `frontend` | `./frontend` | **3000:3000** | Vite dev server; `depends_on: backend (service_healthy)` |
| `backend-tests` *(profile `test`)* | backend SDK image | none | Runs the xUnit tests against the compose `db` |

### 10.2 Dockerfiles
- **Backend:** multi-stage (restore → publish → `aspnet:10.0-alpine`). Copy the `.csproj` first so the restore layer is cached. Non-root user, `ASPNETCORE_HTTP_PORTS=8080`.
- **Frontend (single stage, deliberately simple):**
  1. `FROM node:26-alpine`
  2. copy `package*.json` → `npm ci`
  3. copy the source
  4. `EXPOSE 3000`
  5. start the Vite dev server listening on all interfaces: `npm run dev -- --host 0.0.0.0 --port 3000`
- No nginx, no entrypoint scripts, no build-then-serve step. The trade-off (dev server instead of a production bundle, slower first load, bigger image) is accepted for simplicity, and mentioned in ASSUMPTIONS.md.
- `.dockerignore` for both (node_modules, bin, obj, .git).

### 10.3 Persistence
- Named volume for Postgres, so data survives `docker compose down`. Only `down -v` wipes it (README will say so).
- `restart: unless-stopped`.
- **PostgreSQL 18 image gotcha:** mount the volume at `/var/lib/postgresql`, not `/var/lib/postgresql/data`.

### 10.4 Migrations and seed data at startup
1. Backend starts → waits for the DB (retry with backoff) → takes a **Postgres advisory lock** → applies EF Core migrations → runs the seeder → releases the lock. The lock keeps this safe even if two backend processes start together (bonus scenario).
2. **Seeder rules:** runs only if `SEED_DEMO_DATA=true` (default) **and** the `classes` table is empty. Restarts never duplicate data.
3. **Seed content: 12 classes**, with some already-confirmed admissions so the seat columns look real from the first screen:

| # | Class name | Total seats | Seeded confirmed |
|---|---|---|---|
| 1 | Class 1 - Morning Shift | 60 | 12 |
| 2 | Class 2 - Morning Shift | 60 | 20 |
| 3 | Class 3 - Morning Shift | 60 | 0 |
| 4 | Class 4 - Day Shift | 50 | 10 |
| 5 | Class 5 - Morning Shift | 80 | 30 |
| 6 | Class 6 - Morning Shift | 120 | 80 |
| 7 | Class 6 - Day Shift | 100 | 45 |
| 8 | Class 7 - Morning Shift | 100 | 0 |
| 9 | Class 8 - Day Shift | 90 | 60 |
| 10 | Class 9 - Morning Shift | 80 | 79 (almost full) |
| 11 | Class 10 - Morning Shift | 70 | 70 (full, to see the "no seats" state) |
| 12 | Demo - Only 2 Seats | 2 | 0 (quick way to test full-seat and concurrency by hand) |

4. Seeded confirmed admissions use fake student IDs in a reserved range: `9999` + a 13-digit running number (e.g. `99990000000000001`). They are real `CONFIRMED` rows, so the invariant `held + confirmed <= total` holds for seed data too.
5. README will list 3 sample Student IDs to type in (outside the reserved range, e.g. `20150000000000001`, `20150000000000002`, `20150000000000003`).

### 10.5 Frontend → backend URL
- Compose sets `VITE_API_BASE_URL=http://localhost:8080` as an environment variable on the `frontend` service.
- The Vite dev server reads it when it starts, so changing it and running `docker compose up` again is enough. No rebuild, no extra scripts.
- It must be a URL the **browser** can reach (`http://localhost:8080`), **not** the Docker-internal `http://backend:8080`.
- Because the browser calls the backend directly, the backend sends **CORS** headers for `http://localhost:3000` (`CORS_ALLOWED_ORIGINS`).

### 10.6 Per-service env files
Each app gets its own env file, loaded by Compose through `env_file:`:

- `backend/.env` — `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_HTTP_PORTS`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, `ConnectionStrings__Default`, `HOLD_DURATION_SECONDS`, `EXPIRY_SWEEP_INTERVAL_SECONDS`, `SEED_DEMO_DATA`, `CORS_ALLOWED_ORIGINS`.
- `frontend/.env` — `VITE_API_BASE_URL`.

Both files are committed with working defaults (plus `.env.example` templates), so `docker compose up` needs **no manual setup**. A host shell variable of the same name overrides the file for a single run.

---

## 11. Testing (minimal)

Requirement: **at least 3 automated backend tests, one of them a concurrency test**. Nothing else is required, so there are no frontend tests, no CI and no load scripts.

### 11.1 The tests (5, backend only)
Tools: **xUnit** + `WebApplicationFactory` + the real PostgreSQL from compose (a throwaway database per test class). The expiry test uses `HOLD_DURATION_SECONDS=2`.

| # | Test | What it proves |
|---|---|---|
| 1 | **Concurrency: no overselling.** Class with 20 seats, 200 parallel `POST /classes/{id}/holds` with 200 different student IDs | Exactly 20 × `201` and 180 × `409`; `GET /classes/{id}` shows `heldSeats=20`, `availableSeats=0` |
| 2 | **Concurrency: one active application per student.** Same student sends 30 parallel applies to one class | Exactly one `201`, the rest `409` |
| 3 | **Concurrency: idempotent confirm.** 20 parallel confirms of one hold | All return `200 CONFIRMED`; `confirmedSeats` is exactly 1 |
| 4 | **Expiry.** Apply, wait past the duration, make no other API call | Seat is available again, `GET /holds/{id}` says `EXPIRED`, confirm returns `409` |
| 5 | **Core flow and errors.** Create class → apply → confirm → `/me/holds`; also cancel frees the seat, bad `X-Student-Id` gives `400`, full class gives `409` | Contract field names, status values and status codes match the spec |

### 11.2 How to run
```
docker compose --profile test run --rm backend-tests
```
This only needs Docker, the same as the rest of the project. README will document it.

> The optional bonus (two backend processes on one database) is **not** automatically tested. The design guarantees it (row lock + database clock + partial unique index), and ASSUMPTIONS.md explains how.