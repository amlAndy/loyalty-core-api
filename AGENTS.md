# Agent Instructions

This repository is a reusable ASP.NET Core Web API template.

## Source of Truth

All architecture, coding, and security rules are defined in:

- .github/copilot-instructions.md
- .github/instructions/backend-api.instructions.md
- .github/instructions/security.instructions.md

Agents must read and follow these files before making changes.

Do not duplicate or override rules defined in those files.

---

## Objective

Maintain a secure, maintainable, controller-based backend API template
with strict layered architecture.

---

## Required Workflow

For any change:

1. Identify the affected layer (Api, Application, Domain, Infrastructure)
2. Follow dependency rules from Copilot instructions
3. Implement full end-to-end changes where required
4. Keep code production-ready (no placeholders unless explicitly requested)
5. Ensure consistency with existing patterns

---

## Behaviour Rules

- Do not introduce new architectural patterns unless explicitly requested
- Do not bypass Application layer
- Do not put business logic in controllers
- Do not access DbContext from Api layer
- Do not expose domain entities directly

---

## Feature Implementation Expectation

When implementing features, follow the defined pattern:

- Request DTO
- Response DTO
- Controller endpoint
- Application service interface
- Application service implementation
- Domain updates if required
- Infrastructure updates if required

Refer to Copilot instructions for exact implementation rules.

---

## Output Expectations

- Prefer complete, production-ready files
- Keep code clear and explicit
- Avoid unnecessary abstraction
- Maintain consistency with the template

---

## Summary

- Use .github instructions as the rule set
- Use this file for workflow and behaviour
- Do not duplicate rules across files
