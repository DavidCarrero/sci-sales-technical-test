/* ============================================================================
   SCI Sales LLC - Senior .NET technical test
   03 - Stored procedures

   Every data operation the API performs goes through one of these. The
   application never sends ad-hoc SQL.

   Return codes, shared by all of them:
       0  success
       1  conflict (a product with that name already exists)
       2  not found

   CREATE OR ALTER makes the script idempotent: run it as many times as you like.
   ============================================================================ */

USE SciSalesCatalog;
GO

/* ---------------------------------------------------------------------------
   CREATE
   Returns the new identity value through @Id.
   --------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.usp_Products_Create
    @Name        NVARCHAR(100),
    @Description NVARCHAR(500),
    @Price       DECIMAL(18, 2),
    @CreatedDate DATETIME2(3),
    @Id          INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Id = 0;

    BEGIN TRY
        INSERT INTO dbo.Products (Name, Description, Price, CreatedDate)
        VALUES (LTRIM(RTRIM(@Name)), @Description, @Price, @CreatedDate);

        SET @Id = CAST(SCOPE_IDENTITY() AS INT);

        RETURN 0;
    END TRY
    BEGIN CATCH
        /* 2601 / 2627: the unique index on Name rejected the row. Anything else is
           a real fault and is rethrown so the API logs it and answers 500. */
        IF ERROR_NUMBER() IN (2601, 2627)
        BEGIN
            RETURN 1;
        END

        THROW;
    END CATCH
END
GO

/* ---------------------------------------------------------------------------
   READ - one product
   --------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.usp_Products_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.Id,
        p.Name,
        p.Description,
        p.Price,
        p.CreatedDate
    FROM dbo.Products AS p
    WHERE p.Id = @Id;

    RETURN CASE WHEN @@ROWCOUNT = 0 THEN 2 ELSE 0 END;
END
GO

/* ---------------------------------------------------------------------------
   READ - one page, newest first
   --------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.usp_Products_GetAll
    @Skip INT = 0,
    @Take INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @Skip < 0 SET @Skip = 0;
    IF @Take <= 0 SET @Take = 20;
    IF @Take > 100 SET @Take = 100;

    SELECT
        p.Id,
        p.Name,
        p.Description,
        p.Price,
        p.CreatedDate
    FROM dbo.Products AS p
    ORDER BY p.CreatedDate DESC, p.Id DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;

    RETURN 0;
END
GO

/* ---------------------------------------------------------------------------
   READ - total count, for the paging metadata
   --------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.usp_Products_Count
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT_BIG(1) AS TotalItems FROM dbo.Products;

    RETURN 0;
END
GO

/* ---------------------------------------------------------------------------
   UPDATE
   --------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.usp_Products_Update
    @Id          INT,
    @Name        NVARCHAR(100),
    @Description NVARCHAR(500),
    @Price       DECIMAL(18, 2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = @Id)
    BEGIN
        RETURN 2;
    END

    BEGIN TRY
        UPDATE dbo.Products
        SET Name        = LTRIM(RTRIM(@Name)),
            Description = @Description,
            Price       = @Price
        WHERE Id = @Id;

        RETURN 0;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN (2601, 2627)
        BEGIN
            RETURN 1;
        END

        THROW;
    END CATCH
END
GO

/* ---------------------------------------------------------------------------
   DELETE
   --------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE dbo.usp_Products_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DELETE FROM dbo.Products
    WHERE Id = @Id;

    RETURN CASE WHEN @@ROWCOUNT = 0 THEN 2 ELSE 0 END;
END
GO

PRINT N'Stored procedures created or updated.';
GO
