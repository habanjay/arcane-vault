# Arcane Vault — Entity Relationship Diagram & Data Model

This data model is derived from the UI and mock data contracts in [src/ArcaneVault.Client/src](../src/ArcaneVault.Client/src) (components + `utils/*.ts`), normalized into a relational schema suitable for the ASP.NET Core API backing [src/ArcaneVault.Server](../src/ArcaneVault.Server).

> **Implemented SQL schema:** [001_Initial.sql](../src/ArcaneVault.Server/Database/Migrations/001_Initial.sql) is authoritative for the current server. It uses `uniqueidentifier` primary and foreign keys directly (instead of the `BIGINT`/`PublicId` split shown in this design reference) and stores Data Protection ciphertext as Base64 text in the `EncryptedPassword`/`EncryptedNotes` columns.

## 1. UI → Data mapping

| UI source | TypeScript contract | Backing table |
|---|---|---|
| [auth.ts](../src/ArcaneVault.Client/src/utils/auth.ts) `AuthUser` | `id, email, firstName, lastName, role` | `Users` |
| [settingsData.ts](../src/ArcaneVault.Client/src/utils/settingsData.ts) `SettingsProfile` | profile fields | `Users` |
| [settingsData.ts](../src/ArcaneVault.Client/src/utils/settingsData.ts) `SecuritySetting`, `SettingsPreferences` | 2FA, auto-lock, theme, reminders | `UserSecuritySettings` |
| [ChangePhoto.tsx](../src/ArcaneVault.Client/src/components/ChangePhoto.tsx) | profile photo upload | `Users.ProfilePhotoData`, `ProfilePhotoContentType`, `ProfilePhotoUrl` |
| [dashboardData.ts](../src/ArcaneVault.Client/src/utils/dashboardData.ts) `PasswordCategory`, `VaultCategory` | name, icon, tone/color, count | `Categories` |
| [CreateCategory.tsx](../src/ArcaneVault.Client/src/components/CreateCategory.tsx) | name, description, icon, color | `Categories` |
| [dashboardData.ts](../src/ArcaneVault.Client/src/utils/dashboardData.ts) `VaultPassword`, `RecentPassword` | service, account, favorite, tone, category | `VaultItems` |
| [AddPassword.tsx](../src/ArcaneVault.Client/src/components/AddPassword.tsx) | site address, username, password, tags | `VaultItems`, `Tags`, `VaultItemTags` |
| [EditPassword.tsx](../src/ArcaneVault.Client/src/components/EditPassword.tsx) | updates credential, favorite toggle | `VaultItems`, `PasswordHistory` |
| [PasswordGenerator.tsx](../src/ArcaneVault.Client/src/components/PasswordGenerator.tsx) | strength score | `VaultItems.PasswordStrengthScore` (computed, not persisted as a generator) |
| [auditData.ts](../src/ArcaneVault.Client/src/utils/auditData.ts) `AuditEvent` | event, detail, actor, timestamp, severity | `AuditLogs` |
| Audit "New sign-in" / "Failed sign-in" events | device/session context | `UserSessions` |
| Audit "Vault shared" event | sharing a credential | `VaultItemShares` |

