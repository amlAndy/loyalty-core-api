# Feature Development Guide (Amended)

This document defines the **strict, repeatable structure** for building features in this template.
It is written to be followed by both developers and AI tools.

---

## Overview

Each feature must follow the layered architecture:

- API
- Application
- Domain
- Infrastructure

Flow:

- API -> Application -> Domain
- Application -> Infrastructure when persistence or external systems are needed

---

## API Layer

Project:

- DotNetApiTemplate.Api

Purpose:

- Expose the feature via HTTP.

What to add:

- Controller
- Request DTO
- Response DTO

Structure:

- Controllers/
- Contracts/
  - Requests/
  - Responses/

Rules:

- Controllers must be thin.
- Controllers must call Application services.
- Controllers must not contain business logic.
- Controllers must not access DbContext directly.
- Controllers must not return Domain entities directly.
- Use `AutoMapper` for mapping between API request/response contracts and Application DTOs, commands, or results.
- Keep mapping configuration in API mapping profiles and keep business logic out of mappings.

---

## Application Layer

Project:

- DotNetApiTemplate.Application

Purpose:

- Implement the business use case.

---

## CRITICAL: Folder Structure Rules

All features MUST follow this exact structure:

Features/{Area}/{Operation}/{Concern}

Where:

- Area = logical grouping (e.g. System, Members, Orders)
- Operation = a single use case (e.g. Health, Echo, Create, GetById)
- Concern = one of:
  - Dtos
  - Interfaces
  - Services
  - Validation

---

## HARD RULES (Non-Negotiable)

1. No files are allowed directly under Features/{Area}.
2. Every operation must have its own folder.
3. Do not create shared Dtos, Services, Interfaces, or Validation folders at the Area level.
4. Do not mix multiple operations in one folder.
5. Each operation must be fully self-contained.

---

## Correct Example

Features/
└── System
    ├── Health
    │   ├── Dtos
    │   │   └── SystemHealthDto.cs
    │   ├── Interfaces
    │   │   └── IHealthService.cs
    │   ├── Services
    │   │   └── HealthService.cs
    │   └── Validation
    │
    ├── Info
    │   ├── Dtos
    │   │   └── SystemInfoDto.cs
    │   ├── Interfaces
    │   │   └── IInfoService.cs
    │   ├── Services
    │   │   └── InfoService.cs
    │   └── Validation
    │
    └── Echo
        ├── Dtos
        │   ├── EchoCommand.cs
        │   └── EchoResult.cs
        ├── Interfaces
        │   └── IEchoService.cs
        ├── Services
        │   └── EchoService.cs
        └── Validation
            └── EchoCommandValidator.cs

---

## Naming Conventions (Mandatory)

Dtos:
- {Operation}Command
- {Operation}Result
- {Operation}Dto (for simple GET operations)

Interfaces:
- I{Operation}Service

Services:
- {Operation}Service

Validation:
- {Operation}CommandValidator

---

## Anti-Patterns (Must NOT Be Used)

❌ Features/System/Dtos  
❌ Features/System/Services  
❌ Features/System/Interfaces  
❌ Features/System/Validation  
❌ ISystemService (unless truly cross-operation)  
❌ MemberService handling multiple operations  
❌ Mixing Create/Get/Update logic in one service  

---

## Validation Rules

- Validation must exist inside each operation folder only.
- Use FluentValidation.
- Validate Application DTOs (Commands).
- Controllers must not contain validation logic.
- Domain enforces business invariants, not request validation.

---

## Domain Layer

Project:

- DotNetApiTemplate.Domain

Purpose:

- Define core business models and rules.

What to add:

- Entities
- Value objects
- Enums
- Domain exceptions
- Domain logic

Rules:

- Domain must not depend on other layers.
- Domain must not reference EF Core.
- Domain must not reference ASP.NET Core.
- Domain must enforce business rules and invariants.

---

## Infrastructure Layer

Project:

- DotNetApiTemplate.Infrastructure

Purpose:

- Handle persistence and external systems.

What to add:

- DbContext
- EF Core configuration
- Repositories (if used)
- External services

Rules:

- No business logic.
- No hardcoded configuration.
- Implements Application abstractions.

---

## End-to-End Flow

1. Client calls API endpoint.
2. Controller receives request.
3. Controller maps request DTO to Application command.
4. Controller calls Application service.
5. Validation runs in Application.
6. Application coordinates the use case.
7. Domain enforces rules.
8. Infrastructure handles persistence (if needed).
9. Application returns result.
10. Controller maps to response DTO.
11. Response is returned.

---

## Key Principle

Always place code in the lowest appropriate layer:

- HTTP → API
- Use-case logic → Application
- Business rules → Domain
- Data/external systems → Infrastructure

---

## What This Gives You

- Predictable structure
- AI-friendly generation
- Clean separation of concerns
- Scalable architecture
