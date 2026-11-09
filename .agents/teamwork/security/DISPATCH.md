# Security Specialist Dispatch

## Role
Security Specialist

## Working Directory
`/home/ashiq/work/school-admission-system/.agents/teamwork/security`

## Assigned Responsibilities
1. Security Audit across codebase & configuration:
   - Input validation: exact 17-digit regex `^[0-9]{17}$` for `X-Student-Id`, rejection of non-digit/malformed values.
   - SQL injection & parameter safety: ensure all queries use parameterized Npgsql/EF queries, review raw SQL for `FOR UPDATE`.
   - Concurrency exploit analysis: verify no race conditions in hold duration, expiry, double-spending or seat theft.
   - Information disclosure prevention: verify no stack traces or server internals leak in 4xx/5xx responses.
   - CORS validation: verify origin restrictions in backend configuration.
   - Secret & configuration audit: verify database credentials and passwords are safe and configurable.
2. Review implementation and report findings, risks, and remediations in `SECURITY_AUDIT.md`.
3. Write `handoff.md` with audit verdicts.

## References
- `/home/ashiq/work/school-admission-system/PLAN.md`
- `/home/ashiq/work/school-admission-system/.agents/teamwork/PROJECT.md`
- `/home/ashiq/work/school-admission-system/.agents/teamwork/ORIGINAL_REQUEST.md`

## 2026-11-09T03:53:43Z
You are the Security Specialist for the School Admission System project.
Your working directory is: /home/ashiq/work/school-admission-system/.agents/teamwork/security
Read the following files before starting:
- /home/ashiq/work/school-admission-system/.agents/teamwork/ORIGINAL_REQUEST.md
- /home/ashiq/work/school-admission-system/PLAN.md
- /home/ashiq/work/school-admission-system/.agents/teamwork/PROJECT.md
- /home/ashiq/work/school-admission-system/.agents/teamwork/security/DISPATCH.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Your exclusive responsibilities:
1. Conduct a comprehensive security review and verification of both backend and frontend codebases as they are developed:
   - Input validation: verify strict 17-digit regex `^[0-9]{17}$` for `X-Student-Id` header, boundary checks for class creation (`name` 1-120 chars, `totalSeats` positive integer <= 100,000).
   - SQL Injection & ORM safety: inspect queries in `ClassService`, `HoldService`, `AppDbContext`. Verify parameterization of all dynamic inputs, especially in raw SQL row locking.
   - Concurrency & Race Condition Vulnerabilities: check for TOCTOU (time-of-check to time-of-use), clock drift races, double-spend of seats, hold duration manipulation.
   - Information Disclosure: verify no stack traces, database error messages, or internal server paths leak in HTTP responses (RFC 9457 ProblemDetails global handler).
   - CORS & Transport Security: verify allowed origins, HTTP header hygiene, and credentials handling.
   - Container & Infrastructure Security: non-root user execution in Dockerfiles, sensitive file exclusion in `.dockerignore` and `.gitignore`.
2. Produce a thorough `SECURITY_AUDIT.md` in your working directory with detailed findings, risk assessments, and remediation verifications.
3. Periodically update your `progress.md` in your working directory.
4. When finished, write `handoff.md` with final security verdict and message parent.


## 2026-11-09T04:04:44Z
**Context**: Post-restart resumption of Security review
**Content**: A brief system restart occurred and all subagents are resuming. Please continue your comprehensive security audit of backend, frontend, database, and docker configs (17-digit validation, parameterized queries, row-locking race conditions, RFC 9457 error disclosure, and non-root execution). Prepare `SECURITY_AUDIT.md` and `handoff.md`.
**Action**: Resume audit activities and report findings when ready.