> **Zero-knowledge principle:** Arcane Vault never stores master passwords or vault secrets in plain text. All secret fields are encrypted client-side (or at minimum server-side with a per-user data key) before persistence — see [§6 Security](#6-security--performance-best-practices).

## 2. Entity Relationship Diagram

```mermaid
erDiagram
    USERS ||--|| USER_SECURITY_SETTINGS : "has"
    USERS ||--o{ CATEGORIES : "owns"
    USERS ||--o{ TAGS : "defines"
    USERS ||--o{ VAULT_ITEMS : "owns"
    USERS ||--o{ AUDIT_LOGS : "generates"
    USERS ||--o{ USER_SESSIONS : "authenticates"
    USERS ||--o{ VAULT_ITEM_SHARES : "receives"
    CATEGORIES ||--o{ VAULT_ITEMS : "groups"
    VAULT_ITEMS ||--o{ VAULT_ITEM_TAGS : "tagged with"
    TAGS ||--o{ VAULT_ITEM_TAGS : "applied to"
    VAULT_ITEMS ||--o{ PASSWORD_HISTORY : "revises"
    VAULT_ITEMS ||--o{ AUDIT_LOGS : "referenced by"
    VAULT_ITEMS ||--o{ VAULT_ITEM_SHARES : "shared as"

    USERS {
        bigint UserId PK
        uniqueidentifier PublicId UK "non-enumerable API id"
        varchar_254 Email UK
        varchar_255 MasterPasswordHash "Argon2id, never plaintext"
        varchar_50 FirstName
        varchar_50 LastName
        varchar_20 Role "Owner | Admin | Member"
        varchar_512 ProfilePhotoUrl NULL
        datetime2 CreatedAt
        datetime2 UpdatedAt
        datetime2 LastLoginAt NULL
        bit IsActive
        rowversion RowVersion "optimistic concurrency"
    }

    USER_SECURITY_SETTINGS {
        bigint UserId PK_FK
        bit TwoFactorEnabled
        varchar_100 TwoFactorSecretEncrypted NULL
        bit AutoLockEnabled
        smallint AutoLockMinutes "default 15"
        varchar_20 Theme "Light | Dark | System"
        bit SecurityRemindersEnabled
        datetime2 UpdatedAt
    }

    CATEGORIES {
        bigint CategoryId PK
        bigint UserId FK
        varchar_32 Name
        varchar_90 Description NULL
        varchar_8 Icon "glyph token"
        varchar_16 Color "pink|blue|green|orange|purple"
        datetime2 CreatedAt
        datetime2 UpdatedAt
    }

    TAGS {
        bigint TagId PK
        bigint UserId FK
        varchar_32 Name
        datetime2 CreatedAt
    }

    VAULT_ITEMS {
        bigint VaultItemId PK
        uniqueidentifier PublicId UK
        bigint UserId FK
        bigint CategoryId FK_NULL
        varchar_100 ServiceName
        varchar_2048 SiteUrl NULL
        varchar_254 Username
        varbinary_max EncryptedPassword "AES-256-GCM ciphertext"
        varbinary_16 EncryptionNonce
        varbinary_max EncryptedNotes NULL
        varchar_4 IconInitial NULL
        varchar_16 ColorTone NULL
        tinyint PasswordStrengthScore "0-4, cached"
        bit IsFavorite
        datetime2 LastUsedAt NULL
        datetime2 CreatedAt
        datetime2 UpdatedAt
        rowversion RowVersion
    }

    VAULT_ITEM_TAGS {
        bigint VaultItemId PK_FK
        bigint TagId PK_FK
    }

    PASSWORD_HISTORY {
        bigint PasswordHistoryId PK
        bigint VaultItemId FK
        varbinary_max EncryptedPassword
        varbinary_16 EncryptionNonce
        datetime2 ChangedAt
        bigint ChangedByUserId FK
    }

    AUDIT_LOGS {
        bigint AuditLogId PK
        bigint UserId FK_NULL "null when actor unknown"
        bigint VaultItemId FK_NULL
        varchar_50 EventType "enum, see §4"
        varchar_200 Detail NULL
        varchar_10 Severity "Success|Review|Critical"
        varchar_45 IpAddress NULL
        varchar_256 UserAgent NULL
        datetime2 CreatedAt
    }

    USER_SESSIONS {
        bigint SessionId PK
        bigint UserId FK
        varchar_100 DeviceName NULL
        varchar_45 IpAddress NULL
        varchar_256 UserAgent NULL
        datetime2 CreatedAt
        datetime2 LastActiveAt
        datetime2 ExpiresAt
        bit IsRevoked
    }

    VAULT_ITEM_SHARES {
        bigint ShareId PK
        bigint VaultItemId FK
        bigint OwnerUserId FK
        bigint SharedWithUserId FK
        varchar_10 Permission "View|Edit"
        datetime2 CreatedAt
        datetime2 RevokedAt NULL
    }
```

## 3. Table definitions

### 3.1 `Users`
Core identity and profile data. Kept lean (hot path for authentication) — security/preference flags live in `UserSecuritySettings`.

| Column | Type | Constraints |
|---|---|---|
| `UserId` | `BIGINT` | PK, IDENTITY |
| `PublicId` | `UNIQUEIDENTIFIER` | `UNIQUE`, default `NEWSEQUENTIALID()`, used in URLs/API payloads instead of `UserId` |
| `Email` | `VARCHAR(254)` | `UNIQUE NOT NULL`, stored lower-cased |
| `MasterPasswordHash` | `VARCHAR(255)` | `NOT NULL`, Argon2id hash (never the raw master password) |
| `FirstName` / `LastName` | `VARCHAR(50)` | `NOT NULL` |
| `Role` | `VARCHAR(20)` | `NOT NULL`, default `'Owner'` |
| `ProfilePhotoUrl` | `VARCHAR(512)` | `NULL` |
| `CreatedAt` / `UpdatedAt` | `DATETIME2` | `NOT NULL`, default `SYSUTCDATETIME()` |
| `LastLoginAt` | `DATETIME2` | `NULL` |
| `IsActive` | `BIT` | `NOT NULL`, default `1` (soft-disable instead of hard delete) |
| `RowVersion` | `ROWVERSION` | optimistic concurrency token |

Indexes: unique index on `Email`; unique index on `PublicId`.

### 3.2 `UserSecuritySettings` (1:1 with `Users`)
| Column | Type | Constraints |
|---|---|---|
| `UserId` | `BIGINT` | PK, FK → `Users.UserId` (`ON DELETE CASCADE`) |
| `TwoFactorEnabled` | `BIT` | `NOT NULL`, default `0` |
| `TwoFactorSecretEncrypted` | `VARBINARY(256)` | `NULL`, encrypted TOTP seed |
| `AutoLockEnabled` | `BIT` | `NOT NULL`, default `1` |
| `AutoLockMinutes` | `SMALLINT` | `NOT NULL`, default `15`, check `BETWEEN 1 AND 120` |
| `Theme` | `VARCHAR(20)` | `NOT NULL`, default `'Light'`, check in `('Light','Dark','System')` |
| `SecurityRemindersEnabled` | `BIT` | `NOT NULL`, default `1` |
| `UpdatedAt` | `DATETIME2` | `NOT NULL` |

Rationale: splitting settings from `Users` avoids widening the row that is read on every authenticated request and avoids locking contention when preferences change.

### 3.3 `Categories`
| Column | Type | Constraints |
|---|---|---|
| `CategoryId` | `BIGINT` | PK, IDENTITY |
| `UserId` | `BIGINT` | FK → `Users.UserId` (`ON DELETE CASCADE`) |
| `Name` | `VARCHAR(32)` | `NOT NULL` |
| `Description` | `VARCHAR(90)` | `NULL` |
| `Icon` | `VARCHAR(8)` | `NOT NULL`, glyph token (e.g. `♡`, `▣`) |
| `Color` | `VARCHAR(16)` | `NOT NULL`, check in `('pink','blue','green','orange','purple')` |
| `CreatedAt` / `UpdatedAt` | `DATETIME2` | `NOT NULL` |

Indexes: unique composite index `(UserId, Name)` — prevents duplicate category names per user and serves the category list query.

### 3.4 `Tags`
| Column | Type | Constraints |
|---|---|---|
| `TagId` | `BIGINT` | PK, IDENTITY |
| `UserId` | `BIGINT` | FK → `Users.UserId` (`ON DELETE CASCADE`) |
| `Name` | `VARCHAR(32)` | `NOT NULL` |
| `CreatedAt` | `DATETIME2` | `NOT NULL` |

Indexes: unique composite index `(UserId, Name)`.

### 3.5 `VaultItems`
The core credential record (what the UI calls a "password"/"vault" entry).

| Column | Type | Constraints |
|---|---|---|
| `VaultItemId` | `BIGINT` | PK, IDENTITY |
| `PublicId` | `UNIQUEIDENTIFIER` | `UNIQUE`, default `NEWSEQUENTIALID()` |
| `UserId` | `BIGINT` | FK → `Users.UserId` (`ON DELETE CASCADE`) |
| `CategoryId` | `BIGINT` | FK → `Categories.CategoryId`, `NULL`, `ON DELETE SET NULL` |
| `ServiceName` | `VARCHAR(100)` | `NOT NULL` (e.g. "Netflix") |
| `SiteUrl` | `VARCHAR(2048)` | `NULL` |
| `Username` | `VARCHAR(254)` | `NOT NULL` |
| `EncryptedPassword` | `VARBINARY(MAX)` | `NOT NULL`, AES-256-GCM ciphertext |
| `EncryptionNonce` | `VARBINARY(16)` | `NOT NULL`, unique per encryption operation |
| `EncryptedNotes` | `VARBINARY(MAX)` | `NULL` |
| `IconInitial` | `VARCHAR(4)` | `NULL`, display-only |
| `ColorTone` | `VARCHAR(16)` | `NULL`, check in `('dark','blue','green','light')` |
| `PasswordStrengthScore` | `TINYINT` | `NOT NULL`, default `0`, check `BETWEEN 0 AND 4`, recomputed on write |
| `IsFavorite` | `BIT` | `NOT NULL`, default `0` |
| `LastUsedAt` | `DATETIME2` | `NULL` |
| `CreatedAt` / `UpdatedAt` | `DATETIME2` | `NOT NULL` |
| `RowVersion` | `ROWVERSION` | optimistic concurrency |

Indexes:
- `IX_VaultItems_UserId_CategoryId` on `(UserId, CategoryId)` — powers the "My Vault" filtered-by-category list.
- `IX_VaultItems_UserId_IsFavorite` on `(UserId, IsFavorite) INCLUDE (ServiceName, Username)` — powers the dashboard "favorites" widget without a key lookup.
- `IX_VaultItems_UserId_ServiceName` on `(UserId, ServiceName)` — supports search/sort; consider full-text index if fuzzy search is required.

### 3.6 `VaultItemTags` (many-to-many)
| Column | Type | Constraints |
|---|---|---|
| `VaultItemId` | `BIGINT` | PK, FK → `VaultItems.VaultItemId` (`ON DELETE CASCADE`) |
| `TagId` | `BIGINT` | PK, FK → `Tags.TagId` (`ON DELETE CASCADE`) |

Composite PK `(VaultItemId, TagId)` also serves as the lookup index in both join directions when paired with a secondary index on `TagId`.

### 3.7 `PasswordHistory`
Keeps prior credential values so the server can detect password reuse and support recovery/audit.

| Column | Type | Constraints |
|---|---|---|
| `PasswordHistoryId` | `BIGINT` | PK, IDENTITY |
| `VaultItemId` | `BIGINT` | FK → `VaultItems.VaultItemId` (`ON DELETE CASCADE`) |
| `EncryptedPassword` | `VARBINARY(MAX)` | `NOT NULL` |
| `EncryptionNonce` | `VARBINARY(16)` | `NOT NULL` |
| `ChangedAt` | `DATETIME2` | `NOT NULL` |
| `ChangedByUserId` | `BIGINT` | FK → `Users.UserId` |

Index: `(VaultItemId, ChangedAt DESC)` for "show recent revisions".

### 3.8 `AuditLogs`
Append-only, high-volume table backing [Audit.tsx](../src/ArcaneVault.Client/src/components/Audit.tsx).

| Column | Type | Constraints |
|---|---|---|
| `AuditLogId` | `BIGINT` | PK, IDENTITY (sequential — avoids GUID clustered-index fragmentation on a write-heavy table) |
| `UserId` | `BIGINT` | FK → `Users.UserId`, `NULL` when the actor is unknown (e.g. failed sign-in from an unrecognized source) |
| `VaultItemId` | `BIGINT` | FK → `VaultItems.VaultItemId`, `NULL` |
| `EventType` | `VARCHAR(50)` | `NOT NULL`, e.g. `PasswordUpdated`, `PasswordViewed`, `NewSignIn`, `FailedSignIn`, `VaultShared`, `RecoveryEmailChanged`, `CategoryCreated` (see [§4](#4-enumerations)) |
| `Detail` | `VARCHAR(200)` | `NULL` |
| `Severity` | `VARCHAR(10)` | `NOT NULL`, check in `('Success','Review','Critical')` |
| `IpAddress` | `VARCHAR(45)` | `NULL` (IPv4/IPv6) |
| `UserAgent` | `VARCHAR(256)` | `NULL` |
| `CreatedAt` | `DATETIME2` | `NOT NULL`, default `SYSUTCDATETIME()` |

Indexes: `(UserId, CreatedAt DESC)` for the per-user activity feed; `(Severity, CreatedAt DESC)` for the "needs review" dashboard count. Consider monthly partitioning / a retention job once row counts grow, since this table is append-only and never updated.

### 3.9 `UserSessions`
Backs "New sign-in" / "Failed sign-in" audit entries and enables remote sign-out.

| Column | Type | Constraints |
|---|---|---|
| `SessionId` | `BIGINT` | PK, IDENTITY |
| `UserId` | `BIGINT` | FK → `Users.UserId` (`ON DELETE CASCADE`) |
| `DeviceName` | `VARCHAR(100)` | `NULL` (e.g. "Chrome on Windows") |
| `IpAddress` | `VARCHAR(45)` | `NULL` |
| `UserAgent` | `VARCHAR(256)` | `NULL` |
| `CreatedAt` | `DATETIME2` | `NOT NULL` |
| `LastActiveAt` | `DATETIME2` | `NOT NULL` |
| `ExpiresAt` | `DATETIME2` | `NOT NULL` |
| `IsRevoked` | `BIT` | `NOT NULL`, default `0` |

Index: `(UserId, ExpiresAt)` to efficiently evict/filter expired sessions.

### 3.10 `VaultItemShares`
Backs the "Vault shared" audit event; supports sharing an individual credential with another Arcane Vault user.

| Column | Type | Constraints |
|---|---|---|
| `ShareId` | `BIGINT` | PK, IDENTITY |
| `VaultItemId` | `BIGINT` | FK → `VaultItems.VaultItemId` (`ON DELETE CASCADE`) |
| `OwnerUserId` | `BIGINT` | FK → `Users.UserId` |
| `SharedWithUserId` | `BIGINT` | FK → `Users.UserId` |
| `Permission` | `VARCHAR(10)` | `NOT NULL`, check in `('View','Edit')` |
| `CreatedAt` | `DATETIME2` | `NOT NULL` |
| `RevokedAt` | `DATETIME2` | `NULL` |

Indexes: unique `(VaultItemId, SharedWithUserId)` where `RevokedAt IS NULL` (filtered index) to prevent duplicate active shares; `(SharedWithUserId)` for "shared with me" queries.

## 4. Enumerations

| Enum | Values | Used by |
|---|---|---|
| `Role` | `Owner`, `Admin`, `Member` | `Users.Role` |
| `Theme` | `Light`, `Dark`, `System` | `UserSecuritySettings.Theme` |
| `ColorTone` | `dark`, `blue`, `green`, `light` | `VaultItems.ColorTone` |
| `CategoryColor` | `pink`, `blue`, `green`, `orange`, `purple` | `Categories.Color` |
| `AuditSeverity` | `Success`, `Review`, `Critical` | `AuditLogs.Severity` (matches [auditData.ts](../src/ArcaneVault.Client/src/utils/auditData.ts) `AuditSeverity`) |
| `AuditEventType` | `PasswordCreated`, `PasswordUpdated`, `PasswordViewed`, `PasswordDeleted`, `NewSignIn`, `FailedSignIn`, `VaultShared`, `VaultShareRevoked`, `RecoveryEmailChanged`, `CategoryCreated`, `CategoryDeleted` | `AuditLogs.EventType` |
| `SharePermission` | `View`, `Edit` | `VaultItemShares.Permission` |

Store enums as short `VARCHAR` with a `CHECK` constraint (portable, human-readable in logs/queries) rather than raw integers; pair with a C# `enum` + `[Column(TypeName = "varchar(20)")]`/value converter in the EF Core model for type safety in the API layer.

## 5. Relationship summary

- **User → Categories, Tags, VaultItems, AuditLogs, UserSessions, VaultItemShares (received):** one-to-many, all cascade-deleted with the user except `AuditLogs` (set `UserId` to `NULL` to preserve the audit trail after account deletion, since logs are compliance-relevant).
- **User → UserSecuritySettings:** one-to-one, cascade delete.
- **Category → VaultItems:** one-to-many, `ON DELETE SET NULL` (deleting a category must not delete the credentials in it).
- **VaultItem → Tags:** many-to-many via `VaultItemTags`.
- **VaultItem → PasswordHistory:** one-to-many, cascade delete.
- **VaultItem → VaultItemShares:** one-to-many, cascade delete.

## 6. Security & performance best practices

- **Zero plaintext vault secrets.** The server protects password and note values with ASP.NET Core Data Protection before storing them. The key ring in `DataProtection:KeyDirectory` must be backed up and protected from unauthorized access; losing the key ring makes stored vault secrets unrecoverable.
- **Password verification.** The current implementation stores a per-user random salt and PBKDF2-HMAC-SHA256 verifier (600,000 iterations). The master password itself is not stored.
- **User-scoped identifiers.** The migration uses `UNIQUEIDENTIFIER` primary keys exposed through API URLs. Every procedure also filters on the authenticated user's id to prevent IDOR; an opaque identifier does not replace authorization.
- **Authorization at the query boundary.** Every row in `Categories`, `Tags`, `VaultItems`, `AuditLogs`, `UserSessions` carries `UserId`; all reads/writes must filter by the authenticated user's id (never trust a client-supplied `UserId`) to prevent IDOR.
- **Least-privilege reads.** `Users` stays narrow (auth-critical columns only) with security/preference flags split into `UserSecuritySettings`, so the hot authentication path doesn't scan or lock unrelated columns.
- **Targeted composite indexes** on `(UserId, CategoryId)`, `(UserId, IsFavorite)`, `(UserId, CreatedAt DESC)` match the actual dashboard/list/audit queries from the UI instead of indexing every column, keeping write amplification low.
- **Filtered unique index** on active `VaultItemShares` avoids duplicate-share bugs without a full scan.
- **Optimistic concurrency** (`ROWVERSION`) on `Users` and `VaultItems` prevents lost updates when a credential is edited concurrently (e.g. two tabs), without taking pessimistic locks.
- **Append-only audit table** is never updated, only inserted/read — safe for high write throughput; partition or archive by month once volume grows instead of adding update-triggered indexes.
- **Soft delete over hard delete** for `Users.IsActive` preserves referential integrity for audit/history tables; hard-delete cascades are reserved for genuinely owned child data (categories, tags, vault items, sessions).
- **Input bounds mirror the UI** (`VARCHAR(32)` category name, `VARCHAR(90)` description, `VARCHAR(100)` service name, etc., matching the `maxLength` attributes in [CreateCategory.tsx](../src/ArcaneVault.Client/src/components/CreateCategory.tsx)) to reject oversized payloads at the database layer, not just in the client.
- **Rate-limit and log authentication failures** via `AuditLogs` (`FailedSignIn`) and `UserSessions`, enabling account lockout/anomaly detection without storing more than `IpAddress`/`UserAgent` metadata.
