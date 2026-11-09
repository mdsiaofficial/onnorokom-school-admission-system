## 2026-11-09T04:38:50Z
You are the Forensic Auditor for the School Admission System project.
Your working directory is: /home/ashiq/work/school-admission-system/.agents/teamwork/auditor_1
Read the following files before starting:
- /home/ashiq/work/school-admission-system/.agents/teamwork/ORIGINAL_REQUEST.md
- /home/ashiq/work/school-admission-system/PLAN.md
- /home/ashiq/work/school-admission-system/.agents/teamwork/PROJECT.md
- Subagent handoffs:
  - /home/ashiq/work/school-admission-system/.agents/teamwork/devops/handoff.md
  - /home/ashiq/work/school-admission-system/.agents/teamwork/backend/handoff.md
  - /home/ashiq/work/school-admission-system/.agents/teamwork/frontend/handoff.md
  - /home/ashiq/work/school-admission-system/.agents/teamwork/security/handoff.md
  - /home/ashiq/work/school-admission-system/.agents/teamwork/tester/handoff.md

Your mission:
Perform a comprehensive forensic integrity audit of the entire codebase and test suite:
1. Anti-cheating verification: Verify that NO implementations are dummy, facade, mock, or hardcoded to pass tests. Verify that business logic, seat calculations, row-locking queries, and persistence are 100% genuine.
2. Concurrency mechanism inspection: Inspect `HoldService.cs` to verify that `SELECT id, total_seats, clock_timestamp() AS now FROM classes WHERE id = @id FOR UPDATE;` is genuinely executed and that the database clock is used.
3. Database & Invariant inspection: Verify partial unique index `ux_holds_active_student_class`, check constraints, and Postgres 18 volume mount `/var/lib/postgresql`.
4. API Contract & RFC 9457 inspection: Verify all 8 endpoints match exact paths, methods, field names, casing, and error structures.
5. Frontend inspection: Verify React 19 app, 17-digit student header, 1-second countdown ticker with dynamic status change without page reload, and Bangla/English localization.
6. Docker & Persistence verification: Verify `docker compose config`, `docker compose up` readiness, and persistence across restart.
7. Produce a comprehensive audit verdict in `/home/ashiq/work/school-admission-system/.agents/teamwork/auditor_1/handoff.md` with binary verdict: `CLEAN` or `INTEGRITY VIOLATION`. Report to parent orchestrator.
