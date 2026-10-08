# Arcane Vault — API Reference

REST API implemented by [src/ArcaneVault.Server](../src/ArcaneVault.Server).
The client in [src/ArcaneVault.Client](../src/ArcaneVault.Client) is currently
a UI prototype that uses local mock authentication and does not call this API.
Resources map to the entities defined in [erd.md](erd.md).

> **Persistence:** the API uses SQL Server stored procedures. Apply [001_Initial.sql](../src/ArcaneVault.Server/Database/Migrations/001_Initial.sql) once before starting the server and set `ConnectionStrings__ArcaneVault` for your SQL Server instance. New users register through the API; there is no seeded demo account. Vault secrets are protected with ASP.NET Core Data Protection before being stored. Persist and restrict access to the configured Data Protection key directory (`DataProtection:KeyDirectory`) or existing vault secrets will not be decryptable after key loss. Scalar UI and OpenAPI are available in Development.

## 1. Conventions

- **Base URL:** `/api/v1`
- **Two-factor authentication:** `POST /auth/2fa/verify` is also unauthenticated because it completes sign-in before an access token has been issued.
- **Public authentication operations:** account registration, sign-in, token refresh, and second-factor verification are the only unauthenticated API operations.
- **Format:** `application/json; charset=utf-8` for all request/response bodies, except profile-photo uploads (`multipart/form-data`).
- **Auth:** Bearer JWT access token in `Authorization: Bearer <token>`, obtained from `POST /auth/login`. All endpoints require auth except `POST /auth/register`, `POST /auth/login`, `POST /auth/refresh`.
- **Resource ids:** resource identifiers are SQL Server `uniqueidentifier` primary keys and are scoped to the authenticated user.
- **Timestamps:** ISO 8601 UTC, e.g. `2026-09-30T14:22:05Z`.
- **Ownership:** every resource is implicitly scoped to the authenticated user (`UserId` from the access token); requests for another user's data return `404 Not Found` (not `403`, to avoid confirming existence).
- **Pagination:** list endpoints accept `page` (default `1`) and `pageSize` (default `20`, max `100`), and return the envelope below.
- **Optimistic concurrency:** `PATCH`/`PUT` on versioned resources (`VaultItems`, `Users`) require an `If-Match: "<RowVersion>"` header. The current version is returned in `ETag`; a stale version returns `409 Conflict`, and a missing/invalid header returns `428 Precondition Required`.

### 1.1 Paginated list envelope

```json
{
  "items": [ "..." ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 134,
  "totalPages": 7
}
```

