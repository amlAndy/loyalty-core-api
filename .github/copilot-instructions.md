# Copilot Instructions

## Project Guidelines
- For this repo, do not guess API contracts from the frontend; only implement explicitly confirmed auth endpoints and DTOs. Keep auth implementation minimal: AuthController, IAuthService/AuthService, IUserAuthRepository only if needed, ISsoTokenProcessor only if needed, DTOs, and validators. Do not implement GET /api/auth/session unless explicitly confirmed. Use POST /api/auth/refresh with RefreshSessionRequestDto { refreshToken: string } and AuthSessionDto response. Keep SsoLoginResultDto aligned to the normal auth session result needed by the frontend auth flow.
- For this repo, do not implement admin role contract fields like ParentRoleId, IsRootRole, and IsSystemRole from fixed mappings; support them in the backend role schema and database model instead, while keeping the endpoint response shape exactly aligned to the frontend contract and not adding extra endpoints.
- When implementing API slices from Angular contracts, keep DTOs exactly aligned to the frontend contract. Do not add paging, sorting, or extra metadata unless explicitly required. Keep protected endpoints using the current authorization approach in a way that can later move to permission-based authorization.

## General Instructions
- Include 'Done' in responses when work is finished.
