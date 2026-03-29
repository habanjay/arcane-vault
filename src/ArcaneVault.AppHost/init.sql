-- SQL Server init script

-- Create the AddressBook database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'arcanevaultdb')
BEGIN
  CREATE DATABASE arcanevaultdb;
END;
GO

USE arcanevaultdb;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[Credential]')
      AND type = N'U'
)
BEGIN
    CREATE TABLE [dbo].[Credential] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [Username] NVARCHAR(150) NOT NULL,
        [Password] NVARCHAR(MAX) NOT NULL,
        [Url] NVARCHAR(2083) NOT NULL,
        [Notes] NVARCHAR(MAX) NOT NULL,
        [Created] DATETIME2 NOT NULL CONSTRAINT DF_Credential_Created DEFAULT SYSUTCDATETIME(),
        [Modified] DATETIME2 NOT NULL,
        CONSTRAINT [PK_Credential] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    JOIN sys.objects o ON i.object_id = o.object_id
    WHERE i.name = N'IX_Credential_Username'
      AND o.object_id = OBJECT_ID(N'[dbo].[Credential]')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Credential_Username]
    ON [dbo].[Credential] ([Username]);
END;
GO