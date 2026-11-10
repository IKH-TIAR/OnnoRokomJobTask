# AI Usage

## Tool

**Claude Code** (Anthropic, Claude Opus model), used in the terminal as a pair-programming assistant.

## How I worked with it

I treated the AI as a fast pair programmer, not as the decision-maker:

* **Requirements first.** I had it break the brief into requirements, traps and open questions before any code. The open questions became the decisions in `ASSUMPTIONS.md`.
* **My architecture and conventions.** I chose the structure: a single backend project with minimal APIs, organised into Domain / Application / Endpoints / Infrastructure folders with DTOs, FluentValidation and an endpoint validation filter, following the conventions of my own earlier .NET project. PostgreSQL runs only in Docker.
* **Small, verified steps.** I made it build one piece at a time (skeleton → classes API → hold API → confirm/cancel → tests → Docker → frontend). Each step had to be built, run and checked against the contract with real requests before I reviewed and committed it. That is why the commit history is incremental.
* **Keeping the scope on the brief.** I checked its suggestions against the PDF. For example, I confirmed that backend tests (with a concurrency test) are a required deliverable, and that frontend tests are only a bonus.

## Where I used it

| Area | AI involvement |
|---|---|
| Requirement analysis | Summarised the brief and listed edge cases and ambiguities; I decided the assumptions. |
| Backend code | Generated most of the code within the structure and conventions I specified; I reviewed it step by step. |
| Concurrency design | Discussed options; we used a database row lock on the class, and the AI pointed out a race between confirm and expiry that the same lock closes. |
| Tests | Wrote the backend integration tests (real PostgreSQL via Testcontainers) and the frontend tests (Vitest + React Testing Library) for the scenarios we defined. |
| Docker / Compose | Wrote the Dockerfiles and compose setup. |
| Frontend | Generated the React screens; I tested the flows in the browser. |
| Docs | Drafted `ASSUMPTIONS.md` and this file; I edited them. |

## Where the AI got it wrong, and how it was caught

| # | Mistake | How it was caught | Fix |
|---|---|---|---|
| 1 | The first scaffold was a multi-project "clean architecture" solution with MVC controllers, not what I wanted. | My review of the first step. | I had it rebuilt as a single project with minimal APIs following my own conventions. |
| 2 | It copied the middleware order from my old project: request logging ran before the exception handler, so every handled 400/404 was logged as a **500 with a stack trace**. | Reading the logs during the first smoke test. The client got the right status; only the logs were wrong. | Moved request logging so it records the final status code. |
| 3 | The first load test (300 requests over **two backend processes**) produced **200 × HTTP 500** "too many clients": two connection pools of 100 exceeded PostgreSQL's limit of 100. No seat was overbooked, but users would have seen errors. | Running the concurrency scenario across two processes instead of trusting a single-process run. | Capped the pool at 40 per process, raised `max_connections` to 200, and mapped DB overload to a friendly `503 SERVICE_BUSY`. |
| 4 | `GET /me/holds` returned 500: an EF Core query ordered by a property of a projected record, which EF cannot translate to SQL. | Smoke-testing every endpoint, not only the new ones. | Filter and order on the entity before projecting. |
| 5 | `expiresAt` had 7 decimal digits right after creation but 6 when read back (PostgreSQL stores microseconds), so the same hold showed two different values. | Comparing the POST and GET responses. | Truncate timestamps to microseconds when the hold is created. |
| 6 | The test project picked up a different EF Core version than the API, which broke the test build. | The first test run. | Pinned one EF Core version in the API project. |
| 7 | On a fresh `docker compose up`, the logs showed alarming but harmless "ERR" lines (EF probing its history table, a missing Kerberos library in the slim image). Reviewers would read them as failures. | Doing a full clean run the way a reviewer would (volume deleted, images rebuilt). | Silenced the duplicate EF command log and disabled the Kerberos probe in the connection string. |
| 8 | It also slipped in its own tooling: it once started the API with `--no-build` right after adding a migration (crash on startup), and one of its shell test scripts had a quoting bug that produced bogus 404s. | The results did not match expectations, so they were investigated instead of accepted. | Fixed the command and the script. |

## Making sure the tests themselves were trustworthy

A test suite that passes on the first run proves little. After all 35 backend tests passed, I had the database lock removed on purpose and re-ran the concurrency tests: **42 students got seats in a 10-seat class** and 3 tests failed. With the lock restored they pass again. That showed the tests really detect overbooking.

I did the same with the frontend tests: with the rule that turns a timed-out application into `EXPIRED` switched
off, the two tests covering it failed. With the rule restored, all 40 pass.

## Takeaway

The AI saved a lot of typing and was useful for spotting edge cases, but every one of the mistakes above produced code that compiled and looked reasonable. They were caught only by running the system against the real database, under concurrent load and with a clean Docker start, and by reading the logs instead of assuming it worked.
