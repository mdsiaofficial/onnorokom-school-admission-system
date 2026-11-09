# QA Test Engineer Dispatch

## Role
QA Test Engineer

## Working Directory
`/home/ashiq/work/school-admission-system/.agents/teamwork/tester`

## Assigned Responsibilities
1. Automated Test Suite in `backend/tests/SchoolAdmission.Tests/` using xUnit:
   - Concurrency Test 1 (Overselling Prevention): Class with 20 seats, 200 concurrent requests from 200 distinct student IDs -> exactly 20 successful (201), 180 rejected (409), heldSeats=20, availableSeats=0.
   - Concurrency Test 2 (Single Active Application): 30 concurrent applies from the same student ID to the same class -> exactly 1 success (201), 29 rejected (409).
   - Concurrency Test 3 (Idempotent Confirm): 20 concurrent confirms on the same hold -> all return 200 CONFIRMED, confirmedSeats increments by 1 only.
   - Expiry Test: Apply hold with short duration, wait past duration without making API calls -> verify seat is available again and hold returns EXPIRED on query.
   - API Contract Test: Validate all 8 endpoints, status codes, field names, response structures, and error formats (RFC 9457 ProblemDetails).
2. Standalone integration / container test script:
   - Create `scripts/test-e2e.sh` or Docker test profile service `backend-tests` to run tests against the running compose stack.
3. Verify test runs and report all results in `handoff.md`.

## References
- `/home/ashiq/work/school-admission-system/PLAN.md` (§11)
- `/home/ashiq/work/school-admission-system/.agents/teamwork/PROJECT.md`
- `/home/ashiq/work/school-admission-system/.agents/teamwork/ORIGINAL_REQUEST.md`


## 2026-11-09T03:53:36Z
You are the QA Test Engineer for the School Admission System project.
Your working directory is: /home/ashiq/work/school-admission-system/.agents/teamwork/tester
Read the following files before starting:
- /home/ashiq/work/school-admission-system/.agents/teamwork/ORIGINAL_REQUEST.md
- /home/ashiq/work/school-admission-system/PLAN.md (especially §1.1, §6, §11)
- /home/ashiq/work/school-admission-system/.agents/teamwork/PROJECT.md
- /home/ashiq/work/school-admission-system/.agents/teamwork/tester/DISPATCH.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Your exclusive responsibilities:
1. Implement the xUnit test suite in `backend/tests/SchoolAdmission.Tests/`:
   - Concurrency Test 1 (No Overselling): Class with 20 seats subjected to 200 concurrent `POST /classes/{id}/holds` from 200 distinct student IDs -> exactly 20 return 201, 180 return 409; `GET /classes/{id}` confirms `heldSeats=20` and `availableSeats=0`.
   - Concurrency Test 2 (Single Active Application): 30 parallel requests from the SAME student ID for the SAME class -> exactly 1 returns 201, 29 return 409.
   - Concurrency Test 3 (Idempotent Confirm): 20 concurrent `POST /holds/{id}/confirm` on the same hold -> all return 200 `CONFIRMED`, `confirmedSeats` increments by 1 only.
   - Expiry Test: Create hold with short duration (e.g. 2s), wait 3s with NO API calls -> seat is returned to available pool, `GET /holds/{id}` reports `EXPIRED`, and confirm returns 409 `HOLD_EXPIRED`.
   - API Contract Test: Exercise all 8 endpoints (`POST /classes`, `GET /classes`, `GET /classes/{id}`, `POST /classes/{id}/holds`, `GET /holds/{id}`, `POST /holds/{id}/confirm`, `DELETE /holds/{id}`, `GET /me/holds`) verifying exact paths, status codes, field names, and RFC 9457 ProblemDetails error format.
2. Provide a standalone test runner script or container test runner (e.g. `scripts/run-tests.sh` or Docker test profile `backend-tests`) that can be executed against a running compose stack.
3. Coordinate with Backend engineer if test dependencies or fixtures are required.
4. Periodically update your `progress.md` in your working directory.
5. Run tests, document all results and command outputs in `handoff.md`, and message parent.


## 2026-11-09T04:00:31Z
**Context**: Backend Dockerfile stage name feedback
**Content**: Thank you for catching this issue. I have forwarded the defect to DevOps (`d17c51f2-5137-456c-b0a5-fe71e0a4f2dc`) for immediate fix in `backend/Dockerfile`.
**Action**: Continue setting up and verifying the test suite.

## 2026-11-09T04:05:10Z
You are the QA Test Engineer for the School Admission System project.
Your working directory is: /home/ashiq/work/school-admission-system/.agents/teamwork/tester
Read the following files before starting:
- /home/ashiq/work/school-admission-system/.agents/teamwork/ORIGINAL_REQUEST.md
- /home/ashiq/work/school-admission-system/PLAN.md (especially §1.1, §6, §11)
- /home/ashiq/work/school-admission-system/.agents/teamwork/PROJECT.md
- /home/ashiq/work/school-admission-system/.agents/teamwork/tester/DISPATCH.md
- /home/ashiq/work/school-admission-system/.agents/teamwork/tester/progress.md

Note: DevOps has already fixed line 39 in `backend/Dockerfile` to `COPY --from=build /app/publish .`.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Your exclusive responsibilities:
1. Implement the xUnit test suite in `backend/tests/SchoolAdmission.Tests/`:
   - Concurrency Test 1 (No Overselling): Class with 20 seats subjected to 200 concurrent `POST /classes/{id}/holds` from 200 distinct student IDs -> exactly 20 return 201, 180 return 409; `GET /classes/{id}` confirms `heldSeats=20` and `availableSeats=0`.
   - Concurrency Test 2 (Single Active Application): 30 parallel requests from the SAME student ID for the SAME class -> exactly 1 returns 201, 29 return 409.
   - Concurrency Test 3 (Idempotent Confirm): 20 concurrent `POST /holds/{id}/confirm` on the same hold -> all return 200 `CONFIRMED`, `confirmedSeats` increments by 1 only.
   - Expiry Test: Create hold with short duration (e.g. 2s), wait 3s with NO API calls -> seat is returned to available pool, `GET /holds/{id}` reports `EXPIRED`, and confirm returns 409 `HOLD_EXPIRED`.
   - API Contract Test: Exercise all 8 endpoints (`POST /classes`, `GET /classes`, `GET /classes/{id}`, `POST /classes/{id}/holds`, `GET /holds/{id}`, `POST /holds/{id}/confirm`, `DELETE /holds/{id}`, `GET /me/holds`) verifying exact paths, status codes, field names, and RFC 9457 ProblemDetails error format.
2. Provide a standalone test runner script or container test runner (e.g. `scripts/run-tests.sh` or Docker test profile `backend-tests`) that can be executed against a running compose stack.
3. Coordinate with Backend engineer if test dependencies or fixtures are required.
4. Periodically update your `progress.md` in your working directory.
5. Run tests, document all results and command outputs in `handoff.md`, and message parent.
