# DotNet API Template

Reusable ASP.NET Core Web API template with:

- Controllers instead of Minimal APIs
- Layered architecture: `Api`, `Application`, `Domain`, `Infrastructure`
- SQL Server with EF Core
- Centralised package management
- Clear separation of concerns
- Copilot-friendly project conventions

## Contents

- [Architecture](#architecture) - High-level project structure.
- [Layer Responsibilities](#layer-responsibilities) - What belongs in each layer.
- [Dependency Rules](#dependency-rules) - Allowed project reference directions.
- [Engineering Standards](#engineering-standards) - Shared build and code quality defaults.
- [API Design Rules](#api-design-rules) - Rules for controllers and API contracts.
- [Development Principles](#development-principles) - General implementation guidance.
- [Local SMTP Setup](#local-smtp-setup) - How to configure the SMTP password safely for local development.
- [Adding a Feature](#adding-a-feature-required-pattern) - The expected implementation pattern for new features.
- [Forbidden Patterns](#forbidden-patterns) - Things that should not be done in this codebase.
- [Status](#status) - Current project status and likely next steps.

## Architecture

```text
DotNetApiTemplate.Api
DotNetApiTemplate.Application
DotNetApiTemplate.Domain
DotNetApiTemplate.Infrastructure
```

## Layer Responsibilities

### Api

- Controllers
- HTTP concerns
- Middleware
- Composition root / DI
- Request and response DTOs

### Application

- Use cases and services
- Business orchestration
- Validation
- Interfaces

### Domain

- Entities
- Value objects
- Business rules
- No external dependencies

### Infrastructure

- EF Core and `DbContext`
- Repositories
- External integrations
- Persistence concerns

## Dependency Rules

Allowed directions:

- `Api` -> `Application`
- `Api` -> `Infrastructure`
- `Application` -> `Domain`
- `Infrastructure` -> `Application`
- `Infrastructure` -> `Domain`
- `Domain` -> none

These rules must not be broken.

## Engineering Standards

- Central package management via `Directory.Packages.props`
- Shared build configuration via `Directory.Build.props`
- Nullable reference types enabled
- Implicit usings enabled
- Code style enforced via `.editorconfig`

## API Design Rules

- Controllers must return `ActionResult<T>` where appropriate
- Always use request and response DTOs
- Never return domain entities directly
- Use appropriate HTTP status codes such as `200`, `201`, `400`, `404`, and `500`
- Support cancellation tokens where applicable

## Development Principles

- Controllers must be thin
- Business logic belongs in the `Application` layer
- `Domain` must not depend on `Infrastructure`
- `Infrastructure` must not contain business logic
- Prefer explicit, readable code over clever abstractions

## Local SMTP Setup

For local development, keep `MailSettings:SmtpPassword` out of `DotNetApiTemplate.Api/appsettings.Development.json` and store it in user secrets instead.

### Visual Studio

1. Right-click `DotNetApiTemplate.Api` in Solution Explorer.
2. Select `Manage User Secrets`.
3. Add the SMTP password to the opened `secrets.json` file:

```json
{
  "MailSettings": {
    "SmtpPassword": "your-smtp-password"
  }
}
```

4. Save the file.
5. Keep the other SMTP settings such as `SmtpServer`, `SmtpUserName`, `FromAddress`, and `FromName` in `DotNetApiTemplate.Api/appsettings.Development.json`.

### CLI fallback

`dotnet user-secrets set "MailSettings:SmtpPassword" "your-smtp-password" --project DotNetApiTemplate.Api`

### Notes

- `DotNetApiTemplate.Api` already has a `UserSecretsId` configured
- User secrets are stored outside the repository under the current developer profile
- Never commit SMTP passwords or other secrets to source control

## Adding a Feature (Required Pattern)

Every feature should follow this structure:

1. Request DTO in the `Api` layer
2. Response DTO in the `Api` layer
3. Controller endpoint in the `Api` layer
4. Application service interface in the `Application` layer
5. Application service implementation in the `Application` layer
6. Validation in the `Application` layer
7. Persistence updates in the `Infrastructure` layer
8. `DbContext` update if needed

## Forbidden Patterns

- Business logic in controllers
- Direct `DbContext` usage in controllers
- `Domain` referencing `Infrastructure`
- Returning domain entities directly to clients
- Hardcoded configuration values
- Skipping the `Application` layer

## Status

Baseline template complete for the initial structure and standards phase.

Likely next steps:

- Cross-cutting concerns such as error handling, validation, and logging
- AI governance and repository guidance
- First feature vertical slice
