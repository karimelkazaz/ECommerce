---
name: ECommerce Conventions
description: >
  Conventions for the ECommerce .NET codebase: clean architecture layering,
  strict Result pattern, CQRS handlers, Minimal API endpoints, error catalogs,
  identity specifics, and the required verification workflow. Load before
  writing, changing, or reviewing code in this repository.
---

# ECommerce Project Conventions

Apply these to every change in this repo. The architecture tests in
`tests/ECommerce.ArchitectureTests` enforce the structural rules; conventions
below are the de facto style of the existing code.

## Layering

```
Api → Application → Domain        and        Api → Application ← Infrastructure
```

- `Application` depends only on `Domain`. It declares cross-feature abstractions
  in `Application/Common/Interfaces/` (`IIdentityService`, `IApplicationDbContext`,
  `ICurrentUserService`, ...); `Infrastructure` implements them. Identity ASP.NET
  classes (`UserManager`, `SignInManager`) never appear outside `Infrastructure`.
- Feature-slice **inside** each layer: `Auth/`, `Baskets/`, `Orders/`, `Products/`
  ..., each with `Commands/`, `Queries/`, and `EventHandlers/` when needed.
- Response/DTO types live at the feature root (`Auth/AuthResponse.cs`,
  `Orders/OrderResponse.cs`), not inside `Commands/`.
- Domain aggregates live in `Domain/Entities/<Aggregate>/`: the entity (`sealed`,
  private constructors, `Create(...)` factory returning `Result<T>`), `<Xxx>Errors.cs`,
  `Events/` for domain events. Shared types in `Domain/Common/` (`Result`, `Error`,
  `ErrorType`, `AuthErrors`, `Roles`) and `Domain/ValueObjects/` (`Money`, `Address`).

## Result pattern (strict)

Every method typed `Task<Result>` or `Task<Result<T>>` declares its outcome in one
of exactly three forms:

1. `return Result.Failure(SomeErrors.Something);` / `return Result.Success(value);` — explicit.
2. `return verifyResult;` — echo another `Result`-returning call as-is (never re-wrap, never double-wrap).
3. Inside domain factory methods: `Result<DomainEntity>` built explicitly.

- **Never** rely on `Result<TValue>`'s implicit conversion (`return new XxxResponse(...)`
  or `return XxxResponse.FromDomain(entity)`). A null value silently becomes
  `Failure(General.Null)`; explicit wrapping kills that trap and keeps success
  exits greppable. For non-trivial constructions: build a `response` local, then
  `return Result.Success(response);`.
- Exceptions are used only for infrastructure-level handling (e.g. `StripeException`),
  which is caught, logged, and converted to `Result.Failure` before returning.
- Unexpected-null defensive paths (e.g. missing claim on an authenticated endpoint)
  return a typed failure (e.g. `AuthErrors.UserNotFound`), never throw.

## Error catalogs

- One static class per area: `Domain/Common/AuthErrors.cs` for cross-cutting auth,
  `Domain/Entities/<Aggregate>/<Xxx>Errors.cs` for aggregates.
- Shape: `public static readonly Error Code = new("Area.Code", "message", ErrorType.X);`
  Pick `ErrorType` deliberately — it drives the HTTP status via
  `Api/Common/ResultExtensions.ToProblemDetails()` (Validation→400, NotFound→404,
  Conflict→409, Unauthorized→401, Forbidden→403, else 500).
- Reuse an existing matching error before inventing a new one.

## CQRS handlers

- One file per command/query containing the record, the `sealed` handler, and the
  `AbstractValidator` (same file). Primary constructors only.
- Validation runs via `ValidationBehavior` (MediatR pipeline, `Application/Common/Behaviors/`);
  it can only short-circuit when the handler returns `Result`/`Result<T>` — keep it that way.
- Dupe the same guard-clause style: null/empty userId → `Result.Failure`, domain
  `Create`/`Update` `Result` checks before persisting, `unitOfWork.SaveChangesAsync` at the end.
