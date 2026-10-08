# Server database setup

The server uses SQL Server stored procedures. The initial schema and stored procedures are in [001_Initial.sql](../src/ArcaneVault.Server/Database/Migrations/001_Initial.sql). This is a one-time migration for an empty `ArcaneVault` database.

## Local SQL Server LocalDB

From the repository root, create the database and apply the migration:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "IF DB_ID(N'ArcaneVault') IS NULL CREATE DATABASE ArcaneVault;"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d ArcaneVault -i "src\ArcaneVault.Server\Database\Migrations\001_Initial.sql"
```

The checked-in development connection string targets this LocalDB database. For another SQL Server instance, set `ConnectionStrings__ArcaneVault` (or `ConnectionStrings:ArcaneVault`) rather than changing application code. Do not commit credentials.

## Secret-protection key ring

Vault passwords and notes are protected with ASP.NET Core Data Protection before database storage. The default key-ring directory is `src\ArcaneVault.Server\App_Data\DataProtection-Keys`; override it with `DataProtection__KeyDirectory` when hosting elsewhere. Back up this directory and restrict filesystem access. Losing the key ring makes existing encrypted vault values unrecoverable, and sharing an unprotected key ring exposes those values.

Master-password verifiers and bearer/refresh token hashes are stored in the database; raw passwords and tokens are not.

## Current limitations

- No account or vault demo data is seeded; register an account through `/api/v1/auth/register`.
- The API has no second-factor enrollment flow yet. Enabling two-factor authentication is rejected rather than accepting a fixed demonstration code.
- Run the migration before starting the API. The application does not apply schema changes automatically.
