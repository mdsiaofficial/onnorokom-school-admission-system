AI USAGE - School Admission System

1. AI Tools Used
- gemini-3.8-flash: Used for creating initial boilerplate code (.NET Minimal APIs, React components, and xUnit test structures).

2. Tasks Where AI Assisted
- Initial project structure setup and drafting endpoint contracts.
- Generating Bangla and English localization key files.
- Drafting test files for concurrent requests and validation checks.

3. Mistakes Made by AI and Human Corrections
- PostgreSQL Volume Mount: AI initially suggested mounting to /var/lib/postgresql/data, which caused cluster initialization issues in PostgreSQL 18 Alpine. 
  - -> Corrected to /var/lib/postgresql for proper persistence.

- In-Memory Locking: AI initially used C# SemaphoreSlim/in-memory locks for seat holding. 
  - -> Corrected to PostgreSQL row-level locks (SELECT ... FOR UPDATE) inside transactions so concurrency holds across multiple backend processes and server restarts.

- Expiry Latency: AI initially relied strictly on a timer to flip expired rows. 
  - -> Corrected to derive expiry directly in SQL queries (status = 'HELD' AND expires_at > clock_timestamp()) so expired seats become available immediately without waiting for a background worker.

- Numerals in Bangla UI: AI attempted to convert numbers to Bengali script, causing mismatch in 17-digit birth certificates and timers. 
  - -> Corrected to use standard Latin digits (0-9) throughout the UI.

- Missing Class Management Endpoints: AI originally only provided class creation and reading endpoints.
  - -> Prompted by user to add full teacher class management: added PUT /classes/{id} to update class details/seats and DELETE /classes/{id} to delete classes safely without active holds, along with teacher UI modals.
