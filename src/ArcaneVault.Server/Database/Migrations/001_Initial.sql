SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

CREATE TABLE dbo.Users
(
    UserId uniqueidentifier NOT NULL CONSTRAINT PK_Users PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Email varchar(254) NOT NULL,
    PasswordSalt varchar(64) NOT NULL,
    PasswordHash varchar(64) NOT NULL,
    FirstName varchar(50) NOT NULL,
    LastName varchar(50) NOT NULL,
    Role varchar(20) NOT NULL CONSTRAINT DF_Users_Role DEFAULT 'Owner',
    ProfilePhotoUrl varchar(512) NULL,
    ProfilePhotoContentType varchar(32) NULL,
    ProfilePhotoData varchar(max) NULL,
    CreatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    LastLoginAt datetimeoffset(7) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    RowVersion rowversion NOT NULL,
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);
GO

CREATE TABLE dbo.UserSecuritySettings
(
    UserId uniqueidentifier NOT NULL CONSTRAINT PK_UserSecuritySettings PRIMARY KEY,
    TwoFactorEnabled bit NOT NULL CONSTRAINT DF_UserSecurity_TwoFactor DEFAULT 0,
    TwoFactorSecretEncrypted varchar(max) NULL,
    AutoLockEnabled bit NOT NULL CONSTRAINT DF_UserSecurity_AutoLock DEFAULT 1,
    AutoLockMinutes smallint NOT NULL CONSTRAINT DF_UserSecurity_AutoLockMinutes DEFAULT 15,
    Theme varchar(20) NOT NULL CONSTRAINT DF_UserSecurity_Theme DEFAULT 'Light',
    SecurityRemindersEnabled bit NOT NULL CONSTRAINT DF_UserSecurity_Reminders DEFAULT 1,
    UpdatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_UserSecurity_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_UserSecuritySettings_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE,
    CONSTRAINT CK_UserSecuritySettings_AutoLockMinutes CHECK (AutoLockMinutes BETWEEN 1 AND 120),
    CONSTRAINT CK_UserSecuritySettings_Theme CHECK (Theme IN ('Light', 'Dark', 'System'))
);
GO

CREATE TABLE dbo.Categories
(
    CategoryId uniqueidentifier NOT NULL CONSTRAINT PK_Categories PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    UserId uniqueidentifier NOT NULL,
    Name varchar(32) NOT NULL,
    NormalizedName AS LOWER(Name) PERSISTED,
    Description varchar(90) NULL,
    Icon nvarchar(8) NOT NULL,
    Color varchar(16) NOT NULL,
    CreatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_Categories_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Categories_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT UQ_Categories_User_Name UNIQUE (UserId, NormalizedName),
    CONSTRAINT CK_Categories_Color CHECK (Color IN ('pink', 'blue', 'green', 'orange', 'purple'))
);
GO
CREATE INDEX IX_Categories_UserId ON dbo.Categories(UserId, Name);
GO

CREATE TABLE dbo.Tags
(
    TagId uniqueidentifier NOT NULL CONSTRAINT PK_Tags PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    UserId uniqueidentifier NOT NULL,
    Name varchar(32) NOT NULL,
    NormalizedName AS LOWER(Name) PERSISTED,
    CreatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_Tags_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Tags_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT UQ_Tags_User_Name UNIQUE (UserId, NormalizedName)
);
GO

CREATE TABLE dbo.VaultItems
(
    VaultItemId uniqueidentifier NOT NULL CONSTRAINT PK_VaultItems PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    UserId uniqueidentifier NOT NULL,
    CategoryId uniqueidentifier NULL,
    ServiceName varchar(100) NOT NULL,
    SiteUrl varchar(2048) NULL,
    Username varchar(254) NOT NULL,
    EncryptedPassword varchar(max) NOT NULL,
    EncryptedNotes varchar(max) NULL,
    IconInitial nvarchar(4) NULL,
    ColorTone varchar(16) NOT NULL CONSTRAINT DF_VaultItems_ColorTone DEFAULT 'dark',
    PasswordStrengthScore tinyint NOT NULL,
    IsFavorite bit NOT NULL CONSTRAINT DF_VaultItems_IsFavorite DEFAULT 0,
    LastUsedAt datetimeoffset(7) NULL,
    CreatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_VaultItems_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_VaultItems_UpdatedAt DEFAULT SYSUTCDATETIME(),
    RowVersion rowversion NOT NULL,
    CONSTRAINT FK_VaultItems_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_VaultItems_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(CategoryId) ON DELETE SET NULL,
    CONSTRAINT CK_VaultItems_Strength CHECK (PasswordStrengthScore BETWEEN 0 AND 4),
    CONSTRAINT CK_VaultItems_ColorTone CHECK (ColorTone IN ('dark', 'blue', 'green', 'light'))
);
GO
CREATE INDEX IX_VaultItems_User_Category ON dbo.VaultItems(UserId, CategoryId, UpdatedAt DESC);
CREATE INDEX IX_VaultItems_User_Favorite ON dbo.VaultItems(UserId, IsFavorite);
CREATE INDEX IX_VaultItems_User_ServiceName ON dbo.VaultItems(UserId, ServiceName);
GO

CREATE TABLE dbo.VaultItemTags
(
    VaultItemId uniqueidentifier NOT NULL,
    TagId uniqueidentifier NOT NULL,
    CONSTRAINT PK_VaultItemTags PRIMARY KEY (VaultItemId, TagId),
    CONSTRAINT FK_VaultItemTags_VaultItems FOREIGN KEY (VaultItemId) REFERENCES dbo.VaultItems(VaultItemId) ON DELETE CASCADE,
    CONSTRAINT FK_VaultItemTags_Tags FOREIGN KEY (TagId) REFERENCES dbo.Tags(TagId) ON DELETE CASCADE
);
GO
CREATE INDEX IX_VaultItemTags_TagId ON dbo.VaultItemTags(TagId, VaultItemId);
GO

CREATE TABLE dbo.PasswordHistory
(
    PasswordHistoryId uniqueidentifier NOT NULL CONSTRAINT PK_PasswordHistory PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    VaultItemId uniqueidentifier NOT NULL,
    EncryptedPassword varchar(max) NOT NULL,
    PasswordStrengthScore tinyint NOT NULL,
    ChangedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_PasswordHistory_ChangedAt DEFAULT SYSUTCDATETIME(),
    ChangedByUserId uniqueidentifier NOT NULL,
    CONSTRAINT FK_PasswordHistory_VaultItems FOREIGN KEY (VaultItemId) REFERENCES dbo.VaultItems(VaultItemId) ON DELETE CASCADE,
    CONSTRAINT FK_PasswordHistory_Users FOREIGN KEY (ChangedByUserId) REFERENCES dbo.Users(UserId)
);
GO
CREATE INDEX IX_PasswordHistory_VaultItem_ChangedAt ON dbo.PasswordHistory(VaultItemId, ChangedAt DESC);
GO

