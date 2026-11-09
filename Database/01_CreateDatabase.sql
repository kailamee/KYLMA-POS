/* ============================================================
   KYLMA-POS  |  Database script
   Database name = last name (Mangubat), per the project rules.
   Schema, seed data and core procedures come from SQL.docx.
   Extra procedures (search, orders, users, customers) are at the end.
   Safe to run more than once.
   ============================================================ */
USE master;
GO
IF DB_ID(N'Mangubat') IS NULL CREATE DATABASE Mangubat;
GO
USE Mangubat;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* ===================== TABLES ===================== */

IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
CREATE TABLE dbo.Roles
(
    RoleId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
    RoleName    NVARCHAR(50)  NOT NULL CONSTRAINT UQ_Roles_RoleName UNIQUE,
    Description NVARCHAR(200) NULL
);
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
CREATE TABLE dbo.Users
(
    UserId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    Username     NVARCHAR(50)  NOT NULL CONSTRAINT UQ_Users_Username UNIQUE,
    PasswordHash VARCHAR(64)   NOT NULL,
    Salt         VARCHAR(32)   NOT NULL,
    FullName     NVARCHAR(100) NOT NULL,
    RoleId       INT NOT NULL CONSTRAINT FK_Users_Roles REFERENCES dbo.Roles(RoleId),
    IsActive     BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    CreatedAt    DATETIME NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (GETDATE()),
    LastLoginAt  DATETIME NULL
);
GO

IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
CREATE TABLE dbo.Categories
(
    CategoryId  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL CONSTRAINT UQ_Categories_Name UNIQUE,
    Description NVARCHAR(200) NULL
);
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
CREATE TABLE dbo.Products
(
    ProductId    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
    Sku          NVARCHAR(30)  NOT NULL CONSTRAINT UQ_Products_Sku UNIQUE,
    Barcode      NVARCHAR(50)  NULL,
    Name         NVARCHAR(150) NOT NULL,
    CategoryId   INT NOT NULL CONSTRAINT FK_Products_Categories REFERENCES dbo.Categories(CategoryId),
    Price        DECIMAL(18,2) NOT NULL CONSTRAINT CK_Products_Price CHECK (Price >= 0),
    Cost         DECIMAL(18,2) NOT NULL CONSTRAINT CK_Products_Cost CHECK (Cost >= 0),
    StockQty     INT NOT NULL CONSTRAINT DF_Products_Stock DEFAULT (0) CONSTRAINT CK_Products_Stock CHECK (StockQty >= 0),
    ReorderLevel INT NOT NULL CONSTRAINT DF_Products_Reorder DEFAULT (5) CONSTRAINT CK_Products_Reorder CHECK (ReorderLevel >= 0),
    Image        VARBINARY(MAX) NULL,
    IsActive     BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT (1),
    CreatedAt    DATETIME NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT (GETDATE()),
    UpdatedAt    DATETIME NOT NULL CONSTRAINT DF_Products_UpdatedAt DEFAULT (GETDATE())
);
GO

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
CREATE TABLE dbo.Customers
(
    CustomerId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    FullName   NVARCHAR(100) NOT NULL,
    Phone      NVARCHAR(30)  NULL,
    Email      NVARCHAR(120) NULL,
    Address    NVARCHAR(250) NULL,
    CreatedAt  DATETIME NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT (GETDATE())
);
GO

IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
CREATE TABLE dbo.Orders
(
    OrderId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    OrderNumber    NVARCHAR(30) NOT NULL CONSTRAINT UQ_Orders_Number UNIQUE,
    CustomerId     INT NULL CONSTRAINT FK_Orders_Customers REFERENCES dbo.Customers(CustomerId),
    UserId         INT NOT NULL CONSTRAINT FK_Orders_Users REFERENCES dbo.Users(UserId),
    OrderDate      DATETIME NOT NULL CONSTRAINT DF_Orders_Date DEFAULT (GETDATE()),
    Subtotal       DECIMAL(18,2) NOT NULL,
    TaxAmount      DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_Discount DEFAULT (0),
    Total          DECIMAL(18,2) NOT NULL,
    PaymentMethod  NVARCHAR(20) NOT NULL CONSTRAINT CK_Orders_Payment CHECK (PaymentMethod IN (N'Cash', N'Card', N'Mobile')),
    AmountPaid     DECIMAL(18,2) NOT NULL,
    ChangeDue      DECIMAL(18,2) NOT NULL,
    Status         NVARCHAR(20) NOT NULL CONSTRAINT DF_Orders_Status DEFAULT (N'Completed') CONSTRAINT CK_Orders_Status CHECK (Status IN (N'Completed', N'Voided'))
);
GO

