/* ============================================================================
   SCI Sales LLC - Senior .NET technical test
   01 - Database

   Creates the catalog database if it is not there yet. Safe to run twice.
   Run the scripts in this folder in numeric order with sqlcmd, SSMS or
   Azure Data Studio.
   ============================================================================ */

IF DB_ID(N'SciSalesCatalog') IS NULL
BEGIN
    PRINT N'Creating database SciSalesCatalog...';
    CREATE DATABASE SciSalesCatalog;
END
ELSE
BEGIN
    PRINT N'Database SciSalesCatalog already exists. Nothing to do.';
END
GO
