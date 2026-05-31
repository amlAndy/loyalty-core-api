---
applyTo: "src/**/*.cs"
---

# Security Rules

## Required

Validate all external input.
Use explicit DTOs for API contracts.
Use configuration for connection strings and secrets.
Use parameterized database access through EF Core.
Return safe error responses.
Use appropriate HTTP status codes.

## Forbidden

Do not hardcode secrets.
Do not log sensitive data.
Do not expose stack traces in production responses.
Do not return domain or EF entities directly from controllers.
Do not bypass the Application layer.
Do not create custom cryptography.
Do not weaken authorization or authentication for convenience.

## Logging

Use ILogger<T>.
Do not use Console.WriteLine in application code.
Log meaningful operational information without leaking sensitive data.