IF OBJECT_ID(N'dbo.OrderDetails', N'U') IS NULL
CREATE TABLE dbo.OrderDetails
(
    OrderDetailId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderDetails PRIMARY KEY,
    OrderId       INT NOT NULL CONSTRAINT FK_OrderDetails_Orders REFERENCES dbo.Orders(OrderId) ON DELETE CASCADE,
    ProductId     INT NOT NULL CONSTRAINT FK_OrderDetails_Products REFERENCES dbo.Products(ProductId),
    Quantity      INT NOT NULL CONSTRAINT CK_OrderDetails_Qty CHECK (Quantity > 0),
    UnitPrice     DECIMAL(18,2) NOT NULL,
    LineTotal     DECIMAL(18,2) NOT NULL
);
GO

IF OBJECT_ID(N'dbo.InventoryLog', N'U') IS NULL
CREATE TABLE dbo.InventoryLog
(
    LogId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryLog PRIMARY KEY,
    ProductId INT NOT NULL CONSTRAINT FK_InventoryLog_Products REFERENCES dbo.Products(ProductId),
    ChangeQty INT NOT NULL,
    Reason    NVARCHAR(200) NOT NULL,
    UserId    INT NOT NULL CONSTRAINT FK_InventoryLog_Users REFERENCES dbo.Users(UserId),
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_InventoryLog_Created DEFAULT (GETDATE())
);
GO