CREATE TABLE dbo.UserSessions
(
    SessionId uniqueidentifier NOT NULL CONSTRAINT PK_UserSessions PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    UserId uniqueidentifier NOT NULL,
    DeviceName varchar(100) NULL,
    AccessTokenHash varchar(64) NOT NULL,
    RefreshTokenHash varchar(64) NOT NULL,
    CreatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_UserSessions_CreatedAt DEFAULT SYSUTCDATETIME(),
    LastActiveAt datetimeoffset(7) NOT NULL CONSTRAINT DF_UserSessions_LastActiveAt DEFAULT SYSUTCDATETIME(),
    ExpiresAt datetimeoffset(7) NOT NULL,
    AccessExpiresAt datetimeoffset(7) NOT NULL,
    IsRevoked bit NOT NULL CONSTRAINT DF_UserSessions_IsRevoked DEFAULT 0,
    CONSTRAINT FK_UserSessions_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT UQ_UserSessions_AccessTokenHash UNIQUE (AccessTokenHash),
    CONSTRAINT UQ_UserSessions_RefreshTokenHash UNIQUE (RefreshTokenHash)
);
GO
CREATE INDEX IX_UserSessions_User_Expiry ON dbo.UserSessions(UserId, ExpiresAt);
GO

CREATE TABLE dbo.AuditLogs
(
    AuditLogId uniqueidentifier NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    UserId uniqueidentifier NULL,
    VaultItemId uniqueidentifier NULL,
    EventType varchar(50) NOT NULL,
    Detail varchar(200) NOT NULL,
    Severity varchar(10) NOT NULL,
    CreatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_AuditLogs_VaultItems FOREIGN KEY (VaultItemId) REFERENCES dbo.VaultItems(VaultItemId) ON DELETE SET NULL,
    CONSTRAINT CK_AuditLogs_Severity CHECK (Severity IN ('Success', 'Review', 'Critical'))
);
GO
CREATE INDEX IX_AuditLogs_User_CreatedAt ON dbo.AuditLogs(UserId, CreatedAt DESC);
GO

