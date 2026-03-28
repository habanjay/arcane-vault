-- SQL Server init script

-- Create the AddressBook database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'arcanevaultdb')
BEGIN
  CREATE DATABASE arcanevaultdb;
END;
GO

USE arcanevaultdb;
GO