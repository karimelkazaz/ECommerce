# ECommerce Platform

A production-oriented e-commerce backend built with **.NET 10** and **Clean Architecture**, featuring a rich domain model, CQRS with MediatR, reliable event-driven messaging through a transactional Outbox/Inbox pattern, and full authentication (**ASP.NET Core Identity** + JWT) with verified registration and OTP flows.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![EF Core](https://img.shields.io/badge/EF%20Core-10.0-512BD4)
![Database](https://img.shields.io/badge/Database-SQL%20Server-CC2927)
![Tests](https://img.shields.io/badge/Tests-xUnit%20%2B%20NetArchTest-green)

---

## Table of Contents

- [Key Features](#key-features)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Patterns and Practices](#patterns-and-practices)
- [Authentication & Authorization](#authentication--authorization)
- [Transactional Messaging (Outbox / Inbox)](#transactional-messaging-outbox--inbox)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
- [API Endpoints](#api-endpoints)
- [Testing](#testing)

---

## Key Features

- **Rich domain model** — aggregates (`Product`, `Order`, `ProductBrand`, `ProductType`, `DeliveryMethod`) encapsulate invariants and raise domain events; all mutations flow through intention-revealing methods that return a `Result`.
- **CQRS with MediatR** — commands and queries with cross-cutting pipeline behaviors: validation (FluentValidation), logging, performance warnings, and unhandled exception guarding.
- **Transactional Outbox / Inbox** — domain events are persisted atomically with business data, then published asynchronously with retries, dead-lettering, app-lock based leader election, and idempotent consumption.
- **EF Core SaveChanges interceptors** — audit trail, soft delete, and outbox enqueueing applied transparently across the context.
- **Strongly-typed identifiers** — every aggregate uses a dedicated `readonly record struct` ID, mapped consistently in the database.
- **Architecture governance** — automated layer-dependency and persistence-ignorance rules enforced by NetArchTest.
- **Minimal API endpoints** with OpenAPI documentation and RFC 7807 ProblemDetails error responses.
- **Identity & authentication** — ASP.NET Core Identity (roles: `Admin`, `Customer`) with JWT bearer access tokens, rotating/hashed refresh tokens, and role-based endpoint authorization.
- **Verified registration & OTP** — users register unconfirmed and are gated from login until an emailed one-time code is verified; hashed, single-use, attempt-limited OTPs also power password reset.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                          ECommerce.Api                      │
│          Minimal API endpoints · OpenAPI · ProblemDetails   │
└──────────────────────────────┬──────────────────────────────┘
                               │  depends on
               ┌───────────────▼───────────────┐
               │        ECommerce.Application  │
               │  Commands · Queries · Events  │
               │   Behaviors · Abstractions    │
               └───────────────┬───────────────┘
                               │  defines abstractions
     ┌─────────────────────────▼─────────────────────────┐
     │                       ECommerce.Domain            │
     │   Aggregates · Entities · Value Objects · Events  │
     │              Result Pattern · No dependencies     │
     └─────────────────────────▲─────────────────────────┘
                               │  implements abstractions
               ┌───────────────┴───────────────┐
               │      ECommerce.Infrastructure │
               │  EF Core · SQL Server         │
               │  Interceptors · Repositories  │
               │  Outbox Processor · Seeding   │
               └───────────────────────────────┘
```

Dependencies point strictly inward: **Domain has zero external references**, and the rule is enforced automatically by architecture tests.

## Project Structure

| Project | Responsibility |
|---|---|
| `src/ECommerce.Domain` | Aggregates, entities, value objects, domain events, `Result` pattern, common abstractions |
| `src/ECommerce.Application` | Use cases as Commands/Queries, event handlers, pipeline behaviors, layer abstractions |
| `src/ECommerce.Infrastructure` | EF Core persistence, interceptors, repositories, outbox/inbox implementation, identity (JWT/refresh tokens/OTP), email, seeding |
| `src/ECommerce.Api` | HTTP layer: minimal API endpoints, OpenAPI, ProblemDetails mapping |
| `tests/ECommerce.Domain.UnitTests` | Unit tests for domain logic and invariants |
| `tests/ECommerce.ArchitectureTests` | Layer dependency and convention rules (NetArchTest) |
| `tests/ECommerce.IntegrationTests` | End-to-end tests against a real SQL Server database |

## Patterns and Practices

- **Result pattern** — operations return `Result` / `Result<T>` instead of throwing for expected failures; errors carry type codes mapped to appropriate HTTP responses.
- **CQRS** — read side uses untracked queries exposed via `IApplicationDbContext`; write side goes through repositories and the unit of work.
- **Domain events** — aggregates collect events; an interceptor flushes them into the outbox on save, keeping the model persistence-ignorant.
- **Repository + Unit of Work** — per-aggregate repositories over a shared `DbContext` implementing `IUnitOfWork`.
- **Soft delete & auditing** — implemented as global EF Core interceptors so callers cannot forget them.
- **Seeding** — brand/type reference data seeded from JSON and a default admin user on startup in development.

## Authentication & Authorization

ASP.NET Core Identity with users/roles stored in SQL Server (`app` schema), JWT bearer access tokens with rotating refresh tokens:

- **Roles** — `Admin` and `Customer`. Read endpoints require an authenticated user; write endpoints require `Admin`.
- **Verified registration** — `POST /api/auth/register` creates the user with `EmailConfirmed = false` and returns no tokens; a 6-digit OTP is emailed. `POST /api/auth/confirm-email` verifies it, after which login succeeds. Until confirmed, login returns `401` `Auth.EmailNotConfirmed`; re-registering an unconfirmed email resends a new code.
- **Access tokens** — JWT HS256, 2-hour expiry, carrying user id, email, name, and roles.
- **Refresh tokens** — 30-day expiry, stored **hashed** (SHA-256), **single-use with rotation** (the used token is revoked and a new one issued), and revocable (`/api/v1/auth/revoke`). Reusing an already-rotated token returns `401`.
- **OTP** — hashed at rest, 5-minute expiry, 5 failed-attempt lockout, single-use. Serves two purposes: `EmailVerification` (confirm-email) and `PasswordReset` (reset-password flow: `send-otp` → `verify-otp` → `reset-password`).
- **Email delivery** — MailKit/SMTP when `Smtp:Host` is configured; otherwise development falls back to the console-logging sender (OTP codes are printed in the logs).
- **Default admin** —  Default admin is seeded in development alongside the catalog reference data.

## Transactional Messaging (Outbox / Inbox)

Domain events must never be lost or duplicated. This solution guarantees **at-least-once delivery with exactly-once side effects**:

```
SaveChanges ──► OutboxInterceptor writes OutboxMessage rows
                (same DB transaction as business data)

OutboxProcessor (background service)
 ├─ acquires sp_getapplock (single active instance)
 ├─ loads Pending messages ordered by OccurredOnUtc
 ├─ deserializes and publishes via MediatR
 ├─ handlers call IInbox.TryClaimAsync → duplicate deliveries suppressed
 └─ marks Processed, retries failures up to MaxAttempts,
    then dead-letters with the stored error
```

Design details:

- The inbox claim row is inserted through the same `DbContext`, so claims commit **atomically** with outbox status updates.
- A SQL Server session-level application lock (`sp_getapplock`) ensures only one processor instance drains the queue per tick when running multiple API replicas.
- Unknown event types and repeated failures are logged and dead-lettered instead of crashing the host.

## Tech Stack

| Area | Technology |
|---|---|
| Runtime | .NET 10 |
| Web | ASP.NET Core Minimal APIs, OpenAPI |
| Data | Entity Framework Core 10, SQL Server |
| Identity & auth | ASP.NET Core Identity, JWT Bearer, hash-based refresh tokens |
| Email | MailKit / SMTP (console-fallback in development) |
| Messaging | MediatR, transactional Outbox/Inbox |
| Validation | FluentValidation |
| Testing | xUnit, NetArchTest |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Developer edition, Docker, etc.)

### Setup

```bash
git clone https://github.com/karimelkazaz/ECommerce.git
cd ECommerce

# configure your connection string in src/ECommerce.Api/appsettings.json
# "ConnectionStrings": { "DefaultConnection": "Server=.;Database=ECommerceDb;Trusted_Connection=True;TrustServerCertificate=True;" }

dotnet ef database update --project src/ECommerce.Infrastructure --startup-project src/ECommerce.Api

dotnet run --project src/ECommerce.Api
```

The API is available at `https://localhost:<port>` with OpenAPI documents exposed in development. Reference data (brands/types) and a default admin user are seeded automatically on startup.

> **Note:** JWT/SMTP secrets live in `appsettings.Development.json`. When running EF migrations on the command line, set the environment first:
> `$env:ASPNETCORE_ENVIRONMENT='Development'` (PowerShell) so the design-time host picks up that configuration.

## API Endpoints

Base route: `/api/v{version}` (currently `v1`, e.g. `/api/v1/products`)

### Catalog & management

Read endpoints (`GET`) require an authenticated user; write endpoints (`POST`/`PATCH`/`DELETE`) require the `Admin` role.

| Method | Route | Description |
|---|---|---|
| GET | `/api/v1/products` | List products — search, filter by brand/type, paginated |
| GET | `/api/v1/products/{id}` | Get product by ID |
| POST | `/api/v1/products` | Create product |
| PATCH | `/api/v1/products/{id}/price` | Change product price (raises price-changed event) |
| DELETE | `/api/v1/products/{id}` | Soft-delete product |
| GET | `/api/v1/brands` | List brands |
| GET | `/api/v1/brands/{id}` | Get brand by ID |
| POST | `/api/v1/brands` | Create brand |
| PATCH | `/api/v1/brands/{id}/name` | Rename brand |
| DELETE | `/api/v1/brands/{id}` | Delete brand |
| GET | `/api/v1/types` | List types |
| GET | `/api/v1/types/{id}` | Get type by ID |
| POST | `/api/v1/types` | Create type |
| PATCH | `/api/v1/types/{id}/name` | Rename type |
| DELETE | `/api/v1/types/{id}` | Delete type |

### Auth (anonymous except where noted)

| Method | Route | Description |
|---|---|---|
| POST | `/api/v1/auth/register` | Register (unconfirmed) → `{ email, verificationCodeResent, message }`, no tokens |
| POST | `/api/v1/auth/confirm-email` | Confirm email with the emailed OTP → `204` |
| POST | `/api/v1/auth/login` | Login (confirmed users) → access + refresh tokens |
| POST | `/api/v1/auth/refresh` | Rotate refresh token → new token pair |
| POST | `/api/v1/auth/revoke` | Revoke refresh token → `204` |
| POST | `/api/v1/auth/send-otp` | Send OTP for `EmailVerification` or `PasswordReset` |
| POST | `/api/v1/auth/verify-otp` | Verify an OTP |
| POST | `/api/v1/auth/reset-password` | Reset password using a verified OTP → `204` |

Errors are returned as RFC 7807 ProblemDetails, including validation errors and auth failures (e.g., `401 Auth.EmailNotConfirmed`).

## Testing

```bash
dotnet test ECommerce.slnx
```

| Suite | Scope | Notes |
|---|---|---|
| Domain.UnitTests | Aggregates, invariants, state transitions | Fast, no infrastructure |
| ArchitectureTests | Layer boundaries, persistence ignorance, conventions | Fail the build on violations |
| IntegrationTests | Full Outbox → MediatR → Inbox pipeline | Requires local SQL Server; creates and drops its own scratch database per run |

---

> **Status:** Active development — catalog and identity (registration, login, refresh tokens, OTP) are complete; orders exist in the domain with checkout, payments, and shipping planned next.