/* ===================== INDEXES ===================== */

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Products_Barcode')
CREATE UNIQUE INDEX UX_Products_Barcode ON dbo.Products(Barcode) WHERE Barcode IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_Name')
CREATE INDEX IX_Products_Name ON dbo.Products(Name);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_CategoryId')
CREATE INDEX IX_Products_CategoryId ON dbo.Products(CategoryId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_RoleId')
CREATE INDEX IX_Users_RoleId ON dbo.Users(RoleId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_OrderDate')
CREATE INDEX IX_Orders_OrderDate ON dbo.Orders(OrderDate) INCLUDE (Total, Status);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_UserId')
CREATE INDEX IX_Orders_UserId ON dbo.Orders(UserId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_CustomerId')
CREATE INDEX IX_Orders_CustomerId ON dbo.Orders(CustomerId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderDetails_OrderId')
CREATE INDEX IX_OrderDetails_OrderId ON dbo.OrderDetails(OrderId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderDetails_ProductId')
CREATE INDEX IX_OrderDetails_ProductId ON dbo.OrderDetails(ProductId) INCLUDE (Quantity, LineTotal);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InventoryLog_Product')
CREATE INDEX IX_InventoryLog_Product ON dbo.InventoryLog(ProductId, CreatedAt DESC);
GO

/* ===================== AUTH ===================== */

CREATE OR ALTER PROCEDURE dbo.usp_Login @Username NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.UserId, u.Username, u.PasswordHash, u.Salt, u.FullName, u.RoleId, r.RoleName, u.IsActive
    FROM dbo.Users u
    JOIN dbo.Roles r ON r.RoleId = u.RoleId
    WHERE u.Username = @Username;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_TouchLogin @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users SET LastLoginAt = GETDATE() WHERE UserId = @UserId;
END
GO

/* ===================== PRODUCTS ===================== */

CREATE OR ALTER PROCEDURE dbo.usp_Product_Insert
    @Sku NVARCHAR(30), @Barcode NVARCHAR(50) = NULL, @Name NVARCHAR(150),
    @CategoryId INT, @Price DECIMAL(18,2), @Cost DECIMAL(18,2),
    @StockQty INT, @ReorderLevel INT, @Image VARBINARY(MAX) = NULL, @UserId INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        INSERT dbo.Products (Sku, Barcode, Name, CategoryId, Price, Cost, StockQty, ReorderLevel, Image)
        VALUES (@Sku, @Barcode, @Name, @CategoryId, @Price, @Cost, @StockQty, @ReorderLevel, @Image);
        DECLARE @Id INT = SCOPE_IDENTITY();
        IF @StockQty > 0
            INSERT dbo.InventoryLog (ProductId, ChangeQty, Reason, UserId) VALUES (@Id, @StockQty, N'Initial stock', @UserId);
        COMMIT;
        SELECT @Id AS Id;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Product_Update
    @ProductId INT, @Sku NVARCHAR(30), @Barcode NVARCHAR(50) = NULL, @Name NVARCHAR(150),
    @CategoryId INT, @Price DECIMAL(18,2), @Cost DECIMAL(18,2), @ReorderLevel INT, @IsActive BIT,
    @Image VARBINARY(MAX) = NULL, @ChangeImage BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Products
    SET Sku = @Sku, Barcode = @Barcode, Name = @Name, CategoryId = @CategoryId,
        Price = @Price, Cost = @Cost, ReorderLevel = @ReorderLevel, IsActive = @IsActive,
        Image = CASE WHEN @ChangeImage = 1 THEN @Image ELSE Image END,
        UpdatedAt = GETDATE()
    WHERE ProductId = @ProductId;
    IF @@ROWCOUNT = 0 THROW 50001, N'Product not found.', 1;
END
GO

-- Soft delete: keeps sales history intact.
CREATE OR ALTER PROCEDURE dbo.usp_Product_Delete @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Products SET IsActive = 0, UpdatedAt = GETDATE() WHERE ProductId = @ProductId;
    IF @@ROWCOUNT = 0 THROW 50001, N'Product not found.', 1;
END
GO

/* ===================== INVENTORY ===================== */

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_Adjust
    @ProductId INT, @ChangeQty INT, @Reason NVARCHAR(200), @UserId INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        DECLARE @Cur INT;
        SELECT @Cur = StockQty FROM dbo.Products WITH (UPDLOCK, ROWLOCK) WHERE ProductId = @ProductId;
        IF @Cur IS NULL THROW 50010, N'Product not found.', 1;
        IF @Cur + @ChangeQty < 0 THROW 50011, N'Adjustment would make stock negative.', 1;
        UPDATE dbo.Products SET StockQty = @Cur + @ChangeQty, UpdatedAt = GETDATE() WHERE ProductId = @ProductId;
        INSERT dbo.InventoryLog (ProductId, ChangeQty, Reason, UserId) VALUES (@ProductId, @ChangeQty, @Reason, @UserId);
        COMMIT;
        SELECT @Cur + @ChangeQty AS NewStock;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO

/* ===================== ORDERS (transactional) ===================== */
-- @ItemsXml format: <Items><Item ProductId="1" Quantity="2"/></Items>
-- Prices are always read from the database, never from the client.

CREATE OR ALTER PROCEDURE dbo.usp_Order_Create
    @UserId INT, @CustomerId INT = NULL, @PaymentMethod NVARCHAR(20),
    @DiscountAmount DECIMAL(18,2) = 0, @TaxRate DECIMAL(5,2) = 0,
    @AmountPaid DECIMAL(18,2), @ItemsXml XML
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;

        DECLARE @Items TABLE (ProductId INT NOT NULL PRIMARY KEY, Quantity INT NOT NULL);

        INSERT @Items (ProductId, Quantity)
        SELECT ProductId, SUM(Quantity)
        FROM (SELECT x.value('@ProductId', 'INT') AS ProductId,
                     x.value('@Quantity', 'INT') AS Quantity
              FROM @ItemsXml.nodes('/Items/Item') AS t(x)) s
        GROUP BY ProductId;

        IF NOT EXISTS (SELECT 1 FROM @Items) THROW 50020, N'The order has no items.', 1;
        IF EXISTS (SELECT 1 FROM @Items WHERE Quantity <= 0) THROW 50021, N'Invalid item quantity.', 1;

        -- Lock rows and validate availability.
        IF EXISTS (SELECT 1 FROM @Items i
                   LEFT JOIN dbo.Products p WITH (UPDLOCK, ROWLOCK) ON p.ProductId = i.ProductId
                   WHERE p.ProductId IS NULL OR p.IsActive = 0)
            THROW 50022, N'One or more products are unavailable.', 1;

        DECLARE @Msg NVARCHAR(200) =
            (SELECT TOP 1 N'Insufficient stock for ' + p.Name + N' (available: ' + CAST(p.StockQty AS NVARCHAR(10)) + N').'
             FROM @Items i JOIN dbo.Products p ON p.ProductId = i.ProductId
             WHERE p.StockQty < i.Quantity);
        IF @Msg IS NOT NULL THROW 50023, @Msg, 1;

        DECLARE @Subtotal DECIMAL(18,2) =
            (SELECT SUM(p.Price * i.Quantity) FROM @Items i JOIN dbo.Products p ON p.ProductId = i.ProductId);

        IF @DiscountAmount < 0 OR @DiscountAmount > @Subtotal THROW 50024, N'Invalid discount amount.', 1;

        DECLARE @Tax DECIMAL(18,2) = ROUND((@Subtotal - @DiscountAmount) * @TaxRate / 100.0, 2);
        DECLARE @Total DECIMAL(18,2) = @Subtotal - @DiscountAmount + @Tax;

        IF @AmountPaid < @Total THROW 50025, N'Amount paid is less than the total.', 1;

        INSERT dbo.Orders (OrderNumber, CustomerId, UserId, OrderDate, Subtotal, TaxAmount, DiscountAmount, Total,
                           PaymentMethod, AmountPaid, ChangeDue, Status)
        VALUES (LEFT(CONVERT(NVARCHAR(36), NEWID()), 30), @CustomerId, @UserId, GETDATE(), @Subtotal, @Tax,
                @DiscountAmount, @Total, @PaymentMethod, @AmountPaid, @AmountPaid - @Total, N'Completed');

        DECLARE @OrderId INT = SCOPE_IDENTITY();
        DECLARE @OrderNumber NVARCHAR(30) = N'ORD-' + CONVERT(NVARCHAR(8), GETDATE(), 112) + N'-'
            + RIGHT(N'000000' + CAST(@OrderId AS NVARCHAR(10)), 6);

        UPDATE dbo.Orders SET OrderNumber = @OrderNumber WHERE OrderId = @OrderId;

        INSERT dbo.OrderDetails (OrderId, ProductId, Quantity, UnitPrice, LineTotal)
        SELECT @OrderId, i.ProductId, i.Quantity, p.Price, p.Price * i.Quantity
        FROM @Items i JOIN dbo.Products p ON p.ProductId = i.ProductId;

        UPDATE p SET StockQty = p.StockQty - i.Quantity, UpdatedAt = GETDATE()
        FROM dbo.Products p JOIN @Items i ON i.ProductId = p.ProductId;

        INSERT dbo.InventoryLog (ProductId, ChangeQty, Reason, UserId)
        SELECT i.ProductId, -i.Quantity, N'Sale ' + @OrderNumber, @UserId FROM @Items i;

        COMMIT;
        SELECT @OrderId AS Id;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END
GO

/* ===================== REPORTS ===================== */

CREATE OR ALTER PROCEDURE dbo.usp_Report_DailySales @From DATETIME, @To DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CAST(OrderDate AS DATE) AS SaleDate, COUNT(*) AS OrderCount, SUM(Total) AS Revenue
    FROM dbo.Orders
    WHERE Status = N'Completed' AND OrderDate >= @From AND OrderDate < DATEADD(DAY, 1, @To)
    GROUP BY CAST(OrderDate AS DATE)
    ORDER BY SaleDate;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Report_TopProducts @From DATETIME, @To DATETIME, @Top INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@Top) p.Name AS ProductName, SUM(d.Quantity) AS QtySold, SUM(d.LineTotal) AS Revenue
    FROM dbo.OrderDetails d
    JOIN dbo.Orders o ON o.OrderId = d.OrderId
    JOIN dbo.Products p ON p.ProductId = d.ProductId
    WHERE o.Status = N'Completed' AND o.OrderDate >= @From AND o.OrderDate < DATEADD(DAY, 1, @To)
    GROUP BY p.Name
    ORDER BY SUM(d.LineTotal) DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Report_SalesByCategory @From DATETIME, @To DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.Name AS CategoryName, SUM(d.LineTotal) AS Revenue
    FROM dbo.OrderDetails d
    JOIN dbo.Orders o ON o.OrderId = d.OrderId
    JOIN dbo.Products p ON p.ProductId = d.ProductId
    JOIN dbo.Categories c ON c.CategoryId = p.CategoryId
    WHERE o.Status = N'Completed' AND o.OrderDate >= @From AND o.OrderDate < DATEADD(DAY, 1, @To)
    GROUP BY c.Name
    ORDER BY SUM(d.LineTotal) DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Report_LowStock
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ProductId, Sku, Name, StockQty, ReorderLevel
    FROM dbo.Products
    WHERE IsActive = 1 AND StockQty <= ReorderLevel
    ORDER BY StockQty, Name;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Report_Dashboard
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        (SELECT ISNULL(SUM(Total), 0) FROM dbo.Orders WHERE Status = N'Completed' AND OrderDate >= CAST(GETDATE() AS DATE)) AS TodaySales,
        (SELECT COUNT(*) FROM dbo.Orders WHERE Status = N'Completed' AND OrderDate >= CAST(GETDATE() AS DATE)) AS TodayOrders,
        (SELECT COUNT(*) FROM dbo.Products WHERE IsActive = 1 AND StockQty <= ReorderLevel) AS LowStockCount,
        (SELECT COUNT(*) FROM dbo.Products WHERE IsActive = 1) AS ActiveProducts;
END
GO

/* ===================== SEED DATA (inserted once) ===================== */

IF NOT EXISTS (SELECT 1 FROM dbo.Roles)
INSERT dbo.Roles (RoleName, Description) VALUES
    (N'Admin',   N'Full access including user management'),
    (N'Manager', N'POS, products, inventory and analytics'),
    (N'Cashier', N'Point of sale only');
GO

/* Users: hash = SHA-256 of (Salt + Password), uppercase hex.
   admin / Admin@123     manager / Manager@123     cashier / Cashier@123
   Change these passwords before going live. */
DECLARE @s1 VARCHAR(32) = 'A1B2C3D4E5F60718293A4B5C6D7E8F90';
DECLARE @s2 VARCHAR(32) = '0F1E2D3C4B5A69788796A5B4C3D2E1F0';
DECLARE @s3 VARCHAR(32) = '1122334455667788AABBCCDDEEFF0011';

IF NOT EXISTS (SELECT 1 FROM dbo.Users)
INSERT dbo.Users (Username, PasswordHash, Salt, FullName, RoleId) VALUES
    (N'admin',   CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', @s1 + 'Admin@123'), 2),   @s1, N'System Administrator', (SELECT RoleId FROM dbo.Roles WHERE RoleName = N'Admin')),
    (N'manager', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', @s2 + 'Manager@123'), 2), @s2, N'Store Manager',        (SELECT RoleId FROM dbo.Roles WHERE RoleName = N'Manager')),
    (N'cashier', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', @s3 + 'Cashier@123'), 2), @s3, N'Front Desk Cashier',   (SELECT RoleId FROM dbo.Roles WHERE RoleName = N'Cashier'));
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Categories)
INSERT dbo.Categories (Name, Description) VALUES
    (N'Beverages', N'Drinks'),
    (N'Snacks', N'Chips, biscuits and sweets'),
    (N'Groceries', N'Pantry staples'),
    (N'Household', N'Cleaning and home'),
    (N'Personal Care', N'Hygiene and beauty');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
    INSERT dbo.Products (Sku, Barcode, Name, CategoryId, Price, Cost, StockQty, ReorderLevel) VALUES
    (N'BEV-001', N'8901000000011', N'Mineral Water 500ml',  (SELECT CategoryId FROM dbo.Categories WHERE Name = N'Beverages'),     1.00,  0.45, 200, 40),
    (N'BEV-002', N'8901000000028', N'Cola 330ml Can',       (SELECT CategoryId FROM dbo.Categories WHERE Name = N'Beverages'),     1.50,  0.80, 150, 30),
    (N'BEV-003', N'8901000000035', N'Orange Juice 1L',      (SELECT CategoryId FROM dbo.Categories WHERE Name = N'Beverages'),     3.20,  1.90,  60, 15),
    (N'SNK-001', N'8902000000014', N'Potato Chips 150g',    (SELECT CategoryId FROM dbo.Categories WHERE Name = N'Snacks'),        2.50,  1.30,  90, 20),
    (N'SNK-002', N'8902000000021', N'Chocolate Bar 50g',    (SELECT CategoryId FROM dbo.Categories WHERE Name = N'Snacks'),        1.80,  0.95, 120, 25),
    (N'SNK-003', N'8902000000038', N'Salted Peanuts 200g',  (SELECT CategoryId FROM dbo.Categories WHERE Name = N'Snacks'),        2.90,  1.60,   8, 10),
    (N'GRO-001', N'8903000000017', N'Basmati Rice 5kg',     (SELECT CategoryId FROM dbo.Categories WHERE Name = N'Groceries'),    12.50,  8.90,  40, 10),
    (N'GRO-002', N'8903000000024', N'Olive Oil 500ml',      (SELECT CategoryId FROM dbo.Categories WHERE Name = N'Groceries'),     7.80,  5.20,  35,  8),
    (N'GRO-003', N'8903000000031', N'Spaghetti 500g',       (SELECT CategoryId FROM dbo.Categories WHERE Name = N'Groceries'),     2.10,  1.10,  70, 15),
    (N'HOU-001', N'8904000000010', N'Dish Soap 750ml',      (SELECT CategoryId FROM dbo.Categories WHERE Name = N'Household'),     3.60,  2.00,  45, 10),
    (N'HOU-002', N'8904000000027', N'Paper Towels 2-pack',  (SELECT CategoryId FROM dbo.Categories WHERE Name = N'Household'),     4.20,  2.60,   5, 10),
    (N'PER-001', N'8905000000013', N'Shampoo 400ml',        (SELECT CategoryId FROM dbo.Categories WHERE Name = N'Personal Care'), 6.90,  4.10,  30,  8),
    (N'PER-002', N'8905000000020', N'Toothpaste 120g',      (SELECT CategoryId FROM dbo.Categories WHERE Name = N'Personal Care'), 3.40,  1.70,  55, 12);

    INSERT dbo.InventoryLog (ProductId, ChangeQty, Reason, UserId)
    SELECT ProductId, StockQty, N'Initial stock', (SELECT UserId FROM dbo.Users WHERE Username = N'admin')
    FROM dbo.Products;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Customers)
INSERT dbo.Customers (FullName, Phone, Email, Address) VALUES
    (N'Alice Tan',    N'+65 8111 2233', N'alice@example.com', N'12 Orchard Road'),
    (N'Brian Lim',    N'+65 8222 3344', N'brian@example.com', N'45 Bukit Timah Road'),
    (N'Carla Gomez',  N'+65 8333 4455', NULL,                 NULL);
GO

/* ===================== EXTENDED PROCEDURES (used by the app) ===================== */

CREATE OR ALTER PROCEDURE dbo.usp_Product_Search
    @Search NVARCHAR(100) = NULL, @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.ProductId, p.Sku, p.Barcode, p.Name, p.CategoryId, c.Name AS CategoryName,
           p.Price, p.Cost, p.StockQty, p.ReorderLevel, p.IsActive
    FROM dbo.Products p
    JOIN dbo.Categories c ON c.CategoryId = p.CategoryId
    WHERE (@IncludeInactive = 1 OR p.IsActive = 1)
      AND (@Search IS NULL OR p.Sku LIKE N'%' + @Search + N'%'
                           OR p.Name LIKE N'%' + @Search + N'%'
                           OR p.Barcode LIKE N'%' + @Search + N'%')
    ORDER BY p.Name;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Product_GetById @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.ProductId, p.Sku, p.Barcode, p.Name, p.CategoryId, c.Name AS CategoryName,
           p.Price, p.Cost, p.StockQty, p.ReorderLevel, p.IsActive, p.Image
    FROM dbo.Products p
    JOIN dbo.Categories c ON c.CategoryId = p.CategoryId
    WHERE p.ProductId = @ProductId;
END
GO

-- Barcode/SKU lookup for the POS. Active products only.
CREATE OR ALTER PROCEDURE dbo.usp_Product_GetByCode @Code NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.ProductId, p.Sku, p.Barcode, p.Name, p.CategoryId, c.Name AS CategoryName,
           p.Price, p.Cost, p.StockQty, p.ReorderLevel, p.IsActive
    FROM dbo.Products p
    JOIN dbo.Categories c ON c.CategoryId = p.CategoryId
    WHERE p.IsActive = 1 AND (p.Sku = @Code OR p.Barcode = @Code);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_InventoryLog_List @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (200) l.LogId, l.ChangeQty, l.Reason, u.Username, l.CreatedAt
    FROM dbo.InventoryLog l
    JOIN dbo.Users u ON u.UserId = l.UserId
    WHERE l.ProductId = @ProductId
    ORDER BY l.CreatedAt DESC, l.LogId DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Category_List
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, Name, Description FROM dbo.Categories ORDER BY Name;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Category_Insert @Name NVARCHAR(100), @Description NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT dbo.Categories (Name, Description) VALUES (@Name, @Description);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Customer_List @Search NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CustomerId, FullName, Phone, Email, Address
    FROM dbo.Customers
    WHERE @Search IS NULL OR FullName LIKE N'%' + @Search + N'%' OR Phone LIKE N'%' + @Search + N'%'
    ORDER BY FullName;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Customer_Insert
    @FullName NVARCHAR(100), @Phone NVARCHAR(30) = NULL, @Email NVARCHAR(120) = NULL, @Address NVARCHAR(250) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT dbo.Customers (FullName, Phone, Email, Address) VALUES (@FullName, @Phone, @Email, @Address);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END
GO

-- Order header by id or by order number (used for receipts and reprints).
CREATE OR ALTER PROCEDURE dbo.usp_Order_Header @OrderId INT = NULL, @OrderNumber NVARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) o.OrderId, o.OrderNumber, o.OrderDate, o.Subtotal, o.TaxAmount, o.DiscountAmount,
           o.Total, o.PaymentMethod, o.AmountPaid, o.ChangeDue, o.Status,
           u.FullName AS CashierName, c.FullName AS CustomerName
    FROM dbo.Orders o
    JOIN dbo.Users u ON u.UserId = o.UserId
    LEFT JOIN dbo.Customers c ON c.CustomerId = o.CustomerId
    WHERE (@OrderId IS NOT NULL AND o.OrderId = @OrderId)
       OR (@OrderNumber IS NOT NULL AND o.OrderNumber = @OrderNumber);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Order_Lines @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.Name AS ProductName, p.Sku, d.Quantity, d.UnitPrice, d.LineTotal
    FROM dbo.OrderDetails d
    JOIN dbo.Products p ON p.ProductId = d.ProductId
    WHERE d.OrderId = @OrderId
    ORDER BY d.OrderDetailId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Role_List
AS
BEGIN
    SET NOCOUNT ON;
    SELECT RoleId, RoleName, Description FROM dbo.Roles ORDER BY RoleId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_List
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.UserId, u.Username, u.FullName, u.RoleId, r.RoleName, u.IsActive, u.LastLoginAt, u.CreatedAt
    FROM dbo.Users u
    JOIN dbo.Roles r ON r.RoleId = u.RoleId
    ORDER BY u.Username;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_Create
    @Username NVARCHAR(50), @PasswordHash VARCHAR(64), @Salt VARCHAR(32), @FullName NVARCHAR(100), @RoleId INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT dbo.Users (Username, PasswordHash, Salt, FullName, RoleId)
    VALUES (@Username, @PasswordHash, @Salt, @FullName, @RoleId);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_Update
    @UserId INT, @FullName NVARCHAR(100), @RoleId INT, @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users SET FullName = @FullName, RoleId = @RoleId, IsActive = @IsActive WHERE UserId = @UserId;
    IF @@ROWCOUNT = 0 THROW 50030, N'User not found.', 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_User_SetPassword @UserId INT, @PasswordHash VARCHAR(64), @Salt VARCHAR(32)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users SET PasswordHash = @PasswordHash, Salt = @Salt WHERE UserId = @UserId;
    IF @@ROWCOUNT = 0 THROW 50030, N'User not found.', 1;
END
GO