### 1.2 Error envelope

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "One or more fields are invalid.",
    "details": [
      { "field": "serviceName", "issue": "must not exceed 100 characters" }
    ]
  }
}
```

| Status | Meaning |
|---|---|
| `400 Bad Request` | Malformed request (bad JSON, invalid query params). |
| `401 Unauthorized` | Missing/expired/invalid access token. |
| `403 Forbidden` | Authenticated but not permitted (e.g. acting on a share without `Edit` permission). |
| `404 Not Found` | Resource doesn't exist or isn't owned by the caller. |
| `409 Conflict` | Uniqueness violation or stale `If-Match` version. |
| `428 Precondition Required` | A required `If-Match` version header is missing or invalid. |
| `422 Unprocessable Entity` | Well-formed but semantically invalid (e.g. `autoLockMinutes` out of range). |
| `429 Too Many Requests` | Rate limit exceeded (login, password reveal, register). |

## 2. Auth

| Method | Path | Description |
|---|---|---|
| `POST` | `/auth/register` | Create a new account. |
| `POST` | `/auth/login` | Exchange email + master password for tokens; creates a `UserSessions` row. |
| `POST` | `/auth/refresh` | Exchange a refresh token for a new access token. |
| `POST` | `/auth/logout` | Revoke the current session's refresh token. |
| `POST` | `/auth/2fa/verify` | Complete sign-in when `twoFactorEnabled` is true. |
| `GET` | `/auth/sessions` | List active `UserSessions` (trusted devices). |
| `DELETE` | `/auth/sessions/{sessionId}` | Revoke a session remotely ("sign out of this device"). |

**`POST /auth/register`**
```json
// Request
{
  "email": "hello@designmonk.com",
  "masterPassword": "a-very-strong-master-password",
  "firstName": "Design",
  "lastName": "Monks"
}
```
```json
// 201 Created
{
  "id": "5a9e...-public-id",
  "email": "hello@designmonk.com",
  "firstName": "Design",
  "lastName": "Monks",
  "role": "Owner",
  "createdAt": "2026-09-30T14:22:05Z"
}
```
Rate-limited per IP; `409 Conflict` (`EMAIL_ALREADY_EXISTS`) if the email is taken. Master-password verifiers are stored with a per-user salt using PBKDF2-HMAC-SHA256.

**`POST /auth/login`**
```json
// Request
{ "email": "hello@designmonk.com", "masterPassword": "a-very-strong-master-password", "deviceName": "Chrome on Windows" }
```
```json
// 200 OK (2FA disabled)
{
  "accessToken": "eyJ...",
  "refreshToken": "8f3c...",
  "expiresIn": 900,
  "user": { "id": "5a9e...-public-id", "email": "hello@designmonk.com", "firstName": "Design", "lastName": "Monks", "role": "Owner" }
}
```
```json
// 202 Accepted (2FA enabled — pending `/auth/2fa/verify`)
{ "twoFactorRequired": true, "challengeId": "c1a0...-challenge-id" }
```
Every attempt (success or failure) writes an `AuditLogs` row (`NewSignIn` / `FailedSignIn`). Login is rate-limited per IP and failed logins return `401 Unauthorized` (`INVALID_CREDENTIALS`) without revealing whether the email exists.

Second-factor enrollment is not exposed by this API. Enabling `twoFactorEnabled` returns `422 TWO_FACTOR_ENROLLMENT_REQUIRED`; this implementation does not accept a demo verification code.

## 3. Profile & settings

| Method | Path | Description |
|---|---|---|
| `GET` | `/users/me` | Current user profile. |
| `PATCH` | `/users/me` | Update `firstName`/`lastName`/`email`. |
| `PUT` | `/users/me/photo` | Upload/replace profile photo (`multipart/form-data`, image/*, ≤ 5 MB). |
| `GET` | `/users/me/photo` | Get the stored profile photo. |
| `DELETE` | `/users/me/photo` | Remove profile photo (falls back to initials in the client). |
| `GET` | `/users/me/security-settings` | Read `UserSecuritySettings`. |
| `PATCH` | `/users/me/security-settings` | Update 2FA, auto-lock, theme, reminders. |

**`GET /users/me`**
```json
// 200 OK
{
  "id": "5a9e...-public-id",
  "email": "hello@designmonk.com",
  "firstName": "Design",
  "lastName": "Monks",
  "role": "Owner",
  "profilePhotoUrl": "/api/v1/users/me/photo",
  "createdAt": "2025-01-10T09:00:00Z",
  "lastLoginAt": "2026-09-30T14:22:05Z"
}
```

**`PATCH /users/me/security-settings`**
```json
// Request (all fields optional; twoFactorEnabled cannot be enabled until enrollment is available)
{ "autoLockEnabled": true, "autoLockMinutes": 15, "theme": "Dark", "securityRemindersEnabled": true }
```
```json
// 200 OK
{ "twoFactorEnabled": false, "autoLockEnabled": true, "autoLockMinutes": 15, "theme": "Dark", "securityRemindersEnabled": true, "updatedAt": "2026-09-30T14:25:00Z" }
```
`autoLockMinutes` must be `1`–`120` and `theme` one of `Light`/`Dark`/`System`; violations return `422 Unprocessable Entity`.

## 4. Categories

| Method | Path | Description |
|---|---|---|
| `GET` | `/categories` | List categories with live `passwordCount`. |
| `POST` | `/categories` | Create a category. |
| `GET` | `/categories/{id}` | Get one category. |
| `PATCH` | `/categories/{id}` | Rename / recolor / re-describe. |
| `DELETE` | `/categories/{id}` | Delete; vault items in it are set to uncategorized (`categoryId: null`), never deleted. |

**`POST /categories`**
```json
// Request
{ "name": "Subscriptions", "description": "What belongs in this category?", "icon": "♡", "color": "pink" }
```
```json
// 201 Created
{
  "id": "c2f1...-category-id",
  "name": "Subscriptions",
  "description": "What belongs in this category?",
  "icon": "♡",
  "color": "pink",
  "passwordCount": 0,
  "createdAt": "2026-09-30T14:30:00Z"
}
```
`name` ≤ 32 chars, `description` ≤ 90 chars, `color` ∈ `pink|blue|green|orange|purple`. Duplicate `name` for the same user returns `409 Conflict` (`CATEGORY_NAME_EXISTS`).

## 5. Tags

| Method | Path | Description |
|---|---|---|
| `GET` | `/tags` | List the user's tags. |
| `POST` | `/tags` | Create a tag (used inline by "Add tag +" in the client). |
| `DELETE` | `/tags/{id}` | Delete a tag and its `VaultItemTags` associations. |

```json
// POST /tags request
{ "name": "Design" }
// 201 Created
{ "id": "t771...-tag-id", "name": "Design", "createdAt": "2026-09-30T14:31:00Z" }
```

## 6. Vault items

| Method | Path | Description |
|---|---|---|
| `GET` | `/vault-items` | List/search/filter the vault. |
| `POST` | `/vault-items` | Create a credential. |
| `GET` | `/vault-items/{id}` | Get one item (password omitted). |
| `PATCH` | `/vault-items/{id}` | Update fields; changing `password` appends to `PasswordHistory`. |
| `DELETE` | `/vault-items/{id}` | Delete a credential. |
| `PATCH` | `/vault-items/{id}/favorite` | Toggle `isFavorite`. |
| `POST` | `/vault-items/{id}/reveal` | Decrypt and return the plaintext password for this item. |
| `GET` | `/vault-items/{id}/history` | List `PasswordHistory` revisions (ciphertext metadata only). |
| `POST` | `/vault-items/{id}/shares` | Share this item with another user. |
| `GET` | `/vault-items/{id}/shares` | List active shares. |
| `DELETE` | `/vault-items/{id}/shares/{shareId}` | Revoke a share. |

### 6.1 `GET /vault-items`

Query params: `category` (category id), `tag` (tag id), `favorite` (`true`/`false`), `search` (matches `serviceName`/`username`), `sort` (`updatedAt`\|`serviceName`\|`-updatedAt`, default `-updatedAt`), `page`, `pageSize`.

```json
// 200 OK
{
  "items": [
    {
      "id": "v100...-item-id",
      "serviceName": "Netflix",
      "siteUrl": "https://www.netflix.com",
      "username": "hello@designmonk.com",
      "category": { "id": "c2f1...-category-id", "name": "Personal", "icon": "♡", "color": "pink" },
      "tags": [ { "id": "t771...-tag-id", "name": "Design" } ],
      "iconInitial": "N",
      "colorTone": "dark",
      "passwordStrengthScore": 4,
      "isFavorite": true,
      "lastUsedAt": "2026-09-30T09:42:00Z",
      "updatedAt": "2026-09-30T09:42:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 36,
  "totalPages": 2
}
```
The plaintext password is **never** included in list/get responses — only `passwordStrengthScore` is returned, matching the client's list/dashboard views which never render raw secrets inline.

### 6.2 `POST /vault-items`
```json
// Request
{
  "serviceName": "Netflix",
  "siteUrl": "https://www.netflix.com",
  "username": "hello@designmonk.com",
  "password": "correct horse battery staple",
  "categoryId": "c2f1...-category-id",
  "tagIds": ["t771...-tag-id"],
  "notes": "Shared family plan"
}
```
```json
// 201 Created
{
  "id": "v100...-item-id",
  "serviceName": "Netflix",
  "siteUrl": "https://www.netflix.com",
  "username": "hello@designmonk.com",
  "category": { "id": "c2f1...-category-id", "name": "Personal", "icon": "♡", "color": "pink" },
  "tags": [ { "id": "t771...-tag-id", "name": "Design" } ],
  "passwordStrengthScore": 4,
  "isFavorite": false,
  "createdAt": "2026-09-30T14:35:00Z",
  "updatedAt": "2026-09-30T14:35:00Z"
}
```
`password` and `notes` travel over TLS and are protected with ASP.NET Core Data Protection before being persisted to `EncryptedPassword`/`EncryptedNotes`; `passwordStrengthScore` is computed server-side from the submitted password and cached. `serviceName` ≤ 100 chars, `username` ≤ 254 chars, `siteUrl` ≤ 2048 chars. Writes an `AuditLogs` row (`PasswordCreated`).

### 6.3 `POST /vault-items/{id}/reveal`
```json
// Request
{ "masterPassword": "a-very-strong-master-password" }
```
```json
// 200 OK
{ "password": "correct horse battery staple", "revealedAt": "2026-09-30T14:40:00Z" }
```
Requires step-up confirmation of the master password even though the session is already authenticated; rate-limited; always writes an `AuditLogs` row (`PasswordViewed`, `Severity: Success`) so every reveal is attributable.

### 6.4 `POST /vault-items/{id}/shares`
```json
// Request
{ "sharedWithEmail": "teammate@example.com", "permission": "View" }
```
```json
// 201 Created
{ "id": "s551...-share-id", "vaultItemId": "v100...-item-id", "sharedWith": { "id": "u900...-user-id", "email": "teammate@example.com" }, "permission": "View", "createdAt": "2026-09-30T14:45:00Z" }
```
`409 Conflict` (`SHARE_ALREADY_EXISTS`) if an active share for that user already exists (enforced by the filtered unique index on `VaultItemShares`). Writes an `AuditLogs` row (`VaultShared`).

## 7. Audit log

| Method | Path | Description |
|---|---|---|
| `GET` | `/audit-logs` | Paginated activity feed. |
| `GET` | `/audit-logs/summary` | Counts used by the Audit page's summary cards. |

Query params for `GET /audit-logs`: `severity` (`Success`\|`Review`\|`Critical`), `eventType`, `from`/`to` (ISO date range), `page`, `pageSize` (default sort `-createdAt`).

```json
// GET /audit-logs/summary — 200 OK
{ "eventsThisMonth": 128, "successfulEvents": 121, "needsReview": 7, "criticalEvents": 2 }
```
```json
// GET /audit-logs — 200 OK (excerpt)
{
  "items": [
    { "id": "a330...-log-id", "eventType": "PasswordUpdated", "detail": "Netflix", "severity": "Success", "createdAt": "2026-09-30T09:42:00Z" },
    { "id": "a331...-log-id", "eventType": "FailedSignIn", "detail": "Unknown device", "severity": "Critical", "createdAt": "2026-09-27T03:16:00Z" }
  ],
  "page": 1, "pageSize": 20, "totalCount": 128, "totalPages": 7
}
```
Audit logs are read-only via the API (no `PATCH`/`DELETE`) — they are an append-only trail per [erd.md §6](erd.md#6-security--performance-best-practices).

## 8. Dashboard

| Method | Path | Description |
|---|---|---|
| `GET` | `/dashboard/summary` | Aggregated stats + recent passwords for the Dashboard page. |

```json
// 200 OK
{
  "stats": [ { "label": "Total passwords", "value": 36 }, { "label": "Strong passwords", "value": 12 }, { "label": "Need attention", "value": 3 } ],
  "recentPasswords": [
    { "id": "v100...-item-id", "serviceName": "Netflix", "username": "hello@designmonk.com", "isFavorite": true, "colorTone": "dark", "updatedAt": "2026-09-30T09:42:00Z" }
  ],
  "categories": [ { "id": "c2f1...-category-id", "name": "Browser", "icon": "◎", "passwordCount": 18 } ]
}
```
This is a read-only, server-computed aggregate — it is not a CRUD resource and has no corresponding table of its own (it joins `VaultItems`/`Categories`).

## 9. Password generator

The generator ([PasswordGenerator.tsx](../src/ArcaneVault.Client/src/components/PasswordGenerator.tsx)) runs entirely client-side using `crypto.getRandomValues` and has **no API endpoint** — generated passwords must never be transmitted or logged before the user chooses to save them via `POST /vault-items`, minimizing exposure of freshly generated secrets.
