---
applyTo: "src/**/*.cs"
---

# Backend API Coding Rules

## General

Follow the repository layered architecture.

Do not introduce new architectural patterns unless explicitly requested.

## Controllers

Controllers must:
- inherit from ControllerBase
- use ApiController
- use route attributes
- return ActionResult or ActionResult<T>
- call Application services
- support CancellationToken where practical

Controllers must not:
- use DbContext directly
- contain business logic
- perform mapping-heavy workflows
- return domain entities directly

## Application Layer

Application services should:
- express business use cases
- depend on abstractions where practical
- coordinate validation and persistence
- remain independent of ASP.NET Core

## Domain Layer

Domain code should:
- remain framework-independent
- model business concepts clearly
- avoid persistence attributes unless explicitly agreed

## Infrastructure Layer

Infrastructure code should:
- implement persistence concerns
- contain DbContext configuration
- use EF Core safely
- avoid business decision logic

## Feature Implementation Pattern

When adding a feature, create or update:

1. Request DTO
2. Response DTO
3. Controller endpoint
4. Application service interface
5. Application service implementation
6. Domain entity or value object if needed
7. Infrastructure persistence if needed
8. DbContext if needed
9. Tests if tests exist for the affected layer
