
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, 2012 and Azure
-- --------------------------------------------------
-- Date Created: 01/03/2015 17:06:09
-- Generated from EDMX file: C:\Users\ethan_000\Source\Repos\IntegrationEngine\IntegrationEngine\Models\IntegrationEngineEntities.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------


-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[IntegrationJobs]', 'U') IS NOT NULL
    DROP TABLE [dbo].[IntegrationJobs];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'IntegrationJobs'
CREATE TABLE [dbo].[IntegrationJobs] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Interval] bigint  NOT NULL,
    [StartTimeUtc] datetimeoffset  NOT NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [Id] in table 'IntegrationJobs'
ALTER TABLE [dbo].[IntegrationJobs]
ADD CONSTRAINT [PK_IntegrationJobs]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------