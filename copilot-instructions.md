# GitHub Copilot Instructions

This repository is a reusable ASP.NET Core Web API template.

## Architecture

Use the existing layered architecture:

- Api
- Application
- Domain
- Infrastructure

## Required Dependency Direction

Api may reference Application and Infrastructure.
Application may reference Domain.
Infrastructure may reference Application and Domain.
Domain must not reference any other project.

Do not break these dependency rules.

## Controller Rules

Controllers must be thin.

Controllers may:
- receive HTTP requests
- validate routing/model state concerns
- call Application services
- return appropriate HTTP responses

Controllers must not:
- contain business logic
- access DbContext directly
- return EF/domain entities directly
- perform persistence logic

## Application Rules

Business workflows belong in the Application layer.

Use Application services or use-case classes for:
- business orchestration
- validation coordination
- mapping between API contracts and domain models where appropriate
- transaction workflow coordination

## Domain Rules

The Domain layer contains:
- entities
- value objects
- enums
- domain rules
- domain exceptions

The Domain layer must not depend on:
- EF Core
- ASP.NET Core
- Infrastructure
- external services

## Infrastructure Rules

Infrastructure contains:
- EF Core DbContext
- entity configurations
- repositories
- external service implementations
- persistence concerns

Infrastructure must not contain business rules.

## API Contract Rules

Use explicit request and response DTOs.

Never expose domain entities directly from API controllers.

## Data Rules

Use SQL Server via EF Core.
Connection strings must come from configuration.
Never hardcode secrets, credentials, or environment-specific values.

## Style Rules

Prefer clear, explicit, maintainable code.
Use async APIs for I/O.
Use cancellation tokens where applicable.
Keep files focused and single-purpose.
