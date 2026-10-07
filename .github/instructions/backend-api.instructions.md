---
applyTo: "src/ArcaneVault.Server/**/*.cs"
---

# Arcane Vault backend API instructions

Apply these instructions when creating or changing the ASP.NET Core API in
`src/ArcaneVault.Server/`. Do not apply this API layout to the Aspire AppHost or
frontend. Match the existing .NET 10, nullable-reference-type, and Minimal API
conventions in the server project.

## Project structure

Keep API code in the established project folders:

```text
src/ArcaneVault.Server/
├── Apis/
├── Dtos/
├── Middleware/
├── Mocks/
├── Services/
├── Properties/
├── ArcaneVault.Server.csproj
├── ArcaneVault.Server.http
├── Extensions.cs
└── Program.cs
```

- Put resource endpoint modules and shared API helpers in `Apis/`. Add new
  resource modules there and register them through `ArcaneVaultEndpoints`.
- Put API request and response contracts in `Dtos/`, using the `Dtos` spelling
  and `ArcaneVault.Server.Dtos` namespace.
- Put service interfaces and API-specific service exceptions in `Services/`.
- Put mock service implementations, mock authentication, and in-memory mock
  stores in `Mocks/`. Do not put mock implementation files in `Services/`.
- Put centralized exception handling in `Middleware/`.
- Keep `Program.cs` focused on dependency registration, middleware, and mapping
  the top-level API and default endpoints.
- `Extensions.cs` contains the Aspire service defaults, including telemetry,
  resilience, and health-check registration/mapping. Extend the existing setup
  rather than adding a duplicate service-defaults implementation.
- Do not add folders such as `Infrastructure/`, `Model/`, or `Setup/` unless a
  concrete feature needs them. When real persistence or external adapters are
  introduced, keep them behind the existing service interfaces and add only the
  structure required for that implementation.
- Keep `ArcaneVault.Server.http` as the runnable manual request collection.
  Update it when routes or request contracts change. Update `docs/api.md` when
  the documented API contract changes.

## API design and implementation

- Use ASP.NET Core Minimal APIs; do not add MVC controllers.
- `ArcaneVaultEndpoints.MapArcaneVaultApi` creates the `/api/v1` group and
  applies `ApiValidationFilter`. Resource modules expose a static `Map` method
  that receives the appropriate route group. Follow this pattern rather than
  creating a competing endpoint-registration convention.
- Group routes by resource and add tags, names, summaries, descriptions, and
  `.Produces<T>()` metadata as appropriate. Keep endpoint handlers thin and
  delegate behavior to an interface in `Services/`.
- Keep authentication boundaries explicit. The `/api/v1` API has an
  authorization-required group and a separate anonymous authentication group.
  Put public routes in the anonymous group and use `.AllowAnonymous()` on
  endpoints intended to remain public. Do not move protected operations into an
  anonymous group.
- Use explicit, resource-oriented routes and correct HTTP methods/status codes.
  Creation routes should return `201 Created` with a `Location`; deletion
  routes should return `204 No Content`. Follow the route contract and examples
  in `docs/api.md`.
- Put input and output contracts in `Dtos/`, not beside individual endpoint
  modules. Prefer sealed records; use `required` init-only properties and data
  annotations for request contracts, and immutable positional records for
  responses. Document API-exposed DTOs with XML summaries. Use `DateTimeOffset`
  for date/time values.
- The API validation filter is applied to the root API route group. Keep
  boundary validation there and do not add a second validation mechanism
  without a demonstrated need.
- Endpoint handlers currently receive `CancellationToken` and check it before
  calling synchronous mock services. Preserve that behavior while calls remain
  synchronous. For asynchronous I/O, make service APIs asynchronous and pass
  the cancellation token through to the I/O operation.
- Keep service interfaces independent of the current mock implementation so
  storage can be replaced without changing endpoint handlers. `Program.cs`
  registers the mock implementation against the service interfaces; when
  sharing in-memory mock state, resolve all interfaces to the same singleton
  implementation. Choose appropriate lifetimes for non-mock implementations
  based on their dependencies.

## Errors, security, and reliability

- Use `ApiException` for expected API errors that need a specific status and
  error code. `ApiExceptionHandler` maps these to the established
  `ApiErrorResponse` envelope; status-code pages use the same envelope for
  otherwise-unhandled HTTP status codes. Do not replace this contract with a
  different error shape without updating clients and `docs/api.md`.
- Do not swallow exceptions, return success-shaped fallbacks, or expose
  implementation details, credentials, tokens, or vault contents in errors or
  logs.
- The bearer OpenAPI transformer derives security requirements from endpoint
  authorization metadata. Keep authorization metadata accurate, especially
  when adding anonymous endpoints.
- Use rate limiting for sensitive public operations, following the existing
  `"sensitive"` policy where appropriate. Validate uploaded files and enforce
  user ownership for user-scoped data.
- Mock data and mock credentials are for local/demo use only. Keep mock
  implementations in `Mocks/`; do not treat them as persistent or production
  storage.
- Prefer ASP.NET Core and .NET capabilities over additional dependencies.
  Before changing dependencies, check maintenance status and known advisories.
- Propagate cancellation and use the existing resilience and telemetry
  configuration for outbound calls.

## OpenAPI and health endpoints

- Use the built-in ASP.NET Core OpenAPI support configured in `Program.cs`.
  Scalar is the interactive API reference. Both OpenAPI and Scalar are mapped
  only in Development. Do not add Swashbuckle packages.
- Keep bearer security metadata consistent with endpoint authorization. Public
  operations must not appear as bearer-protected in the generated OpenAPI
  document.
- Aspire service defaults register `/health` readiness checks and `/alive`
  liveness checks. They are mapped only in Development in `Extensions.cs`; both
  endpoints are explicitly anonymous. `/health` evaluates all registered
  checks, while `/alive` evaluates only checks tagged `live`. Preserve the
  Development-only exposure unless deployment requirements are deliberately
  changed and reviewed.

## Verification

- Add or update focused tests for behavior changes, especially validation,
  authorization, ownership, and error cases.
- Build the server project and run the smallest relevant test suite. If an
  Aspire-launched process locks the normal output, use an isolated output path
  for validation rather than stopping a process that is not part of the task.
- Smoke-test changed routes where practical, including unauthenticated access
  for endpoints marked anonymous.
- Keep the `.http` examples and `docs/api.md` aligned with the implemented
  public contract.
- Do not add packages, layers, or project files unless the requirement needs
  them and the existing patterns cannot support the change.
