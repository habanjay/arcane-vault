# Getting started

This guide runs the Arcane Vault API and client locally on Windows. The client
is a UI prototype; its authentication and screen data are not connected to the
API.

## Prerequisites

- .NET 10 SDK
- Node.js `20.19+` or `22.12+`
- SQL Server LocalDB and `sqlcmd`
- PowerShell

The API's checked-in development connection string targets
`(localdb)\MSSQLLocalDB`. For another SQL Server instance, see
[database setup](database.md).

## Prepare the database

From the repository root, create the local database and apply its initial schema:

```powershell
$createDatabase = "IF DB_ID(N'ArcaneVault') IS NULL " +
    "CREATE DATABASE ArcaneVault;"
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q $createDatabase
$migration = "src\ArcaneVault.Server\Database\Migrations\001_Initial.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d ArcaneVault -i $migration
```

The API does not apply migrations automatically. The migration is intended
for an empty database; do not rerun it on a database that already has the
schema.

## Install client dependencies

From the repository root:

```powershell
Set-Location src\ArcaneVault.Client
npm ci
Set-Location ..\..
```

## Run the application

Start the Aspire AppHost from the repository root:

```powershell
dotnet run --project src\ArcaneVault.AppHost
```

Use the Aspire dashboard URLs printed by the AppHost to open the client and
API. The API's interactive Scalar reference is available at `/scalar` on its
server endpoint when running in Development. API routes are rooted at
`/api/v1`.

To try the documented requests, open
[ArcaneVault.Server.http](../src/ArcaneVault.Server/ArcaneVault.Server.http)
in VS Code with a REST client extension and set its `baseUrl` to the server
URL shown in the Aspire dashboard.

## Current client/API boundary

The client currently uses local mock authentication and does not make requests
to the API. The backend is implemented separately and can be explored using
the `.http` examples or the [API reference](api.md). Register an API account
with `POST /api/v1/auth/register`; there is no seeded API account.

## Protect local data

Vault passwords and notes use ASP.NET Core Data Protection. Keep the API's key
ring in `src\ArcaneVault.Server\App_Data\DataProtection-Keys`, back it up, and
restrict access. Losing those keys makes already-stored vault secrets
unrecoverable. See [database setup](database.md#secret-protection-key-ring)
for configuration details.
