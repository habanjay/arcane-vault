---
applyTo: "src/**/*.cs"
---

# Backend service and API instructions

Apply these instructions when creating or changing an ASP.NET Core backend service or API. Do not apply the API layout to the Aspire AppHost or frontend.

## Project structure

Use this structure for a backend API project, adapting the project name to the service. Keep the pictured folder and file names unless the existing project has an established convention:

```text
<Service>.API/
├── Apis/
├── Extensions/
├── Infrastructure/
├── IntegrationEvents/
├── Model/
├── Pics/
├── Properties/
├── Services/
├── Setup/
├── <Service>.API.csproj
├── <Service>.API.http
├── <Service>.API.json
├── <Service>.API_v2.json
├── <Service>Options.cs
├── GlobalUsings.cs
├── Program.Testing.cs
├── Program.cs
├── appsettings.Development.json
└── appsettings.json
```

- Put HTTP endpoints and endpoint-specific request/response types in `Apis/`.
- Put reusable registration and application-pipeline extensions in `Extensions/`.
- Put persistence, external-system adapters, and other infrastructure concerns in `Infrastructure/`.
- Put integration event contracts and handlers in `IntegrationEvents/`.
- Put domain and API model types in `Model/`; do not expose persistence entities directly as public API contracts.
- Use `Pics/` only for project-specific image assets when needed. Do not add it if the service has no such assets.
- Put business use cases and application services in `Services/`.
- Put dependency-injection and other service setup modules in `Setup/`.
- Keep `Program.cs` focused on composing configuration, services, middleware, and endpoint mappings. Use `Program.Testing.cs` only for a deliberate test-host entry point or test-specific configuration.
- Keep the `.http` file as a runnable collection of representative API requests. Maintain the OpenAPI JSON files only when the project publishes or checks in those documents; do not create duplicate or stale specifications.
- Bind service configuration through a typed `<Service>Options` class and validate required settings at startup. Never store secrets in checked-in settings files.

## API design and implementation

- Use ASP.NET Core Minimal APIs; do not implement API endpoints with MVC controllers or `ControllerBase`.
- Define endpoint mapping modules in `Apis/`, grouped by resource or feature. Expose them as extension methods such as `MapUsersApi` that register routes on `IEndpointRouteBuilder`, and call those methods from `Program.cs` or a focused application-pipeline extension.
- Use `MapGroup` to organize related routes and apply shared prefixes, authorization, tags, and endpoint filters at the group level where appropriate. Keep endpoint handlers thin; delegate business rules to `Services/`.
- Follow the repository's existing ASP.NET Core and .NET target framework conventions.
- Use explicit, resource-oriented HTTP routes and correct HTTP methods and status codes. Keep endpoint handlers thin; delegate business rules to `Services/`.
- Validate untrusted input at the boundary. Use typed request/response contracts, nullable reference types, and asynchronous I/O with cancellation tokens where supported.
- Prefer `record` types for DTOs, including API request and response contracts. Use a `class` only when its identity, mutability, or inheritance semantics are needed.
- Return consistent Problem Details for errors. Do not expose stack traces, internal identifiers, secrets, or implementation details to callers; do not swallow exceptions or return success-shaped fallbacks.
- Keep persistence and external-service details behind `Infrastructure/` abstractions. Do not put business rules in endpoint mapping, configuration, or database code.
- Add OpenAPI metadata for routes, request and response types, status codes, and authentication requirements. Keep the runnable `.http` examples and any published OpenAPI documents aligned with the implementation.
- When changing a documented endpoint or its contract, update the relevant API documentation (for Arcane Vault, `docs/api.md`) in the same change.

## Security and reliability

- Prefer .NET BCL and ASP.NET Core capabilities over third-party libraries when they provide an equivalent solution. Add a third-party dependency only when the required capability is not adequately available in the platform.
- Before adding or updating a dependency, check its maintenance status and known security advisories. Do not introduce or use a dependency with a known vulnerability; choose a safe alternative or a non-vulnerable version, and address relevant vulnerable existing dependencies rather than suppressing the finding.
- Treat all request data, headers, tokens, files, and configuration as untrusted. Apply authorization at the resource boundary and enforce ownership checks for user-scoped data.
- Never log passwords, access or refresh tokens, encryption keys, vault contents, or other sensitive personal data. Avoid returning secrets in API responses.
- Use parameterized database operations and enforce relevant uniqueness, concurrency, and transaction rules in the infrastructure layer.
- Protect sensitive operations with appropriate authentication, authorization, rate limiting, and input-size limits. Validate uploaded files by both content and size when uploads are supported.
- Use the repository's configured resilience and telemetry patterns for outbound calls. Propagate cancellation and set appropriate timeouts.

## Verification

- Add or update focused tests for changed behavior, including validation, authorization/ownership, and error cases where relevant.
- Verify the service builds and run the smallest relevant test suite. Update `.http` examples and API documentation when the public contract changes.
- Do not add packages, architectural layers, or project files unless the requirement needs them and the existing solution has no suitable pattern.
