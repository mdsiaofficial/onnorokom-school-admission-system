# Original User Request

## Initial Request — 2026-11-09T03:49:41Z

Invoke a team of 5 subagents: frontend, backend, devops, tester, security. Read /home/ashiq/work/school-admission-system/PLAN.md, cross-match against the requirements document to ensure all requirements are present, delegate work between the subagents, and collaboratively and concurrently build the complete school admission system.

Working directory: /home/ashiq/work/school-admission-system
Integrity mode: development

## Reference Material
- Planning document: `/home/ashiq/work/school-admission-system/PLAN.md`
- Requirements brief: `/home/ashiq/work/school-admission-system/2026-11-08-assignment-brief-assistant-se-bangla_3e17ac4e-5f43-4bc2-8aa9-0c3f887f82d5.md`

## Requirements

### R1. Requirements Analysis & Plan Validation
Analyze `/home/ashiq/work/school-admission-system/PLAN.md` and cross-match it against `/home/ashiq/work/school-admission-system/2026-11-08-assignment-brief-assistant-se-bangla_3e17ac4e-5f43-4bc2-8aa9-0c3f887f82d5.md` to ensure all functional, API contract, and technical requirements are accounted for.

### R2. Concurrent Multi-Agent Delegation & Implementation
Delegate work among the 5 subagent roles (frontend, backend, devops, tester, security) to collaboratively and concurrently build the complete School Admission System based on PLAN.md and the requirement brief.

### R3. Docker & Infrastructure Integration
Deliver a containerized setup where PostgreSQL, the backend API, and the frontend run via Docker Compose, supporting one-command startup and persistence across container restarts.

### R4. Core Concurrency & Business Rules
Implement seat-hold, payment confirmation, cancellation, and seat expiry semantics such that seat allocations are strictly enforced under concurrency without overselling.

### R5. Automated Testing & Security Verification
Implement and execute automated test suites covering API contract validation and concurrency verification against running services, along with security verification.

## Acceptance Criteria

### Plan & Requirements Cross-Match
- [ ] All requirements in `2026-11-08-assignment-brief-assistant-se-bangla_3e17ac4e-5f43-4bc2-8aa9-0c3f887f82d5.md` are cross-referenced with `PLAN.md`, with any identified gaps documented or addressed.

### Docker & Infrastructure
- [ ] Running `docker compose up` starts PostgreSQL, the backend API at `http://localhost:8080`, and the frontend UI without manual intervention.
- [ ] Stopping and restarting the stack via `docker compose down && docker compose up` preserves database records and application state.
- [ ] Database connection settings and frontend-to-backend API URL are configurable via environment variables.

### API Contract & Concurrency
- [ ] Backend exposes all required endpoints with exact matching paths, status codes, field names, and error formats specified in PLAN.md and the assignment brief.
- [ ] Concurrent hold requests hitting the seat-hold endpoint never allow `held + confirmed` seats to exceed `totalSeats`.
- [ ] Seat hold expiration and cancellation correctly return held seats to the available pool.

### Frontend Application
- [ ] Frontend allows admin class creation, displays class list with live counts (total, held, confirmed, available), and allows seat applications using a 17-digit student identifier header.
- [ ] UI provides student application status view ("My Applications"), payment confirmation flow, cancellation, and handles seat expiry dynamically without requiring a full page reload.

### Automated Testing & Security
- [ ] Automated integration and concurrency test suites run against the API and pass.
- [ ] Core input validation and security practices are verified across frontend, backend, and infrastructure configuration.
