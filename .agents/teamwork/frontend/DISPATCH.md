# Frontend Engineer Dispatch

## Role
Frontend Engineer

## Working Directory
`/home/ashiq/work/school-admission-system/.agents/teamwork/frontend`

## Assigned Responsibilities
1. Scaffold React 19 + TypeScript + Vite app in `frontend/`:
   - Configurable `VITE_API_BASE_URL` (default `http://localhost:8080`)
   - Styling with Tailwind CSS
   - State management with Zustand (persisted `studentId`, language)
   - Server state with TanStack Query (caching, polling, invalidation)
   - Axios instance with `X-Student-Id` interceptor and clock skew tracking
   - i18n with react-i18next (Bangla default, English toggle, Latin digits)
   - Self-hosted Noto Sans Bengali font
2. Core UI Features:
   - Top Student ID bar: 17-digit numeric validation, counter (e.g. "12/17"), persisted in `localStorage`
   - Class Listing: table/card view showing total, held, confirmed, available seats; polling every ~3s; Apply button disabled when appropriate
   - My Applications ("আমার আবেদন"): list of student's holds, live 1-second countdown to expiry, automatic status flip to `EXPIRED` at 0 without page reload, payment button ("ভর্তি ফি দিন") to confirm hold, cancel button with confirmation
   - Admin Class Creation: form with name and totalSeats inputs, validation
   - Friendly error handling: user-facing Bangla/English messages mapped from backend error codes (RFC 9457 `code`)
   - React Error Boundary, loading skeletons, responsive design
3. Verify: `npm run build` succeeds cleanly with zero errors. Write `handoff.md`.

## References
- `/home/ashiq/work/school-admission-system/PLAN.md` (§8, §9)
- `/home/ashiq/work/school-admission-system/.agents/teamwork/PROJECT.md`
- `/home/ashiq/work/school-admission-system/.agents/teamwork/ORIGINAL_REQUEST.md`


## 2026-11-09T03:53:29Z
You are the Frontend Engineer for the School Admission System project.
Your working directory is: /home/ashiq/work/school-admission-system/.agents/teamwork/frontend
Read the following files before starting:
- /home/ashiq/work/school-admission-system/.agents/teamwork/ORIGINAL_REQUEST.md
- /home/ashiq/work/school-admission-system/PLAN.md (especially §2.2, §8, §9)
- /home/ashiq/work/school-admission-system/.agents/teamwork/PROJECT.md
- /home/ashiq/work/school-admission-system/.agents/teamwork/frontend/DISPATCH.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Your exclusive responsibilities:
1. Scaffold React 19 + TypeScript + Vite project in `frontend/`:
   - Configurable `VITE_API_BASE_URL` (reads `import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080'`)
   - Styling: Tailwind CSS
   - State management: Zustand 5 with `persist` middleware for student ID and language preferences
   - Server state: TanStack Query v5 for caching, polling, invalidation
   - HTTP client: Axios instance with `X-Student-Id` interceptor and HTTP `Date` header clock skew compensation
   - Localization: react-i18next with Bangla (`bn`) as default and English (`en`) secondary; self-hosted Noto Sans Bengali font; English/Latin digits (0-9) used consistently; Asia/Dhaka time zone formatting
2. Screens & Features:
   - Header Bar: 17-digit student birth registration input with numeric validation, live counter ("12/17"), persisted in `localStorage`
   - Class Listing: table/card view showing total, held, confirmed, and available seats; polling `GET /classes` every ~3s; Apply ("আবেদন করুন") button disabled when student has active hold/confirmed or when class is full
   - My Applications ("আমার আবেদন"): displays all student applications from `GET /me/holds`; 1-second live countdown ticker for `HELD` status; dynamic UI status change to `EXPIRED` at 0 without page reload; payment button ("ভর্তি ফি দিন") calling confirm endpoint; cancel button with confirmation calling DELETE
   - Admin Class Creation: form for school admin to create new classes with name and totalSeats
   - Friendly error handling: user-friendly Bangla/English messages mapped from RFC 9457 ProblemDetails `code` (e.g. `SEATS_FULL`, `ALREADY_HELD`, `HOLD_EXPIRED`, `INVALID_STUDENT_ID`); top-level React Error Boundary; loading indicators
3. Periodically update your `progress.md` in your working directory.
4. Verify with `npm run build` in `frontend/`.
5. Write `handoff.md` with build verification results and message parent.


## 2026-11-09T04:04:29Z
**Context**: Post-restart resumption of Frontend implementation
**Content**: A brief system restart occurred and all subagents are being resumed. Please continue building the React 19 + TypeScript + Vite frontend application: package.json, Zustand stores (17-digit studentId persisted, language), TanStack Query polling (~3s), Axios with skew compensation, i18n (Bangla default), student ID bar, Class listing, My Applications with 1s countdown & dynamic expiry, admin class creation, and error mapping. Verify with `npm run build` and submit `handoff.md`.
**Action**: Resume work from your current progress point and report when complete.
