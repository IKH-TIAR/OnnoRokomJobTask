# Assumptions

Things the brief does not spell out, what I decided, and why.

## Classes

| Topic | Decision | Why |
|---|---|---|
| Shifts | Every shift is its own class (as the brief says), e.g. "Class 6 - Morning Shift" and "Class 6 - Day Shift". | Matches §1.3. |
| Class validation | `name` is required (max 200 chars, trimmed); `totalSeats` must be 1–10,000. Names do not have to be unique. | Prevents empty/negative classes; duplicates are harmless because classes are identified by id. |
| IDs | UUID (v7) strings, e.g. `01a1c4ee-613b-…`. | The brief allows any format; UUIDs are not guessable, which matters because `GET /holds/{id}` is public. |
| Admin | No authentication for the admin screen/API. | The brief says no login is needed. |

## Student identity

| Topic | Decision | Why |
|---|---|---|
| `X-Student-Id` | Must be exactly 17 digits. Missing → `400 STUDENT_ID_REQUIRED`, malformed → `400 STUDENT_ID_INVALID`. | A birth registration number is 17 digits (§1.4). |
| `GET /holds/{holdId}` | Does not require the header. | The contract shows no header for this endpoint. |
| Someone else's hold | Confirm/cancel return `403 HOLD_NOT_OWNED`. | Only the owner may act (§1.5). 403 instead of 404 because the hold is already readable via the public `GET /holds/{id}`. |

## Holds (applications)

| Topic | Decision | Why |
|---|---|---|
| One application per class | A student can have only one `HELD` or `CONFIRMED` hold per class (`409 ALREADY_APPLIED`). Applying to a *different* class/shift is allowed. | §1.5 restricts it per class only. |
| Re-applying | Allowed after the previous hold `EXPIRED` or was `CANCELLED`. | The seat went back to the pool, so the student is just another applicant. |
| Full class | `409 CLASS_FULL`. | §1.4 says 409 when no seat is left. |
| Expiry | A `HELD` hold stops occupying a seat the moment `expiresAt` passes. Every read and every seat count treats it as `EXPIRED` immediately; the stored row is updated to `Expired` the next time someone applies to that class. | §1.5: the seat must come back automatically, without any API call. Evaluating expiry at read time makes it exact to the second, with no background job that could lag behind. |
| Hold duration | `HOLD_DURATION_SECONDS` env var, default `600`. | §1.5. |
| Confirm twice | Returns `200 CONFIRMED` again and changes nothing. | §1.5: confirming repeatedly must count the seat once. |
| Confirm after expiry / after cancel | `409 HOLD_EXPIRED` / `409 HOLD_CANCELLED`. | Only an own, unexpired `HELD` hold can be confirmed (§1.5). |
| Cancel | Only a `HELD` hold can be cancelled (`204`). Cancelling again → `204`. Cancelling a `CONFIRMED` admission → `409 HOLD_ALREADY_CONFIRMED`; an expired one → `409 HOLD_EXPIRED`. | Once the fee is paid, undoing it would mean a refund, which is out of scope. |
| `GET /me/holds` | Returns all of the student's holds in every status, newest first. | The "My applications" screen shows every application and its status (§1.3). |
| Extra fields | Hold responses also include `className` and `studentId`; confirm returns the full hold. | The brief allows adding fields; the UI needs the class name. |
| Time | The server clock is the source of truth. Timestamps are UTC ISO-8601 with `Z`. Several backend instances are assumed to have synchronised clocks (same host / NTP). | Expiry must be decided in one place. |

## Errors

All errors use RFC 7807 Problem Details plus a machine-readable `code` (e.g. `CLASS_FULL`, `HOLD_EXPIRED`, `VALIDATION_FAILED`). The UI maps codes to plain-language messages. If the database is temporarily overloaded, the API returns `503 SERVICE_BUSY` with `Retry-After` instead of a generic 500.

## Concurrency: how `held + confirmed ≤ totalSeats` is guaranteed (incl. bonus)

* Every operation that changes a hold runs in one transaction that first locks the class row (`SELECT … FOR UPDATE`), then counts active holds, then inserts or updates.
* Because the lock lives in PostgreSQL, it works the same for one or many backend processes on different ports (bonus §1.6).
* Confirm and cancel take the same class lock, always in the order class → hold. That prevents a race at the moment of expiry and rules out deadlocks.
* A partial unique index (`class_id, student_id WHERE status IN ('Held','Confirmed')`) is a second safety net in the database.
* Each process pools at most 40 connections and PostgreSQL allows 200, so a burst of requests waits for a connection instead of failing.
* Proven by tests: 300 simultaneous applicants for 10 seats, also split across two app instances, always give exactly 10 holds. With the lock removed on purpose, the same test produced 42 holds for 10 seats and failed.

## Frontend

| Topic | Decision | Why |
|---|---|---|
| Student id | Typed once at the top and kept in `localStorage`, so applications are still visible after a refresh or browser restart. | §1.3 and §1.5. If storage is blocked, the app still works; it just forgets the id. |
| Live updates | Seat counts and application status are polled every 3 seconds. | Simple and robust; meets "updates every few seconds without reload". |
| Countdown | Uses the device clock and switches to `EXPIRED` at zero without a reload. If the device clock is wrong, the display may be slightly off, but the server still decides: paying after the real expiry is rejected with a clear message. | §1.5. |
| Backend URL | Read at page load from `config.js`, which the Docker container generates from an environment variable, so it can change without rebuilding. | §1.2. |

## Infrastructure

| Topic | Decision | Why |
|---|---|---|
| .NET version | .NET 10. | The brief allows .NET 8 or newer. |
| Migrations | Applied automatically when the backend starts. | `docker compose up` must be enough. |
| Persistence | Data lives in a named Docker volume and survives `docker compose down` / `up`. (`docker compose down -v` deletes it on purpose.) | §1.5. |
| Ports | Frontend on `3000`, backend on `8080` (required by the brief). | 3000 differs from the Vite dev server (5173), so both can run side by side. |
| PostgreSQL host port | Published on `5433`, not 5432. | Avoids a clash if a PostgreSQL is already installed on the reviewer's machine. Containers talk to each other on 5432 internally. |
