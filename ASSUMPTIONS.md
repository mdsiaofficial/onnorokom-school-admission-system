ASSUMPTIONS - School Admission System

1. One Application Per Birth Certificate Number
A student is identified by their 17-digit birth registration number (X-Student-Id). The system allows only one active hold or confirmed admission per student across the school at any given time. This is enforced at the service level and via a PostgreSQL unique partial index on student_id for active statuses (HELD, CONFIRMED). Changing the birth certificate number in the UI allows applying for another student.

2. Role and Teacher Capabilities
Classes can only be created and managed by a teacher. A demo teacher login toggle is added for test purposes to make evaluation easier. When logged in as a teacher, the user can view class lists, create new classes, update class details/capacities, and delete classes that have no active holds. Teachers cannot submit student admission applications.

3. Seat Expiry Handling
Hold duration defaults to HOLD_DURATION_SECONDS (600s). Expiry is derived dynamically in queries: any hold with status HELD and expires_at <= clock_timestamp() is immediately treated as expired and returns its seat to the pool without requiring manual API calls. A background sweeper also marks expired rows in the database for cleanup.

4. Class and Shift Representation
Per the assignment brief, each shift is treated as an independent class entity with its own seat capacity (for example, "Class 6 - Morning Shift" and "Class 6 - Day Shift" are separate records).

5. Data Persistence and Demo Seeding
Data is persisted in a named Docker volume (school_admission_db_data). On startup, standard demo classes are seeded so the application is immediately testable upon running docker compose up.

6. Concurrency and Multi-Process Safety
Seat overselling is strictly prevented (held + confirmed <= totalSeats) even if multiple backend processes run simultaneously against the same database. This is achieved using PostgreSQL row-level locking (SELECT ... FOR UPDATE) on the class row inside a database transaction, rather than in-memory locks. Time is checked using PostgreSQL clock_timestamp() to avoid server clock drift.

7. Simulated Payment and Confirmation
No external payment gateway is needed. Clicking the payment button simulates confirmation via POST /holds/{id}/confirm. The confirmation endpoint is idempotent: multiple calls for the same hold return 200 OK without increasing confirmed seats.