CREATE TABLE dbo.VaultItemShares
(
    ShareId uniqueidentifier NOT NULL CONSTRAINT PK_VaultItemShares PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    VaultItemId uniqueidentifier NOT NULL,
    OwnerUserId uniqueidentifier NOT NULL,
    SharedWithUserId uniqueidentifier NOT NULL,
    Permission varchar(10) NOT NULL,
    CreatedAt datetimeoffset(7) NOT NULL CONSTRAINT DF_VaultItemShares_CreatedAt DEFAULT SYSUTCDATETIME(),
    RevokedAt datetimeoffset(7) NULL,
    CONSTRAINT FK_VaultItemShares_VaultItems FOREIGN KEY (VaultItemId) REFERENCES dbo.VaultItems(VaultItemId) ON DELETE CASCADE,
    CONSTRAINT FK_VaultItemShares_Owner FOREIGN KEY (OwnerUserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_VaultItemShares_Recipient FOREIGN KEY (SharedWithUserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_VaultItemShares_Permission CHECK (Permission IN ('View', 'Edit'))
);
GO
CREATE UNIQUE INDEX UX_VaultItemShares_Active ON dbo.VaultItemShares(VaultItemId, SharedWithUserId) WHERE RevokedAt IS NULL;
GO

CREATE OR ALTER PROCEDURE dbo.Auth_Execute
    @Action varchar(40),
    @Payload nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @UserId uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@Payload, '$.userId'));
    DECLARE @SessionId uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@Payload, '$.sessionId'));

    IF @Action = 'Register'
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.Users WHERE Email = JSON_VALUE(@Payload, '$.email'))
            THROW 50016, 'An account with this email already exists.', 1;
        DECLARE @NewUserId uniqueidentifier = NEWID();
        BEGIN TRANSACTION;
        INSERT dbo.Users (UserId, Email, PasswordSalt, PasswordHash, FirstName, LastName)
        VALUES (@NewUserId, JSON_VALUE(@Payload, '$.email'), JSON_VALUE(@Payload, '$.passwordSalt'),
                JSON_VALUE(@Payload, '$.passwordHash'), LTRIM(RTRIM(JSON_VALUE(@Payload, '$.firstName'))),
                LTRIM(RTRIM(JSON_VALUE(@Payload, '$.lastName'))));
        INSERT dbo.UserSecuritySettings (UserId) VALUES (@NewUserId);
        COMMIT TRANSACTION;
        SELECT (SELECT UserId AS id, Email AS email, FirstName AS firstName, LastName AS lastName, Role AS role,
                       ProfilePhotoUrl AS profilePhotoUrl, CreatedAt AS createdAt, LastLoginAt AS lastLoginAt,
                       CONVERT(bigint, RowVersion) AS rowVersion
                FROM dbo.Users WHERE UserId = @NewUserId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;

    IF @Action = 'FindForLogin'
    BEGIN
        SELECT (SELECT TOP (1) u.UserId AS id, u.Email AS email, u.FirstName AS firstName, u.LastName AS lastName,
                       u.Role AS role, u.PasswordSalt AS passwordSalt, u.PasswordHash AS passwordHash,
                       s.TwoFactorEnabled AS twoFactorEnabled
                FROM dbo.Users u JOIN dbo.UserSecuritySettings s ON s.UserId = u.UserId
                WHERE u.Email = JSON_VALUE(@Payload, '$.email') AND u.IsActive = 1
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;

    IF @Action = 'GetPasswordVerifier'
    BEGIN
        SELECT (SELECT UserId AS id, PasswordSalt AS passwordSalt, PasswordHash AS passwordHash
                FROM dbo.Users WHERE UserId = @UserId AND IsActive = 1 FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;

    IF @Action = 'RecordFailedLogin'
    BEGIN
        DECLARE @FailedUserId uniqueidentifier = (SELECT UserId FROM dbo.Users WHERE Email = JSON_VALUE(@Payload, '$.email'));
        INSERT dbo.AuditLogs (UserId, EventType, Detail, Severity)
        VALUES (@FailedUserId, 'FailedSignIn', 'Failed sign-in attempt', 'Critical');
        SELECT N'{}';
        RETURN;
    END;

    IF @Action = 'VerifyTwoFactor'
    BEGIN
        THROW 50002, 'Invalid or unconfigured two-factor challenge.', 1;
    END;

    IF @Action = 'CreateSession'
    BEGIN
        DECLARE @NewSessionId uniqueidentifier = NEWID();
        BEGIN TRANSACTION;
        INSERT dbo.UserSessions (SessionId, UserId, DeviceName, AccessTokenHash, RefreshTokenHash, ExpiresAt, AccessExpiresAt)
        VALUES (@NewSessionId, @UserId, JSON_VALUE(@Payload, '$.deviceName'),
                JSON_VALUE(@Payload, '$.accessTokenHash'), JSON_VALUE(@Payload, '$.refreshTokenHash'),
                DATEADD(day, 30, SYSUTCDATETIME()), DATEADD(second, 900, SYSUTCDATETIME()));
        UPDATE dbo.Users SET LastLoginAt = SYSUTCDATETIME() WHERE UserId = @UserId;
        INSERT dbo.AuditLogs (UserId, EventType, Detail, Severity)
        VALUES (@UserId, 'NewSignIn', COALESCE(JSON_VALUE(@Payload, '$.deviceName'), 'Unknown device'), 'Review');
        COMMIT TRANSACTION;
        SELECT N'{}';
        RETURN;
    END;

    IF @Action = 'RotateRefreshToken'
    BEGIN
        DECLARE @RefreshHash varchar(64) = JSON_VALUE(@Payload, '$.refreshTokenHash');
        DECLARE @SessionUserId uniqueidentifier;
        BEGIN TRANSACTION;
        SELECT @SessionUserId = UserId, @SessionId = SessionId
        FROM dbo.UserSessions WITH (UPDLOCK, ROWLOCK)
        WHERE RefreshTokenHash = @RefreshHash AND IsRevoked = 0 AND ExpiresAt > SYSUTCDATETIME();
        IF @SessionId IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            SELECT N'null';
            RETURN;
        END;
        UPDATE dbo.UserSessions
        SET RefreshTokenHash = JSON_VALUE(@Payload, '$.newRefreshTokenHash'),
            AccessTokenHash = JSON_VALUE(@Payload, '$.accessTokenHash'),
            LastActiveAt = SYSUTCDATETIME(), AccessExpiresAt = DATEADD(second, 900, SYSUTCDATETIME())
        WHERE SessionId = @SessionId;
        COMMIT TRANSACTION;
        SELECT (SELECT u.UserId AS userId, u.Email AS email, u.FirstName AS firstName, u.LastName AS lastName, u.Role AS role
                FROM dbo.Users u WHERE u.UserId = @SessionUserId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;

    IF @Action = 'ValidateAccessToken'
    BEGIN
        SELECT (SELECT s.UserId AS userId, s.SessionId AS sessionId
                FROM dbo.UserSessions s JOIN dbo.Users u ON u.UserId = s.UserId
                WHERE s.AccessTokenHash = JSON_VALUE(@Payload, '$.accessTokenHash')
                  AND s.IsRevoked = 0 AND s.AccessExpiresAt > SYSUTCDATETIME() AND s.ExpiresAt > SYSUTCDATETIME()
                  AND u.IsActive = 1
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;

    IF @Action = 'GetSessions'
    BEGIN
        SELECT (SELECT s.SessionId AS id, s.DeviceName AS deviceName, s.CreatedAt AS createdAt,
                       s.LastActiveAt AS lastActiveAt, s.ExpiresAt AS expiresAt
                FROM dbo.UserSessions s
                WHERE s.UserId = @UserId AND s.IsRevoked = 0 AND s.ExpiresAt > SYSUTCDATETIME()
                ORDER BY s.LastActiveAt DESC FOR JSON PATH);
        RETURN;
    END;

    IF @Action = 'RevokeSession'
    BEGIN
        UPDATE dbo.UserSessions SET IsRevoked = 1
        WHERE SessionId = @SessionId AND UserId = @UserId AND IsRevoked = 0;
        IF @@ROWCOUNT = 0 THROW 50003, 'Session not found.', 1;
        SELECT N'{}';
        RETURN;
    END;

    THROW 50000, 'Unknown authentication operation.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.Users_Execute
    @Action varchar(40),
    @Payload nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @UserId uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@Payload, '$.userId'));
    IF @Action = 'Get'
    BEGIN
        SELECT (SELECT UserId AS id, Email AS email, FirstName AS firstName, LastName AS lastName, Role AS role,
                       ProfilePhotoUrl AS profilePhotoUrl, CreatedAt AS createdAt, LastLoginAt AS lastLoginAt,
                       CONVERT(bigint, RowVersion) AS rowVersion
                FROM dbo.Users WHERE UserId = @UserId AND IsActive = 1 FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;
    IF @Action = 'Update'
    BEGIN
        IF JSON_VALUE(@Payload, '$.email') IS NOT NULL
           AND EXISTS (SELECT 1 FROM dbo.Users WHERE Email = LOWER(LTRIM(RTRIM(JSON_VALUE(@Payload, '$.email')))) AND UserId <> @UserId)
            THROW 50016, 'An account with this email already exists.', 1;
        UPDATE dbo.Users
        SET Email = CASE WHEN JSON_VALUE(@Payload, '$.email') IS NULL THEN Email ELSE LOWER(LTRIM(RTRIM(JSON_VALUE(@Payload, '$.email')))) END,
            FirstName = CASE WHEN JSON_VALUE(@Payload, '$.firstName') IS NULL THEN FirstName ELSE LTRIM(RTRIM(JSON_VALUE(@Payload, '$.firstName'))) END,
            LastName = CASE WHEN JSON_VALUE(@Payload, '$.lastName') IS NULL THEN LastName ELSE LTRIM(RTRIM(JSON_VALUE(@Payload, '$.lastName'))) END
        WHERE UserId = @UserId AND CONVERT(bigint, RowVersion) = TRY_CONVERT(bigint, JSON_VALUE(@Payload, '$.expectedVersion'));
        IF @@ROWCOUNT = 0
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @UserId) THROW 50004, 'User not found.', 1;
            THROW 50005, 'The resource has changed.', 1;
        END;
        SELECT (SELECT UserId AS id, Email AS email, FirstName AS firstName, LastName AS lastName, Role AS role,
                       ProfilePhotoUrl AS profilePhotoUrl, CreatedAt AS createdAt, LastLoginAt AS lastLoginAt,
                       CONVERT(bigint, RowVersion) AS rowVersion FROM dbo.Users WHERE UserId = @UserId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;
    IF @Action = 'GetSecuritySettings'
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.UserSecuritySettings WHERE UserId = @UserId) THROW 50004, 'User not found.', 1;
        SELECT (SELECT TwoFactorEnabled AS twoFactorEnabled, AutoLockEnabled AS autoLockEnabled,
                       AutoLockMinutes AS autoLockMinutes, Theme AS theme, SecurityRemindersEnabled AS securityRemindersEnabled,
                       UpdatedAt AS updatedAt FROM dbo.UserSecuritySettings WHERE UserId = @UserId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;
    IF @Action = 'UpdateSecuritySettings'
    BEGIN
        IF JSON_VALUE(@Payload, '$.twoFactorEnabled') = 'true'
           AND EXISTS (SELECT 1 FROM dbo.UserSecuritySettings WHERE UserId = @UserId AND TwoFactorEnabled = 0)
            THROW 50015, 'Two-factor enrollment is not configured.', 1;
        IF TRY_CONVERT(int, JSON_VALUE(@Payload, '$.autoLockMinutes')) NOT BETWEEN 1 AND 120
           AND JSON_VALUE(@Payload, '$.autoLockMinutes') IS NOT NULL
            THROW 50018, 'Auto-lock minutes are out of range.', 1;
        IF JSON_VALUE(@Payload, '$.theme') IS NOT NULL
           AND JSON_VALUE(@Payload, '$.theme') COLLATE Latin1_General_100_BIN2 NOT IN ('Light', 'Dark', 'System')
            THROW 50019, 'Theme is invalid.', 1;
        UPDATE dbo.UserSecuritySettings
        SET TwoFactorEnabled = COALESCE(TRY_CONVERT(bit, JSON_VALUE(@Payload, '$.twoFactorEnabled')), TwoFactorEnabled),
            AutoLockEnabled = COALESCE(TRY_CONVERT(bit, JSON_VALUE(@Payload, '$.autoLockEnabled')), AutoLockEnabled),
            AutoLockMinutes = COALESCE(TRY_CONVERT(smallint, JSON_VALUE(@Payload, '$.autoLockMinutes')), AutoLockMinutes),
            Theme = COALESCE(JSON_VALUE(@Payload, '$.theme'), Theme),
            SecurityRemindersEnabled = COALESCE(TRY_CONVERT(bit, JSON_VALUE(@Payload, '$.securityRemindersEnabled')), SecurityRemindersEnabled),
            UpdatedAt = SYSUTCDATETIME()
        WHERE UserId = @UserId;
        IF @@ROWCOUNT = 0 THROW 50004, 'User not found.', 1;
        SELECT (SELECT TwoFactorEnabled AS twoFactorEnabled, AutoLockEnabled AS autoLockEnabled,
                       AutoLockMinutes AS autoLockMinutes, Theme AS theme, SecurityRemindersEnabled AS securityRemindersEnabled,
                       UpdatedAt AS updatedAt FROM dbo.UserSecuritySettings WHERE UserId = @UserId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;
    IF @Action = 'UpdateProfilePhoto'
    BEGIN
        UPDATE dbo.Users SET ProfilePhotoUrl = '/api/v1/users/me/photo',
            ProfilePhotoContentType = JSON_VALUE(@Payload, '$.contentType'),
            ProfilePhotoData = JSON_VALUE(@Payload, '$.content')
        WHERE UserId = @UserId AND CONVERT(bigint, RowVersion) = TRY_CONVERT(bigint, JSON_VALUE(@Payload, '$.expectedVersion'));
        IF @@ROWCOUNT = 0 THROW 50005, 'The resource has changed.', 1;
        SELECT (SELECT UserId AS id, Email AS email, FirstName AS firstName, LastName AS lastName, Role AS role,
                       ProfilePhotoUrl AS profilePhotoUrl, CreatedAt AS createdAt, LastLoginAt AS lastLoginAt,
                       CONVERT(bigint, RowVersion) AS rowVersion FROM dbo.Users WHERE UserId = @UserId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;
    IF @Action = 'GetProfilePhoto'
    BEGIN
        SELECT (SELECT ProfilePhotoData AS content, ProfilePhotoContentType AS contentType
                FROM dbo.Users WHERE UserId = @UserId AND ProfilePhotoData IS NOT NULL
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;
    IF @Action = 'DeleteProfilePhoto'
    BEGIN
        UPDATE dbo.Users SET ProfilePhotoUrl = NULL, ProfilePhotoContentType = NULL, ProfilePhotoData = NULL WHERE UserId = @UserId;
        IF @@ROWCOUNT = 0 THROW 50004, 'User not found.', 1;
        SELECT N'{}';
        RETURN;
    END;
    THROW 50000, 'Unknown user operation.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.Categories_Execute
    @Action varchar(40),
    @Payload nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @UserId uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@Payload, '$.userId'));
    DECLARE @Id uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@Payload, '$.id'));
    DECLARE @Page int = TRY_CONVERT(int, JSON_VALUE(@Payload, '$.page'));
    DECLARE @PageSize int = TRY_CONVERT(int, JSON_VALUE(@Payload, '$.pageSize'));
    IF @Action = 'List'
    BEGIN
        IF @Page IS NULL OR @Page < 1 OR @PageSize IS NULL OR @PageSize < 1 OR @PageSize > 100
            THROW 50006, 'Invalid pagination.', 1;
        DECLARE @Total int = (SELECT COUNT(*) FROM dbo.Categories WHERE UserId = @UserId);
        SELECT (SELECT JSON_QUERY((SELECT c.CategoryId AS id, c.Name AS name, c.Description AS description, c.Icon AS icon,
                                          c.Color AS color, (SELECT COUNT(*) FROM dbo.VaultItems v WHERE v.UserId = @UserId AND v.CategoryId = c.CategoryId) AS passwordCount,
                                          c.CreatedAt AS createdAt
                                   FROM dbo.Categories c WHERE c.UserId = @UserId ORDER BY c.Name
                                   OFFSET (CONVERT(bigint, @Page) - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY FOR JSON PATH)) AS items,
                       @Page AS page, @PageSize AS pageSize, @Total AS totalCount,
                       CONVERT(int, CEILING(@Total * 1.0 / @PageSize)) AS totalPages FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;
    IF @Action = 'Get'
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE CategoryId = @Id AND UserId = @UserId)
            THROW 50007, 'Category not found.', 1;
        SELECT (SELECT c.CategoryId AS id, c.Name AS name, c.Description AS description, c.Icon AS icon, c.Color AS color,
                       (SELECT COUNT(*) FROM dbo.VaultItems v WHERE v.CategoryId = c.CategoryId AND v.UserId = @UserId) AS passwordCount,
                       c.CreatedAt AS createdAt FROM dbo.Categories c WHERE c.CategoryId = @Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;
    IF @Action = 'Create'
    BEGIN
        IF JSON_VALUE(@Payload, '$.color') COLLATE Latin1_General_100_BIN2 NOT IN ('pink', 'blue', 'green', 'orange', 'purple')
            THROW 50017, 'Category color is invalid.', 1;
        IF EXISTS (SELECT 1 FROM dbo.Categories WHERE UserId = @UserId
                   AND LOWER(Name) = LOWER(LTRIM(RTRIM(JSON_VALUE(@Payload, '$.name')))))
            THROW 50020, 'A category with this name already exists.', 1;
        DECLARE @NewId uniqueidentifier = NEWID();
        INSERT dbo.Categories (CategoryId, UserId, Name, Description, Icon, Color)
        VALUES (@NewId, @UserId, LTRIM(RTRIM(JSON_VALUE(@Payload, '$.name'))), JSON_VALUE(@Payload, '$.description'),
                JSON_VALUE(@Payload, '$.icon'), JSON_VALUE(@Payload, '$.color'));
        SELECT (SELECT c.CategoryId AS id, c.Name AS name, c.Description AS description, c.Icon AS icon, c.Color AS color,
                       0 AS passwordCount, c.CreatedAt AS createdAt FROM dbo.Categories c WHERE c.CategoryId = @NewId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;
    IF @Action = 'Update'
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE CategoryId = @Id AND UserId = @UserId)
            THROW 50007, 'Category not found.', 1;
        IF JSON_VALUE(@Payload, '$.color') IS NOT NULL
           AND JSON_VALUE(@Payload, '$.color') COLLATE Latin1_General_100_BIN2 NOT IN ('pink', 'blue', 'green', 'orange', 'purple')
            THROW 50017, 'Category color is invalid.', 1;
        IF JSON_VALUE(@Payload, '$.name') IS NOT NULL
           AND EXISTS (SELECT 1 FROM dbo.Categories WHERE UserId = @UserId AND CategoryId <> @Id
                       AND LOWER(Name) = LOWER(LTRIM(RTRIM(JSON_VALUE(@Payload, '$.name')))))
            THROW 50020, 'A category with this name already exists.', 1;
        UPDATE dbo.Categories SET Name = CASE WHEN JSON_VALUE(@Payload, '$.name') IS NULL THEN Name ELSE LTRIM(RTRIM(JSON_VALUE(@Payload, '$.name'))) END,
            Description = CASE WHEN JSON_VALUE(@Payload, '$.description') IS NULL THEN Description ELSE LTRIM(RTRIM(JSON_VALUE(@Payload, '$.description'))) END,
            Icon = COALESCE(JSON_VALUE(@Payload, '$.icon'), Icon), Color = COALESCE(JSON_VALUE(@Payload, '$.color'), Color),
            UpdatedAt = SYSUTCDATETIME()
        WHERE CategoryId = @Id AND UserId = @UserId;
        SELECT (SELECT c.CategoryId AS id, c.Name AS name, c.Description AS description, c.Icon AS icon, c.Color AS color,
                       (SELECT COUNT(*) FROM dbo.VaultItems v WHERE v.CategoryId = c.CategoryId AND v.UserId = @UserId) AS passwordCount,
                       c.CreatedAt AS createdAt FROM dbo.Categories c WHERE c.CategoryId = @Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;
    IF @Action = 'Delete'
    BEGIN
        DELETE FROM dbo.Categories WHERE CategoryId = @Id AND UserId = @UserId;
        IF @@ROWCOUNT = 0 THROW 50007, 'Category not found.', 1;
        SELECT N'{}';
        RETURN;
    END;
    THROW 50000, 'Unknown category operation.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.Tags_Execute
    @Action varchar(40),
    @Payload nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @UserId uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@Payload, '$.userId'));
    DECLARE @Id uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@Payload, '$.id'));
    DECLARE @Page int = TRY_CONVERT(int, JSON_VALUE(@Payload, '$.page'));
    DECLARE @PageSize int = TRY_CONVERT(int, JSON_VALUE(@Payload, '$.pageSize'));
    IF @Action = 'List'
    BEGIN
        IF @Page IS NULL OR @Page < 1 OR @PageSize IS NULL OR @PageSize < 1 OR @PageSize > 100 THROW 50006, 'Invalid pagination.', 1;
        DECLARE @Total int = (SELECT COUNT(*) FROM dbo.Tags WHERE UserId = @UserId);
        SELECT (SELECT JSON_QUERY((SELECT TagId AS id, Name AS name, CreatedAt AS createdAt FROM dbo.Tags WHERE UserId = @UserId
                                   ORDER BY Name OFFSET (CONVERT(bigint, @Page) - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY FOR JSON PATH)) AS items,
                       @Page AS page, @PageSize AS pageSize, @Total AS totalCount, CONVERT(int, CEILING(@Total * 1.0 / @PageSize)) AS totalPages
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;
    IF @Action = 'Create'
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.Tags WHERE UserId = @UserId AND LOWER(Name) = LOWER(LTRIM(RTRIM(JSON_VALUE(@Payload, '$.name')))))
            THROW 50021, 'A tag with this name already exists.', 1;
        DECLARE @NewId uniqueidentifier = NEWID();
        INSERT dbo.Tags (TagId, UserId, Name) VALUES (@NewId, @UserId, LTRIM(RTRIM(JSON_VALUE(@Payload, '$.name'))));
        SELECT (SELECT TagId AS id, Name AS name, CreatedAt AS createdAt FROM dbo.Tags WHERE TagId = @NewId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;
    IF @Action = 'Delete'
    BEGIN
        DELETE FROM dbo.Tags WHERE TagId = @Id AND UserId = @UserId;
        IF @@ROWCOUNT = 0 THROW 50008, 'Tag not found.', 1;
        SELECT N'{}';
        RETURN;
    END;
    THROW 50000, 'Unknown tag operation.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.Audit_Execute
    @Action varchar(40),
    @Payload nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @UserId uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@Payload, '$.userId'));
    IF @Action = 'List'
    BEGIN
        DECLARE @Page int = TRY_CONVERT(int, JSON_VALUE(@Payload, '$.page'));
        DECLARE @PageSize int = TRY_CONVERT(int, JSON_VALUE(@Payload, '$.pageSize'));
        DECLARE @From datetimeoffset(7) = TRY_CONVERT(datetimeoffset(7), JSON_VALUE(@Payload, '$.from'));
        DECLARE @To datetimeoffset(7) = TRY_CONVERT(datetimeoffset(7), JSON_VALUE(@Payload, '$.to'));
        DECLARE @Severity varchar(10) = JSON_VALUE(@Payload, '$.severity');
        DECLARE @EventType varchar(50) = JSON_VALUE(@Payload, '$.eventType');
        IF @Page IS NULL OR @Page < 1 OR @PageSize IS NULL OR @PageSize < 1 OR @PageSize > 100 THROW 50006, 'Invalid pagination.', 1;
        IF @From > @To THROW 50009, 'Invalid date range.', 1;
        IF @Severity IS NOT NULL AND @Severity COLLATE Latin1_General_100_BIN2 NOT IN ('Success', 'Review', 'Critical')
            THROW 50010, 'Invalid audit severity.', 1;
        DECLARE @Total int = (SELECT COUNT(*) FROM dbo.AuditLogs WHERE UserId = @UserId
            AND (@Severity IS NULL OR Severity = @Severity) AND (@EventType IS NULL OR EventType = @EventType)
            AND (@From IS NULL OR CreatedAt >= @From) AND (@To IS NULL OR CreatedAt <= @To));
        SELECT (SELECT JSON_QUERY((SELECT AuditLogId AS id, EventType AS eventType, Detail AS detail, Severity AS severity, CreatedAt AS createdAt
                                   FROM dbo.AuditLogs WHERE UserId = @UserId
                                   AND (@Severity IS NULL OR Severity = @Severity) AND (@EventType IS NULL OR EventType = @EventType)
                                   AND (@From IS NULL OR CreatedAt >= @From) AND (@To IS NULL OR CreatedAt <= @To)
                                   ORDER BY CreatedAt DESC OFFSET (CONVERT(bigint, @Page) - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY FOR JSON PATH)) AS items,
                       @Page AS page, @PageSize AS pageSize, @Total AS totalCount, CONVERT(int, CEILING(@Total * 1.0 / @PageSize)) AS totalPages
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;
    IF @Action = 'GetSummary'
    BEGIN
        SELECT (SELECT COALESCE(SUM(CASE WHEN CreatedAt >= DATEFROMPARTS(YEAR(SYSUTCDATETIME()), MONTH(SYSUTCDATETIME()), 1) THEN 1 ELSE 0 END), 0) AS eventsThisMonth,
                       COALESCE(SUM(CASE WHEN Severity = 'Success' THEN 1 ELSE 0 END), 0) AS successfulEvents,
                       COALESCE(SUM(CASE WHEN Severity = 'Review' THEN 1 ELSE 0 END), 0) AS needsReview,
                       COALESCE(SUM(CASE WHEN Severity = 'Critical' THEN 1 ELSE 0 END), 0) AS criticalEvents
                FROM dbo.AuditLogs WHERE UserId = @UserId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;
    THROW 50000, 'Unknown audit operation.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.Dashboard_Execute
    @Action varchar(40),
    @Payload nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    IF @Action <> 'GetSummary' THROW 50000, 'Unknown dashboard operation.', 1;
    DECLARE @UserId uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@Payload, '$.userId'));
    DECLARE @Total int = (SELECT COUNT(*) FROM dbo.VaultItems WHERE UserId = @UserId);
    DECLARE @Strong int = (SELECT COUNT(*) FROM dbo.VaultItems WHERE UserId = @UserId AND PasswordStrengthScore >= 3);
    DECLARE @Weak int = (SELECT COUNT(*) FROM dbo.VaultItems WHERE UserId = @UserId AND PasswordStrengthScore < 2);
    SELECT (SELECT JSON_QUERY((SELECT Label AS label, Value AS value FROM (VALUES
                       ('Total passwords', @Total), ('Strong passwords', @Strong), ('Need attention', @Weak)) AS stats(Label, Value) FOR JSON PATH)) AS stats,
                   JSON_QUERY((SELECT TOP (5) v.VaultItemId AS id, v.ServiceName AS serviceName, v.Username AS username,
                                      v.IsFavorite AS isFavorite, v.ColorTone AS colorTone, v.UpdatedAt AS updatedAt
                               FROM dbo.VaultItems v WHERE v.UserId = @UserId ORDER BY v.UpdatedAt DESC FOR JSON PATH)) AS recentPasswords,
                   JSON_QUERY((SELECT c.CategoryId AS id, c.Name AS name, c.Icon AS icon,
                                      (SELECT COUNT(*) FROM dbo.VaultItems v WHERE v.CategoryId = c.CategoryId AND v.UserId = @UserId) AS passwordCount
                               FROM dbo.Categories c WHERE c.UserId = @UserId ORDER BY c.Name FOR JSON PATH)) AS categories
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
END;
GO

CREATE OR ALTER PROCEDURE dbo.VaultItems_Execute
    @Action varchar(40),
    @Payload nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @UserId uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@Payload, '$.userId'));
    DECLARE @Id uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@Payload, '$.id'));
    DECLARE @CategoryId uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@Payload, '$.categoryId'));
    DECLARE @Page int = TRY_CONVERT(int, JSON_VALUE(@Payload, '$.page'));
    DECLARE @PageSize int = TRY_CONVERT(int, JSON_VALUE(@Payload, '$.pageSize'));

    IF @Action = 'List'
    BEGIN
        IF @Page IS NULL OR @Page < 1 OR @PageSize IS NULL OR @PageSize < 1 OR @PageSize > 100 THROW 50006, 'Invalid pagination.', 1;
        DECLARE @TagId uniqueidentifier = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@Payload, '$.tagId'));
        DECLARE @Favorite bit = TRY_CONVERT(bit, JSON_VALUE(@Payload, '$.isFavorite'));
        DECLARE @Search nvarchar(254) = JSON_VALUE(@Payload, '$.search');
        DECLARE @Sort varchar(32) = JSON_VALUE(@Payload, '$.sort');
        IF @Sort IS NOT NULL AND @Sort NOT IN ('updatedAt', '-updatedAt', 'serviceName') THROW 50011, 'Invalid sort order.', 1;
        IF @CategoryId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE CategoryId = @CategoryId AND UserId = @UserId)
            THROW 50007, 'Category not found.', 1;
        IF @TagId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Tags WHERE TagId = @TagId AND UserId = @UserId)
            THROW 50008, 'Tag not found.', 1;
        DECLARE @Total int = (SELECT COUNT(*) FROM dbo.VaultItems v
            WHERE v.UserId = @UserId AND (@CategoryId IS NULL OR v.CategoryId = @CategoryId)
              AND (@Favorite IS NULL OR v.IsFavorite = @Favorite)
              AND (@TagId IS NULL OR EXISTS (SELECT 1 FROM dbo.VaultItemTags vt WHERE vt.VaultItemId = v.VaultItemId AND vt.TagId = @TagId))
              AND (@Search IS NULL OR v.ServiceName LIKE '%' + @Search + '%' OR v.Username LIKE '%' + @Search + '%'));
        SELECT (SELECT JSON_QUERY((SELECT v.VaultItemId AS id, v.ServiceName AS serviceName, v.SiteUrl AS siteUrl, v.Username AS username,
                                          JSON_QUERY((SELECT c.CategoryId AS id, c.Name AS name, c.Icon AS icon, c.Color AS color
                                                      FROM dbo.Categories c WHERE c.CategoryId = v.CategoryId
                                                      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)) AS category,
                                          JSON_QUERY((SELECT t.TagId AS id, t.Name AS name FROM dbo.VaultItemTags vt
                                                      JOIN dbo.Tags t ON t.TagId = vt.TagId WHERE vt.VaultItemId = v.VaultItemId
                                                      ORDER BY t.Name FOR JSON PATH)) AS tags,
                                          LEFT(v.ServiceName, 1) AS iconInitial, v.ColorTone AS colorTone,
                                          v.PasswordStrengthScore AS passwordStrengthScore, v.IsFavorite AS isFavorite,
                                          v.LastUsedAt AS lastUsedAt, v.CreatedAt AS createdAt, v.UpdatedAt AS updatedAt,
                                          CONVERT(bigint, v.RowVersion) AS rowVersion
                                   FROM dbo.VaultItems v WHERE v.UserId = @UserId AND (@CategoryId IS NULL OR v.CategoryId = @CategoryId)
                                     AND (@Favorite IS NULL OR v.IsFavorite = @Favorite)
                                     AND (@TagId IS NULL OR EXISTS (SELECT 1 FROM dbo.VaultItemTags vt WHERE vt.VaultItemId = v.VaultItemId AND vt.TagId = @TagId))
                                     AND (@Search IS NULL OR v.ServiceName LIKE '%' + @Search + '%' OR v.Username LIKE '%' + @Search + '%')
                                   ORDER BY CASE WHEN @Sort = 'updatedAt' THEN v.UpdatedAt END ASC,
                                            CASE WHEN @Sort IS NULL OR @Sort = '-updatedAt' THEN v.UpdatedAt END DESC,
                                            CASE WHEN @Sort = 'serviceName' THEN v.ServiceName END ASC
                                   OFFSET (CONVERT(bigint, @Page) - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY FOR JSON PATH)) AS items,
                       @Page AS page, @PageSize AS pageSize, @Total AS totalCount, CONVERT(int, CEILING(@Total * 1.0 / @PageSize)) AS totalPages
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;

    IF @Action = 'Get' OR @Action = 'Create' OR @Action = 'Update'
    BEGIN
        DECLARE @ItemId uniqueidentifier = @Id;
        IF @Action = 'Create'
        BEGIN
            SET @ItemId = NEWID();
            SET @CategoryId = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@Payload, '$.categoryId'));
            IF @CategoryId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE CategoryId = @CategoryId AND UserId = @UserId)
                THROW 50007, 'Category not found.', 1;
            IF EXISTS (SELECT 1 FROM OPENJSON(@Payload, '$.tagIds') j LEFT JOIN dbo.Tags t
                       ON t.TagId = TRY_CONVERT(uniqueidentifier, j.value) AND t.UserId = @UserId WHERE t.TagId IS NULL)
                THROW 50008, 'Tag not found.', 1;
            BEGIN TRANSACTION;
            INSERT dbo.VaultItems (VaultItemId, UserId, CategoryId, ServiceName, SiteUrl, Username, EncryptedPassword,
                                   EncryptedNotes, PasswordStrengthScore)
            VALUES (@ItemId, @UserId, @CategoryId, LTRIM(RTRIM(JSON_VALUE(@Payload, '$.serviceName'))),
                    JSON_VALUE(@Payload, '$.siteUrl'), LTRIM(RTRIM(JSON_VALUE(@Payload, '$.username'))),
                    JSON_VALUE(@Payload, '$.encryptedPassword'), JSON_VALUE(@Payload, '$.encryptedNotes'),
                    TRY_CONVERT(tinyint, JSON_VALUE(@Payload, '$.passwordStrengthScore')));
            INSERT dbo.VaultItemTags (VaultItemId, TagId)
                SELECT @ItemId, TRY_CONVERT(uniqueidentifier, value) FROM OPENJSON(@Payload, '$.tagIds');
            INSERT dbo.AuditLogs (UserId, VaultItemId, EventType, Detail, Severity)
                VALUES (@UserId, @ItemId, 'PasswordCreated', JSON_VALUE(@Payload, '$.serviceName'), 'Success');
            COMMIT TRANSACTION;
        END;
        IF @Action = 'Update'
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.VaultItems WHERE VaultItemId = @ItemId AND UserId = @UserId)
                THROW 50012, 'Vault item not found.', 1;
            IF NOT EXISTS (SELECT 1 FROM dbo.VaultItems WHERE VaultItemId = @ItemId AND UserId = @UserId
                           AND CONVERT(bigint, RowVersion) = TRY_CONVERT(bigint, JSON_VALUE(@Payload, '$.expectedVersion')))
                THROW 50005, 'The resource has changed.', 1;
            SET @CategoryId = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@Payload, '$.categoryId'));
            IF @CategoryId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE CategoryId = @CategoryId AND UserId = @UserId)
                THROW 50007, 'Category not found.', 1;
            IF JSON_QUERY(@Payload, '$.tagIds') IS NOT NULL AND EXISTS
                (SELECT 1 FROM OPENJSON(@Payload, '$.tagIds') j LEFT JOIN dbo.Tags t
                 ON t.TagId = TRY_CONVERT(uniqueidentifier, j.value) AND t.UserId = @UserId WHERE t.TagId IS NULL)
                THROW 50008, 'Tag not found.', 1;
            BEGIN TRANSACTION;
            IF JSON_VALUE(@Payload, '$.encryptedPassword') IS NOT NULL
                INSERT dbo.PasswordHistory (VaultItemId, EncryptedPassword, PasswordStrengthScore, ChangedByUserId)
                SELECT VaultItemId, EncryptedPassword, PasswordStrengthScore, @UserId FROM dbo.VaultItems WHERE VaultItemId = @ItemId;
            UPDATE dbo.VaultItems SET
                ServiceName = CASE WHEN JSON_VALUE(@Payload, '$.serviceName') IS NULL THEN ServiceName ELSE LTRIM(RTRIM(JSON_VALUE(@Payload, '$.serviceName'))) END,
                SiteUrl = CASE WHEN JSON_VALUE(@Payload, '$.siteUrl') IS NULL THEN SiteUrl ELSE JSON_VALUE(@Payload, '$.siteUrl') END,
                Username = CASE WHEN JSON_VALUE(@Payload, '$.username') IS NULL THEN Username ELSE LTRIM(RTRIM(JSON_VALUE(@Payload, '$.username'))) END,
                EncryptedPassword = COALESCE(JSON_VALUE(@Payload, '$.encryptedPassword'), EncryptedPassword),
                EncryptedNotes = COALESCE(JSON_VALUE(@Payload, '$.encryptedNotes'), EncryptedNotes),
                CategoryId = COALESCE(@CategoryId, CategoryId),
                PasswordStrengthScore = COALESCE(TRY_CONVERT(tinyint, JSON_VALUE(@Payload, '$.passwordStrengthScore')), PasswordStrengthScore),
                IsFavorite = COALESCE(TRY_CONVERT(bit, JSON_VALUE(@Payload, '$.isFavorite')), IsFavorite),
                UpdatedAt = SYSUTCDATETIME()
            WHERE VaultItemId = @ItemId AND UserId = @UserId;
            IF JSON_QUERY(@Payload, '$.tagIds') IS NOT NULL
            BEGIN
                DELETE FROM dbo.VaultItemTags WHERE VaultItemId = @ItemId;
                INSERT dbo.VaultItemTags (VaultItemId, TagId)
                    SELECT @ItemId, TRY_CONVERT(uniqueidentifier, value) FROM OPENJSON(@Payload, '$.tagIds');
            END;
            INSERT dbo.AuditLogs (UserId, VaultItemId, EventType, Detail, Severity)
                SELECT @UserId, @ItemId, 'PasswordUpdated', ServiceName, 'Success' FROM dbo.VaultItems WHERE VaultItemId = @ItemId;
            COMMIT TRANSACTION;
        END;
        IF @Action = 'Get' AND NOT EXISTS (SELECT 1 FROM dbo.VaultItems WHERE VaultItemId = @ItemId AND UserId = @UserId)
            THROW 50012, 'Vault item not found.', 1;
        SELECT (SELECT v.VaultItemId AS id, v.ServiceName AS serviceName, v.SiteUrl AS siteUrl, v.Username AS username,
                       v.EncryptedNotes AS encryptedNotes,
                       JSON_QUERY((SELECT c.CategoryId AS id, c.Name AS name, c.Icon AS icon, c.Color AS color
                                   FROM dbo.Categories c WHERE c.CategoryId = v.CategoryId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)) AS category,
                       JSON_QUERY((SELECT t.TagId AS id, t.Name AS name FROM dbo.VaultItemTags vt
                                   JOIN dbo.Tags t ON t.TagId = vt.TagId WHERE vt.VaultItemId = v.VaultItemId
                                   ORDER BY t.Name FOR JSON PATH)) AS tags,
                       LEFT(v.ServiceName, 1) AS iconInitial, v.ColorTone AS colorTone,
                       v.PasswordStrengthScore AS passwordStrengthScore, v.IsFavorite AS isFavorite,
                       v.LastUsedAt AS lastUsedAt, v.CreatedAt AS createdAt, v.UpdatedAt AS updatedAt,
                       CONVERT(bigint, v.RowVersion) AS rowVersion
                FROM dbo.VaultItems v WHERE v.VaultItemId = @ItemId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;

    IF @Action = 'Delete'
    BEGIN
        DELETE FROM dbo.VaultItems WHERE VaultItemId = @Id AND UserId = @UserId;
        IF @@ROWCOUNT = 0 THROW 50012, 'Vault item not found.', 1;
        SELECT N'{}';
        RETURN;
    END;
    IF @Action = 'ToggleFavorite'
    BEGIN
        UPDATE dbo.VaultItems SET IsFavorite = IIF(IsFavorite = 1, 0, 1), UpdatedAt = SYSUTCDATETIME()
        WHERE VaultItemId = @Id AND UserId = @UserId
          AND CONVERT(bigint, RowVersion) = TRY_CONVERT(bigint, JSON_VALUE(@Payload, '$.expectedVersion'));
        IF @@ROWCOUNT = 0
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.VaultItems WHERE VaultItemId = @Id AND UserId = @UserId) THROW 50012, 'Vault item not found.', 1;
            THROW 50005, 'The resource has changed.', 1;
        END;
        SELECT (SELECT v.VaultItemId AS id, v.ServiceName AS serviceName, v.SiteUrl AS siteUrl, v.Username AS username,
                       JSON_QUERY((SELECT c.CategoryId AS id, c.Name AS name, c.Icon AS icon, c.Color AS color FROM dbo.Categories c WHERE c.CategoryId = v.CategoryId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)) AS category,
                       JSON_QUERY((SELECT t.TagId AS id, t.Name AS name FROM dbo.VaultItemTags vt JOIN dbo.Tags t ON t.TagId = vt.TagId
                                   WHERE vt.VaultItemId = v.VaultItemId ORDER BY t.Name FOR JSON PATH)) AS tags,
                       LEFT(v.ServiceName, 1) AS iconInitial, v.ColorTone AS colorTone, v.PasswordStrengthScore AS passwordStrengthScore,
                       v.IsFavorite AS isFavorite, v.LastUsedAt AS lastUsedAt, v.CreatedAt AS createdAt, v.UpdatedAt AS updatedAt,
                       CONVERT(bigint, v.RowVersion) AS rowVersion FROM dbo.VaultItems v WHERE v.VaultItemId = @Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;
    IF @Action = 'Reveal'
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.VaultItems WHERE VaultItemId = @Id AND UserId = @UserId) THROW 50012, 'Vault item not found.', 1;
        UPDATE dbo.VaultItems SET LastUsedAt = SYSUTCDATETIME() WHERE VaultItemId = @Id AND UserId = @UserId;
        INSERT dbo.AuditLogs (UserId, VaultItemId, EventType, Detail, Severity)
            SELECT @UserId, @Id, 'PasswordViewed', ServiceName, 'Success' FROM dbo.VaultItems WHERE VaultItemId = @Id;
        SELECT (SELECT EncryptedPassword AS encryptedPassword, SYSUTCDATETIME() AS revealedAt
                FROM dbo.VaultItems WHERE VaultItemId = @Id FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;
    IF @Action = 'GetPasswordHistory'
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.VaultItems WHERE VaultItemId = @Id AND UserId = @UserId) THROW 50012, 'Vault item not found.', 1;
        SELECT (SELECT PasswordHistoryId AS id, PasswordStrengthScore AS passwordStrengthScore, ChangedAt AS changedAt
                FROM dbo.PasswordHistory WHERE VaultItemId = @Id ORDER BY ChangedAt DESC FOR JSON PATH);
        RETURN;
    END;
    IF @Action = 'CreateShare'
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.VaultItems WHERE VaultItemId = @Id AND UserId = @UserId) THROW 50012, 'Vault item not found.', 1;
        DECLARE @RecipientId uniqueidentifier = (SELECT UserId FROM dbo.Users WHERE Email = LOWER(LTRIM(RTRIM(JSON_VALUE(@Payload, '$.sharedWithEmail')))) AND IsActive = 1);
        DECLARE @Permission varchar(10) = JSON_VALUE(@Payload, '$.permission');
        IF @Permission COLLATE Latin1_General_100_BIN2 NOT IN ('View', 'Edit') THROW 50013, 'Invalid share permission.', 1;
        IF @RecipientId IS NULL OR @RecipientId = @UserId THROW 50007, 'Share recipient not found.', 1;
        IF EXISTS (SELECT 1 FROM dbo.VaultItemShares WHERE VaultItemId = @Id AND SharedWithUserId = @RecipientId AND RevokedAt IS NULL)
            THROW 50022, 'An active share already exists.', 1;
        DECLARE @ShareId uniqueidentifier = NEWID();
        BEGIN TRANSACTION;
        INSERT dbo.VaultItemShares (ShareId, VaultItemId, OwnerUserId, SharedWithUserId, Permission)
            VALUES (@ShareId, @Id, @UserId, @RecipientId, @Permission);
        INSERT dbo.AuditLogs (UserId, VaultItemId, EventType, Detail, Severity)
            SELECT @UserId, @Id, 'VaultShared', ServiceName, 'Success' FROM dbo.VaultItems WHERE VaultItemId = @Id;
        COMMIT TRANSACTION;
        SELECT (SELECT s.ShareId AS id, s.VaultItemId AS vaultItemId,
                       JSON_QUERY((SELECT u.UserId AS id, u.Email AS email FROM dbo.Users u WHERE u.UserId = s.SharedWithUserId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)) AS sharedWith,
                       s.Permission AS permission, s.CreatedAt AS createdAt
                FROM dbo.VaultItemShares s WHERE s.ShareId = @ShareId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        RETURN;
    END;
    IF @Action = 'GetShares'
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.VaultItems WHERE VaultItemId = @Id AND UserId = @UserId) THROW 50012, 'Vault item not found.', 1;
        SELECT (SELECT s.ShareId AS id, s.VaultItemId AS vaultItemId,
                       JSON_QUERY((SELECT u.UserId AS id, u.Email AS email FROM dbo.Users u WHERE u.UserId = s.SharedWithUserId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)) AS sharedWith,
                       s.Permission AS permission, s.CreatedAt AS createdAt
                FROM dbo.VaultItemShares s WHERE s.VaultItemId = @Id AND s.OwnerUserId = @UserId AND s.RevokedAt IS NULL
                ORDER BY s.CreatedAt DESC FOR JSON PATH);
        RETURN;
    END;
    IF @Action = 'DeleteShare'
    BEGIN
        UPDATE dbo.VaultItemShares SET RevokedAt = SYSUTCDATETIME()
        WHERE ShareId = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@Payload, '$.shareId'))
          AND VaultItemId = @Id AND OwnerUserId = @UserId AND RevokedAt IS NULL;
        IF @@ROWCOUNT = 0 THROW 50014, 'Share not found.', 1;
        SELECT N'{}';
        RETURN;
    END;
    THROW 50000, 'Unknown vault item operation.', 1;
END;
GO
