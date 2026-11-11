ASSUMPTIONS - School Admission System

1. One Application Per Birth Certificate Number
A student is identified by their 17-digit birth registration number (X-Student-Id) and may hold only one active (HELD or CONFIRMED) application at a time, school-wide. Enforced in the service layer and by a PostgreSQL unique partial index on student_id. Changing the number allows applying as another student.

2. Role and Teacher Capabilities
Classes can only be created and managed by a teacher; a one-click demo teacher login is added for easier evaluation. Teachers can view, create, edit and delete classes that have no active holds, but cannot submit student applications.

3. Seat Expiry Handling
Hold duration comes from HOLD_DURATION_SECONDS (default 600s). Expiry is derived in queries - any HELD row past expires_at frees its seat immediately, with no API call - and a background sweeper marks expired rows for cleanup.

4. Class and Shift Representation
Per the brief, each shift is an independent class with its own seat capacity (for example "Class 6 - Morning Shift" and "Class 6 - Day Shift" are separate records).

5. Data Persistence and Demo Seeding
Data lives in a named Docker volume (school_admission_db_data). Demo classes are seeded only when the database is empty, so restarts never destroy real data while a fresh volume is still instantly testable.

6. Concurrency and Multi-Process Safety
Seat overselling is impossible (held + confirmed <= totalSeats) even with several backend processes on one database, enforced by PostgreSQL row-level locking (SELECT ... FOR UPDATE) inside a transaction rather than in-memory locks. Time comes from clock_timestamp() to avoid clock drift.

7. Simulated Payment and Confirmation
No payment gateway is needed; the pay button confirms via POST /holds/{id}/confirm. The endpoint is idempotent - repeat calls return 200 without increasing confirmed seats.

8. Bilingual User Interface (Bangla and English)
The brief sets no UI language, so the UI ships in both Bangla (default) and English, switchable at runtime from the navbar. Copy lives in i18next resource files kept at identical key parity; the choice is persisted and reflected in the document lang attribute. Numerals stay Latin in both languages because seat counts and 17-digit IDs must be copy-pasteable. The API stays language-neutral, returning English error codes in RFC 9457 ProblemDetails that the frontend localizes.
