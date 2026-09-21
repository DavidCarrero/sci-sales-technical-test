/* ============================================================================
   SCI Sales LLC - Senior .NET technical test
   02 - Tables

   dbo.Products is the only table the test needs. The constraints here are the
   same rules the domain enforces in C#: the database is the last line, not the
   only one.
   ============================================================================ */

USE SciSalesCatalog;
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        Id          INT            IDENTITY(1, 1) NOT NULL,
        Name        NVARCHAR(100)  NOT NULL,
        Description NVARCHAR(500)  NOT NULL CONSTRAINT DF_Products_Description DEFAULT (N''),
        Price       DECIMAL(18, 2) NOT NULL,
        /* DATETIME2(3) instead of the legacy DATETIME: same idea, wider range and
           it round-trips a .NET DateTimeOffset converted to UTC without drift. */
        CreatedDate DATETIME2(3)   NOT NULL CONSTRAINT DF_Products_CreatedDate DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Products_Price_Positive CHECK (Price > 0),
        CONSTRAINT CK_Products_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
    );

    PRINT N'Table dbo.Products created.';
END
ELSE
BEGIN
    PRINT N'Table dbo.Products already exists. Nothing to do.';
END
GO

/* Two products with the same name are a data-entry mistake, and the unique index
   is what makes the check race-proof: two concurrent inserts cannot both win. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Products_Name' AND object_id = OBJECT_ID(N'dbo.Products'))
BEGIN
    CREATE UNIQUE INDEX UX_Products_Name ON dbo.Products (Name);
    PRINT N'Unique index UX_Products_Name created.';
END
GO

/* Paging orders by CreatedDate DESC, Id DESC. This index keeps that sort off the
   heap once the catalog grows. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_CreatedDate' AND object_id = OBJECT_ID(N'dbo.Products'))
BEGIN
    CREATE INDEX IX_Products_CreatedDate ON dbo.Products (CreatedDate DESC, Id DESC);
    PRINT N'Index IX_Products_CreatedDate created.';
END
GO
