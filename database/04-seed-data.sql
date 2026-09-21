/* ============================================================================
   SCI Sales LLC - Senior .NET technical test
   04 - Seed data (optional)

   Four products so the endpoints answer something on a fresh database. Prices
   are in USD, the catalog currency. Running it twice adds nothing new.
   ============================================================================ */

USE SciSalesCatalog;
GO

MERGE dbo.Products AS target
USING (VALUES
    (N'Wireless Mouse',      N'Six-button ergonomic mouse, 2.4 GHz receiver.',      24.99,  DATEADD(DAY, -30, SYSUTCDATETIME())),
    (N'Mechanical Keyboard', N'87-key tenkeyless board, brown switches.',           89.50,  DATEADD(DAY, -21, SYSUTCDATETIME())),
    (N'27 inch Monitor',     N'QHD IPS panel, 144 Hz, height adjustable stand.',    329.00, DATEADD(DAY, -14, SYSUTCDATETIME())),
    (N'USB-C Dock',          N'Dual HDMI, gigabit ethernet, 100 W power delivery.', 149.90, DATEADD(DAY, -3,  SYSUTCDATETIME()))
) AS source (Name, Description, Price, CreatedDate)
ON target.Name = source.Name
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Name, Description, Price, CreatedDate)
    VALUES (source.Name, source.Description, source.Price, source.CreatedDate);

-- A subquery cannot go inside PRINT, so the count travels through a variable.
DECLARE @Total INT = (SELECT COUNT(1) FROM dbo.Products);
PRINT CONCAT(N'Seed finished. Products in catalog: ', @Total);
GO
