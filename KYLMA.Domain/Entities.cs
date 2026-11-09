using System;
using System.Collections.Generic;

namespace KYLMA.Domain
{
    public enum UserRole { Admin = 1, Manager = 2, Cashier = 3 }

    /// <summary>The signed-in user (session identity).</summary>
    public sealed class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>A user account as shown in User Administration.</summary>
    public sealed class UserAccount
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public sealed class RoleInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public override string ToString() { return Name; }
    }

    public sealed class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public override string ToString() { return Name; }
    }

    public sealed class Customer
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public override string ToString() { return FullName; }
    }

    public sealed class Product
    {
        public int Id { get; set; }
        public string Sku { get; set; }
        public string Barcode { get; set; }
        public string Name { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int StockQuantity { get; set; }
        public int ReorderLevel { get; set; }
        public bool IsActive { get; set; }
        /// <summary>Image bytes (JPEG/PNG). Only loaded when a single product is opened.</summary>
        public byte[] Image { get; set; }
        public bool IsLowStock { get { return StockQuantity <= ReorderLevel; } }
    }

    /// <summary>One line in the POS cart. Prices shown here are re-read by SQL Server at checkout.</summary>
    public sealed class CartLine
    {
        public int ProductId { get; set; }
        public string Sku { get; set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public int StockOnHand { get; set; }
        public decimal LineTotal { get { return Quantity * UnitPrice; } }
    }

    public sealed class SalesTotals
    {
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
    }

    public sealed class OrderHeader
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal ChangeDue { get; set; }
        public string Status { get; set; }
        public string CashierName { get; set; }
        public string CustomerName { get; set; }
    }

    public sealed class OrderLine
    {
        public string ProductName { get; set; }
        public string Sku { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public sealed class SalesDay
    {
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public sealed class TopProduct
    {
        public string ProductName { get; set; }
        public int QtySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public sealed class CategorySales
    {
        public string CategoryName { get; set; }
        public decimal Revenue { get; set; }
    }

    public sealed class StockMovement
    {
        public int ChangeQty { get; set; }
        public string Reason { get; set; }
        public string Username { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public sealed class DashboardMetrics
    {
        public decimal TodaySales { get; set; }
        public int TodayOrders { get; set; }
        public int LowStockCount { get; set; }
        public int ActiveProducts { get; set; }
    }

    /// <summary>Store details printed on receipts. Read from App.config.</summary>
    public sealed class BusinessProfile
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string ReturnPolicy { get; set; }
    }

    /// <summary>Receipt model, kept separate from database entities so printers do not depend on SQL.</summary>
    public sealed class ReceiptLine
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
    }

    public sealed class ReceiptData
    {
        public string BusinessName { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string OrderNumber { get; set; }
        public DateTime Date { get; set; }
        public string CashierName { get; set; }
        public string CustomerName { get; set; }
        public List<ReceiptLine> Lines { get; } = new List<ReceiptLine>();
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; }
        public decimal AmountTendered { get; set; }
        public decimal Change { get; set; }
        public string ReturnPolicy { get; set; }
    }
}
