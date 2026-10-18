# Task Manager (.NET 8 + React)

Full-stack task management app: ASP.NET Core Web API (DDD-style layers) + React/TypeScript SPA.
PostgreSQL for data, Redis for caching, a `BackgroundService` for background processing.

- Repository: `https://github.com/MohamedMahmoud01110/task-manager`
- Video walkthrough: `https://drive.google.com/file/d/1puoDVcqWxQmMLXIsO_wmix0RufQpbt81/view?usp=sharing`

## Table of contents

1. [Quick start](#quick-start)
2. [Seeded admin](#seeded-admin)
3. [Environment variables](#environment-variables)
4. [Project structure and architecture](#project-structure-and-architecture)
5. [How the key requirements are solved](#how-the-key-requirements-are-solved)
6. [API overview](#api-overview)
7. [Tests](#tests)
8. [Assumptions](#assumptions)
9. [Enhancements](#enhancements)
10. [Bonus items](#bonus-items)
11. [Known limitations / unfinished](#known-limitations--unfinished)
12. [How I used AI tools](#how-i-used-ai-tools)

---

## Quick start

### Prerequisites (clean machine)

- [.NET 8 SDK](https://dotnet.microsoft.com/download) or newer (the projects target `net8.0`).
- [Node.js 20+](https://nodejs.org/).
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (must be running).
- Free ports: `5432` (PostgreSQL), `6379` (Redis). For the full-stack option, also `8080` and `3000`.

- **Integration tests:** Docker must be running because Testcontainers starts a temporary PostgreSQL container during the tests.

### Option A: everything with one command (Docker)

```bash
docker compose --profile full up --build -d
```

- Frontend: http://localhost:3000
- API Swagger: http://localhost:8080/swagger

The API applies EF Core migrations and seeds the admin automatically on startup.
Stop with `docker compose --profile full down` (add `-v` to also delete the database volume).

### Option B: development mode (DB + Redis in Docker, API and frontend run locally)

```bash
# 1. Database + Redis
docker compose up -d

# 2. Backend (new terminal)
cd backend
dotnet run --project src/TaskManager.Api
# The URL is printed in the console and defined in src/TaskManager.Api/Properties/launchSettings.json

# 3. Frontend (new terminal)
cd frontend
npm install
cp .env.example .env          # Windows PowerShell: Copy-Item .env.example .env
# edit .env so VITE_API_URL matches the API URL from step 2
npm run dev                   # http://localhost:5173
```

Migrations are applied and the admin is seeded automatically when the API starts, so no manual `dotnet ef` step is needed.

### Run the tests

```bash
cd backend
dotnet test
```

---

## Seeded admin

Created on first startup if it does not exist:

| Field    | Value                     |
| -------- | ------------------------- |
| Email    | `admin@taskmanager.local` |
| Password | `Admin@12345`             |

These are development defaults. Override them with `Seed__AdminEmail` / `Seed__AdminPassword` (see below) for anything beyond a local demo.

---

## Environment variables

### Backend (ASP.NET Core configuration; use `__` for nesting in environment variables)

| Variable                                   | Default (appsettings.json)           | Purpose                                                                  |
| ------------------------------------------ | ------------------------------------ | ------------------------------------------------------------------------ |
| `ConnectionStrings__Default`               | local PostgreSQL on `localhost:5432` | PostgreSQL connection string                                             |
| `ConnectionStrings__Redis`                 | `localhost:6379`                     | Redis connection string                                                  |
| `Jwt__Key`                                 | dev-only key                         | JWT signing key, **at least 32 characters, change it outside local dev** |
| `Jwt__Issuer` / `Jwt__Audience`            | `TaskManager` / `TaskManager.Client` | Token validation                                                         |
| `Jwt__ExpiryMinutes`                       | `60`                                 | Access token lifetime                                                    |
| `Seed__AdminEmail` / `Seed__AdminPassword` | see [Seeded admin](#seeded-admin)    | Seeded admin account                                                     |
| `Cors__Origins__0`                         | `http://localhost:5173`              | Allowed frontend origin (add `__1`, `__2` for more)                      |
| `Worker__ProcessingDelaySeconds`           | `5`                                  | Simulated processing time of the background worker                       |
| `Worker__MaxConcurrency`                   | `4`                                  | Tasks processed in parallel by the worker                                |
| `RateLimiting__LoginPerMinute`             | `10`                                 | Login attempts per IP per minute                                         |

### Frontend

| Variable       | Example                 | Purpose                                                                     |
| -------------- | ----------------------- | --------------------------------------------------------------------------- |
| `VITE_API_URL` | `http://localhost:5000` | API base URL (see `frontend/.env.example`). Baked in at build time by Vite. |

---

## Project structure and architecture

```
.
├── docker-compose.yml            PostgreSQL + Redis (+ api and web under the "full" profile)
├── .github/workflows/ci.yml      Build + test for backend and frontend
├── backend/
│   ├── src/
│   │   ├── TaskManager.Domain          Entities, enums, domain exceptions. No dependencies.
│   │   ├── TaskManager.Application     Services, DTOs, validators, interfaces (ports)
│   │   ├── TaskManager.Infrastructure  EF Core, Redis cache, JWT, hashing, queue and worker
│   │   └── TaskManager.Api             Controllers, middleware, auth/swagger setup
│   └── tests/TaskManager.UnitTests
└── frontend/
    └── src/ api/  auth/  components/  hooks/  lib/  pages/
```

Dependencies point inward: `Api → Application → Domain`, and `Infrastructure → Application → Domain`.
The Domain knows nothing about EF Core, HTTP or Redis.

**Where the business rules live (all in the domain, all unit-tested):**

| Rule                                                                | Location                                        |
| ------------------------------------------------------------------- | ----------------------------------------------- |
| Status moves forward only                                           | `TaskItem.ChangeStatus` / `CanMove`             |
| Due date cannot be in the past (create/update), overdue computation | `TaskItem.Create`, `UpdateDetails`, `IsOverdue` |
| Title validation, normalization (trim + case-insensitive key)       | `TaskItem`                                      |
| Ownership check (404 for other users' tasks)                        | `TaskItem.EnsureOwnedBy`                        |
| Admin cannot delete own account, soft delete                        | `User.SoftDelete`                               |
| Email normalization (case-insensitive)                              | `User.NormalizeEmail`                           |

Controllers are thin: they call an application service, which loads domain entities and calls their methods.

---

## How the key requirements are solved

**Duplicate titles under concurrency.** The `tasks` table has a unique index
`ux_tasks_user_title_day` on `(UserId, NormalizedTitle, CreatedDate)`, where `NormalizedTitle` is the trimmed, lower-cased title and `CreatedDate` is the UTC day.
There is deliberately no "does it exist?" check in code, which would be racy. The database decides, and the repository maps the unique violation to **409**. Two identical simultaneous requests: one gets 201, the other 409.

**Unique emails.** Same approach: unique index on `NormalizedEmail` (upper-cased), plus a friendly pre-check.

**Auth, end to end.** Register/login return a JWT (BCrypt-hashed passwords). `OnTokenValidated` checks the database on every request, so a token issued before a user was soft-deleted is rejected with **401** immediately. The React app keeps the token in `localStorage`, restores the session on refresh (checks `exp`, then calls `/api/auth/me`), and a single API client module adds the token and handles any authenticated 401 by logging out with a message. Admin endpoints use `[Authorize(Roles = "Admin")]` (403 as ProblemDetails); the React route guard and menu are only UX on top.

**Redis caching.** `GET /api/tasks/{id}` caches a raw task snapshot under `task:{userId}:{taskId}`. The cache is invalidated after every successful detail update, status change, delete and worker change. Writes always read the database, never the cache. All Redis calls are wrapped so any failure becomes a cache miss plus a **warning log**; the connection uses `AbortOnConnectFail=false`, short timeouts and `FailFast`, so the API **starts and stays fast while Redis is down** and recovers automatically when Redis returns. A 2-minute TTL is a safety net.

**Background worker.** On create the task is saved first, then its id is written to an in-memory `Channel`. A `BackgroundService` waits the simulated delay and moves it `Pending → InProgress`.
It never overwrites user changes: (1) it re-reads the task and skips it unless it is still `Pending`; (2) the row has an optimistic concurrency token (PostgreSQL `xmin`), so a change between read and write raises `DbUpdateConcurrencyException`; (3) on conflict it retries with a fresh `DbContext`, re-reads and re-decides.
**Restart safety:** the queue is only a speed-up. At startup the worker loads every `Pending` task from the database (using a partial index) and re-queues it.

**Error handling.** One exception middleware plus FluentValidation produce RFC 7807 `ProblemDetails` for 400/401/403/404/409/429/500. No stack traces or internal messages are returned (500 is generic, details only in logs). Binding errors (bad JSON, unknown enum value) are also 400, not 500.

---

## API overview

Swagger UI: `/swagger` (use the **Authorize** button and paste the JWT without the `Bearer ` prefix).

| Method     | Route                    | Access                                                                     |
| ---------- | ------------------------ | -------------------------------------------------------------------------- |
| POST       | `/api/auth/register`     | public (always creates role `User`)                                        |
| POST       | `/api/auth/login`        | public, rate limited                                                       |
| GET        | `/api/auth/me`           | authenticated                                                              |
| POST       | `/api/tasks`             | authenticated                                                              |
| GET        | `/api/tasks`             | authenticated: `page`, `pageSize` (max 50), `status`, `priority`, `search` |
| GET        | `/api/tasks/{id}`        | owner only (cached)                                                        |
| PUT        | `/api/tasks/{id}`        | owner only (title, description, priority, due date)                        |
| PATCH      | `/api/tasks/{id}/status` | owner only                                                                 |
| DELETE     | `/api/tasks/{id}`        | owner only                                                                 |
| GET / POST | `/api/admin/users`       | admin only                                                                 |
| DELETE     | `/api/admin/users/{id}`  | admin only (soft delete, not self)                                         |

List ordering: priority High first, then oldest first, then id (stable paging). The response includes `totalCount` and `totalPages`.
Task responses include `isOverdue` and `allowedNextStatuses`.

---

## Tests

Run the backend tests:

```bash
cd backend
dotnet test
```

**Unit tests**

- **Domain tests:** duplicate-title key (case, edge spaces, UTC day boundary), status workflow (every allowed and rejected transition), due-date rules (create, update, overdue editing), ownership checks, and user rules (email normalization, admin cannot delete self).
- **Service tests:** small in-memory fakes verify that cache hits avoid database reads, another user never gets a cached task, cache invalidation after update/status/delete, cache preservation when saving fails, and enqueueing only after a successful save.

**Integration tests**

`TaskManager.IntegrationTests` uses xUnit, `WebApplicationFactory`, and Testcontainers to run the real API against a temporary PostgreSQL container, with Redis intentionally unreachable.

The tests cover:

- **Concurrent duplicate task creation:** 10 identical concurrent requests result in exactly one `201 Created` and nine `409 Conflict` responses.
- **Concurrent email uniqueness:** case-insensitive email uniqueness under concurrent requests.
- **Task ownership:** accessing another user's task returns `404 Not Found`.
- **Deleted-user authentication:** a token issued before the user was deleted is rejected with `401 Unauthorized`.
- **Admin authorization:** unauthenticated and unauthorized requests return `401 Unauthorized` and `403 Forbidden`, respectively.
- **Background worker:** tasks move to `InProgress`, and the worker does not overwrite a task already completed by the user.
- **Status workflow:** backward status transitions are rejected.
- **Validation and error handling:** invalid requests return `400 Bad Request` with `ProblemDetails`.
- **Redis resilience:** the API continues to work when Redis is unavailable.

**Requirements:** Docker must be running to execute the integration tests because they start a temporary PostgreSQL container.

The database-level guarantees, including unique indexes and PostgreSQL concurrency behavior, are also covered by the integration tests where applicable.

---

## Assumptions

- **Status workflow:** status moves forward only (`Pending < InProgress < Done`). Any forward move is allowed, including `Pending → Done`, because the spec says a task the user marks Done before the worker runs must stay Done. Backward moves and repeating the same status are rejected with 400. The rule is `TaskItem.CanMove`; making it strictly sequential is a one-line change. The UI shows only `allowedNextStatuses` returned by the API.
- **"Same day" for duplicate titles is the UTC day**, as the spec says.
- **Due dates are stored in UTC.** The React form converts between the user's local time and UTC. A due date sent without an offset is treated as UTC by the API.
- **Editing an overdue task** is allowed as long as its due date is unchanged; only a _changed_ due date must not be in the past.
- **Deleting a task is a hard delete.** Deleting a user is a soft delete.
- **Public registration** always creates a normal user. Only an admin can create admins.
- **Requesting another user's task returns 404** (not 403) so the existence of the task is not revealed.
- **Login errors are generic** ("Invalid email or password") to avoid revealing which emails exist.
- **The worker moves every new task from Pending to InProgress** after `Worker:ProcessingDelaySeconds` (default 5s).
- Only `GET /api/tasks/{id}` is cached, as specified. The list endpoint always reads the database.
- The seeded admin credentials and the JWT key in `appsettings.json` are for local development only.

---

## Enhancements

Beyond the spec, each with the reason:

- **Titles made of only spaces are rejected** (domain + API + form) so a task can never have an empty title.
- **Due date validated only when changed**, so a user can still edit the description of an overdue task.
- **Due date converted between local time and UTC in the React form**, so the deadline does not shift by the user's time zone offset.
- **Safe UTC conversion of incoming dates** (Npgsql rejects non-UTC `DateTime`), which would otherwise be a 500.
- **Binding errors (bad JSON, invalid enum) return 400 ProblemDetails** instead of 500.
- **Validation error keys are camelCase** so the frontend maps them to form fields directly.
- **Stable list ordering (tie-break by id)** so paging never repeats or skips items.
- **Indexes matching the list filters** `(UserId, Status, Priority)` and a **partial index on Pending tasks** for worker recovery.
- **Cache stores raw state; `isOverdue` is computed on read**, so the badge is never frozen.
- **Cache key includes the user id plus an owner check on read**, so a task can never leak across users.
- **Redis fail-fast and short timeouts**, so an outage does not slow the API.
- **Worker is idempotent, retries on concurrent changes, processes tasks in parallel with a limit, and survives a failing task.**
- **Concurrency conflict returns a clear 409** and the UI reloads the task so the right actions are shown.
- **URL is the single source of truth for page, filters and search**; search is trimmed and debounced; changing a filter returns to page 1.
- **Polling pauses when the browser tab is hidden**, to avoid needless requests.
- **Forms keep their input after a server error, show server errors next to the field, and disable buttons while a request runs.**
- **Redirect back to the page the user came from after login**, and no "session expired" message on a plain wrong password.
- **Responsive layout; tables become cards on mobile.**
- **Delete confirmations are in-page dialogs**, with the self-delete button disabled for the admin.

---

## Bonus items

Done:

- Rate limiting on login (per IP, `429` ProblemDetails with `Retry-After`)
- Docker Compose that runs the whole stack with one command (`--profile full`)
- GitHub Actions CI (backend build + tests, frontend type-check + build)
- Correlation ID per request (`X-Correlation-ID` header, `traceId` in errors, included in log scopes)
- TanStack Query for server state; React Hook Form + zod for forms
- integration tests

Not done: refresh tokens with rotation, SignalR real-time updates (polling is used), frontend tests.

---

## Known limitations / unfinished

- A tiny stale-cache window exists if a reader loads the old row and writes it to Redis after a writer invalidated the key. It is bounded by the 2-minute TTL.
- If Redis is down while a task changes, a stale key may remain until its TTL expires after Redis returns.
- If the app crashes between saving a task and queueing it, the task is picked up at the next restart (there is no periodic sweep).
- The in-memory queue and rate limiter are per-instance; running several API instances would need a shared queue/limiter.
- Behind a reverse proxy, enable forwarded headers so the rate limiter sees real client IPs.
- The JWT is stored in `localStorage` (readable by XSS). An httpOnly refresh-token cookie would be the safer design.
- Worker logs have no correlation id because they run outside a request.
- The token's user check hits the database once per request (a primary-key lookup). A short in-memory cache could reduce this.

---


## How I used AI tools

- I used **Claude Code** as a pair-programming assistant to help with architecture planning, implementation, and understanding design decisions, particularly around concurrency, caching, and background processing.
- I used AI assistance to help write and improve unit and integration tests, identify edge cases, and verify expected behavior. I reviewed and ran the tests to check the results.
- When I encountered issues, I used Claude Code to investigate errors, understand their causes, find suitable NuGet packages or other libraries when needed, and explore possible solutions.
- I also used it for specific implementation tasks, such as writing date and time handling functions, timezone conversions, and complex regular expressions when appropriate.
- I reviewed, tested, and refined the generated code, addressed issues discovered during development, and made sure I understood the final implementation rather than relying on generated code without verification.
