# School Admission System

Online admission for a private school on a **first come, first served** basis. Each class shift has a limited
number of seats. Applying puts a seat **on hold** for a limited time (default 10 minutes); paying the admission fee
within that time **confirms** the admission, otherwise the seat is released automatically.

**Stack:** .NET 10 (minimal APIs, EF Core) · PostgreSQL 17 · React 19 + TypeScript (Vite) · Docker

---

## Run the app

The only requirement is **Docker** (with Docker Compose).

```bash
docker compose up --build
```

| What | URL |
|---|---|
| **Frontend (open this)** | **http://localhost:3000** |
| Backend API | http://localhost:8080 |
| PostgreSQL (only for debugging) | `localhost:5433`, user/password `postgres` / `postgres` |

The database tables are created automatically on startup. Data is stored in a Docker volume, so it survives
`docker compose down` followed by `docker compose up`. To start again from an empty database, run
`docker compose down -v`.

## Run the tests

### Backend tests (required: 35 integration tests, including the concurrency test)

Inside Docker (nothing else to install):

```bash
docker compose run --rm --build backend-tests
```

Or locally, with the .NET 10 SDK and Docker running (a throwaway PostgreSQL container is started automatically):

```bash
cd backend
dotnet test tests/SchoolAdmission.Tests
```

The tests run the real API against a real PostgreSQL database. They cover:

* **Concurrency:** 300 students apply at the same moment for 10 seats → exactly 10 succeed, also when the requests
  are split across **two separate backend instances**; one student applying 50 times at once gets one hold;
  50 simultaneous confirms count the seat once; confirms, cancels and new applications racing each other keep the
  seat counts correct.
* **Hold rules:** class full (409), one application per class, expiry without any API call (via a fake clock),
  confirm only by the owner and only before expiry, idempotent confirm, cancel, re-applying after expiry.
* **API contract:** status codes, field names and status values exactly as specified; validation errors.

### Frontend tests (bonus: 40 tests)

```bash
docker compose run --rm --build frontend-tests
```

Or locally with Node.js 22+:

```bash
cd frontend
npm install
npm test
```

Vitest + React Testing Library, with the API mocked. They cover the countdown and the rule that shows an
application as `EXPIRED` the moment time runs out (no reload), paying and cancelling (with the "are you sure?"
prompt), the other-student case, the student id input and that it is remembered after a refresh, Apply being
disabled for full classes, and plain-language messages for errors and for an unreachable server.

## Bonus: two backend processes on one database

```bash
docker compose --profile multi up --build
```

This starts a second backend on **http://localhost:8081** sharing the same database. Seats are still never
overbooked, because the seat check uses a row lock inside PostgreSQL instead of in the application's memory. See
[ASSUMPTIONS.md](ASSUMPTIONS.md#concurrency-how-held--confirmed--totalseats-is-guaranteed-incl-bonus) for the
details. The automated test `TwoBackendInstancesSharingTheDatabase_NeverOverbookTheClass` proves it.

## Using the app

1. **Enter the student's birth registration number** (17 digits) at the top. It is remembered, so a refresh or
   browser restart keeps the student's applications.
2. **Admin** → create classes, e.g. "Class 6 - Morning Shift" with 120 seats. Every shift is a separate class.
3. **Classes** → see total / on hold / confirmed / free seats for each class (updates every 3 seconds) and **Apply**.
4. **Application** → status and a live countdown. **Pay admission fee** confirms the seat (no real payment);
   **Cancel** releases it. When time runs out the application turns `EXPIRED` on its own.
5. **My applications** → every application of this student and its current status.

## Configuration

All settings are environment variables (defaults are in `docker-compose.yml`):

| Variable | Default | Used by | Meaning |
|---|---|---|---|
| `HOLD_DURATION_SECONDS` | `600` | backend | How long a seat stays on hold before it is released. |
| `ConnectionStrings__DefaultConnection` | compose PostgreSQL | backend | Database connection string. |
| `Cors__AllowedOrigins` | `*` | backend | Allowed frontend origins (comma-separated). |
| `API_BASE_URL` | `http://localhost:8080` | frontend | Backend URL used by the browser. Applied at container start, no rebuild needed. |

Example, with 30-second holds for a quick demo:

```bash
HOLD_DURATION_SECONDS=30 docker compose up --build
```

## API

Base URL `http://localhost:8080`; JSON everywhere. The student is identified by the `X-Student-Id` header
(17-digit birth registration number).

| # | Method & path | Header | Success | Purpose |
|---|---|---|---|---|
| 1 | `POST /classes` | | 201 | Create a class `{ name, totalSeats }` |
| 2 | `GET /classes` | | 200 | All classes with `heldSeats`, `confirmedSeats`, `availableSeats` |
| 3 | `GET /classes/{classId}` | | 200 | One class with seat counts |
| 4 | `POST /classes/{classId}/holds` | `X-Student-Id` | 201 | Apply (hold a seat); **409** if full or already applied |
| 5 | `GET /holds/{holdId}` | | 200 | Application status: `HELD` / `CONFIRMED` / `EXPIRED` / `CANCELLED` |
| 6 | `POST /holds/{holdId}/confirm` | `X-Student-Id` | 200 | Pay the fee → `CONFIRMED` (safe to repeat) |
| 7 | `DELETE /holds/{holdId}` | `X-Student-Id` | 204 | Cancel the application |
| 8 | `GET /me/holds` | `X-Student-Id` | 200 | All applications of the student |

Errors use RFC 7807 Problem Details with an extra `code` field (e.g. `CLASS_FULL`, `HOLD_EXPIRED`,
`STUDENT_ID_INVALID`), which the UI turns into plain-language messages. Ready-made requests are in
[`backend/SchoolAdmission.http`](backend/SchoolAdmission.http).

## Project structure

```
backend/                      .NET 10 minimal API (single project)
  Domain/                     Entities and business rules (SchoolClass, Hold), value objects, domain exceptions
  Application/                DTOs, FluentValidation validators, service interfaces, settings
  Endpoints/                  Minimal API routes + handlers, validation & X-Student-Id filters
  Infrastructure/             EF Core DbContext, configurations, migrations, services, global error handler
  tests/SchoolAdmission.Tests xUnit integration tests (Testcontainers / real PostgreSQL)
  Dockerfile                  build → test → runtime stages
frontend/                     React + TypeScript + Vite + TanStack Query + Tailwind
  src/api/                    API client, typed errors, React Query hooks
  src/pages/                  Classes, Admin, Application, My applications
  src/components/             Layout, student id bar, countdown, status badge, …
  src/**/*.test.ts(x)         Vitest + React Testing Library tests
  Dockerfile, nginx.conf      static build served by nginx; config.js generated from API_BASE_URL
docker-compose.yml            postgres + backend + frontend (+ backend-tests, frontend-tests, backend-2 profiles)
ASSUMPTIONS.md                decisions where the brief was not explicit
AI_USAGE.md                   how AI was used, and where it was wrong
```

## Local development (optional, without Docker for the apps)

```bash
docker compose up -d postgres                    # database on localhost:5433
cd backend  && dotnet run                        # API on http://localhost:8080
cd frontend && npm install && npm run dev        # UI on http://localhost:5173
```

## More

* [ASSUMPTIONS.md](ASSUMPTIONS.md): what the brief left open and what was decided, including how concurrency is handled.
* [AI_USAGE.md](AI_USAGE.md): which AI tool was used, for what, and the mistakes that were caught.
