# ECommerce Platform

A production-oriented e-commerce backend built with **.NET 10** and **Clean Architecture**, featuring a rich domain model, CQRS with MediatR, and reliable event-driven messaging through a transactional Outbox/Inbox pattern.

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
| `src/ECommerce.Infrastructure` | EF Core persistence, interceptors, repositories, outbox/inbox implementation, seeding |
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
- **Seeding** — brand/type reference data seeded from JSON on startup in development.

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

The API is available at `https://localhost:<port>` with OpenAPI documents exposed in development. Reference data (brands/types) is seeded automatically on startup.

## API Endpoints

Base route: `/api`

| Method | Route | Description |
|---|---|---|
| GET | `/api/products` | List products — search, filter by brand/type, paginated |
| GET | `/api/products/{id}` | Get product by ID |
| POST | `/api/products` | Create product |
| PATCH | `/api/products/{id}/price` | Change product price (raises price-changed event) |
| DELETE | `/api/products/{id}` | Soft-delete product |
| GET | `/api/brands` | List brands |
| GET | `/api/brands/{id}` | Get brand by ID |
| POST | `/api/brands` | Create brand |
| PATCH | `/api/brands/{id}/name` | Rename brand |
| DELETE | `/api/brands/{id}` | Delete brand |
| GET | `/api/types` | List types |
| GET | `/api/types/{id}` | Get type by ID |
| POST | `/api/types` | Create type |
| PATCH | `/api/types/{id}/name` | Rename type |
| DELETE | `/api/types/{id}` | Delete type |

Errors are returned as RFC 7807 ProblemDetails, including validation errors.

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

> **Status:** Active development — catalog features complete; orders exist in the domain with checkout, payments, shipping, and identity planned next.