- Current-user identity always comes from `ICurrentUserService` (`UserId`, `Email`),
  injected into the handler — commands carrying `UserId` in the body are only used
  where intentional (basket guest merge `/checkout` folder patterns). `userId is null ||
  userId == Guid.Empty` is the canonical guard.
- Paged read handlers (`GetOrdersQuery`) return `PagedResponse<T>` directly — that is
  intentionally not `Result<T>`. Don't convert them.

## Minimal API endpoints

- One static class per feature: `public static class XxxEndpoints` with
  `public static IEndpointRouteBuilder MapXxxEndpoints(this IEndpointRouteBuilder app)`,
  mapped in `Api/Common`/`Program.cs`.
- `app.MapGroup("/route").WithTags("Tag")`; per operation `.MapToApiVersion(1)`;
  `.RequireAuthorization()` at group level (public group only → per endpoint instead,
  e.g. auth `/me` inside the anonymous `/auth` group).
- Handlers are thin: `(CommandFromBody command, ISender sender, CancellationToken ct)`
  → send → `result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails()`
  (204-style side effects follow the existing endpoint: `Results.NoContent()`).
- Request-bound records go at the bottom of the endpoint file; commands validate.
- Adding a route → update the endpoint table in `README.md` (method, `/api/v1/...`,
  one-line description, **Auth** marker when protected).

## Options & configuration

- Options classes live beside their feature (`Infrastructure/.../JwtOptions.cs`,
  `OtpOptions`) with a `SectionName` const; bound in `Infrastructure/DependencyInjection.cs`.
- Fail fast at startup: extend the existing manual checks (JWT secret length,
  `RefreshTokenExpirationDays > 0`) or `.Validate(...).ValidateOnStart()` like `InboxOptions`.
  A bad value must crash startup, not throw mid-request.
- Never store secrets in config files; use environment substitution.

## Identity specifics

- All identity flows go through `Infrastructure/Persistence/Identity/IdentityService.cs`
  (`UserManager`/`SignInManager`). Login calls `CheckPasswordSignInAsync(user, pwd, lockoutOnFailure: true)`;
  never weaken lockout or reorder its error checks (`IsLockedOut → IsNotAllowed → !Succeeded`).
- Refresh tokens are the `RefreshToken` aggregate: hashed, single-use, rotated by
  revoke-old + issue-new; change both login and refresh through the shared helpers
  (`CreateAuthResponseAsync` / `IssueRefreshTokenAsync`).
- External adapter failures log via constructor-injected `ILogger<T>` with structured
  placeholders (e.g. the compensating-delete failure in `CreateUserAsync`).

## Provider configuration & migrations

- Persistence split across `AppDbContext` + `AppIdentityDbContext`; EF configurations in
  `Persistence/Configurations`, migrations under each context's own folder.
  Migrations via `dotnet ef` with `--context` when touching identity tables.

## Tests & verification workflow

- `Domain.UnitTests`: pure entity/VO tests. `IntegrationTests`: application-level tests;
  identity service tests are **stub-based** (custom `UserManager`/`SignInManager` types +
  `NullLogger<T>`, no DB) — extend that pattern per branch, and update direct
  `new IdentityService(...)` constructions when its constructor changes.
- `ArchitectureTests`: layer/convention rules; must stay green with new code.
- Required verification for any change: `dotnet build ECommerce.slnx`, then
  `dotnet test` on `ECommerce.Domain.UnitTests`, the affected `ECommerce.IntegrationTests`
  filters (full integration suite needs the compose databases — don't assume it runs),
  and `ECommerce.ArchitectureTests`.
- If a build fails with "file is being used by another process" (MSB3027-style),
  run `scripts\dev-clean.ps1` first: it stops leftover dev-server / testhost processes
  (including agent-session copies under `%TEMP%\ECommerce-*`), shuts down stale
  MSBuild/Roslyn build servers, and with `-Build` rebuilds in one step.
- Build warnings from `System.Security.Cryptography.Pkcs` (NU1902/NU1903) are known and
  unrelated; don't chase them in ordinary changes.
