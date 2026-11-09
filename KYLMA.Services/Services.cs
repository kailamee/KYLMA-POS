using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KYLMA.Data;
using KYLMA.Domain;

namespace KYLMA.Services
{
    /// <summary>A problem the user can fix. Its message is safe to show on screen.</summary>
    public sealed class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message) { }
    }

    /// <summary>
    /// Role checks. Hiding a menu is not security, so every service calls these.
    /// Admin: everything. Manager: products, inventory, reports. Cashier: sales only.
    /// </summary>
    public static class Permissions
    {
        public static bool CanManageInventory(User user)
        {
            return user != null && (user.Role == UserRole.Admin || user.Role == UserRole.Manager);
        }

        public static bool CanViewReports(User user)
        {
            return CanManageInventory(user);
        }

        public static bool CanManageUsers(User user)
        {
            return user != null && user.Role == UserRole.Admin;
        }

        public static void Require(bool allowed, string message)
        {
            if (!allowed) throw new BusinessRuleException(message);
        }
    }

    /// <summary>Input cleanup shared by all services.</summary>
    internal static class Input
    {
        public static string Required(string value, int maxLength, string field)
        {
            var text = Optional(value, maxLength, field);
            if (text == null) throw new BusinessRuleException(field + " is required.");
            return text;
        }

        public static string Optional(string value, int maxLength, string field)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var text = value.Trim();
            if (text.Length > maxLength)
                throw new BusinessRuleException(field + " must be " + maxLength + " characters or fewer.");
            return text;
        }
    }

    /// <summary>Product images: only real JPEG or PNG data, up to 2 MB. Checked by file signature, not extension.</summary>
    public static class ImageRules
    {
        public const int MaxBytes = 2 * 1024 * 1024;

        public static void Validate(byte[] data)
        {
            if (data == null || data.Length == 0) return;
            if (data.Length > MaxBytes) throw new BusinessRuleException("Image must be 2 MB or smaller.");

            bool jpeg = data.Length > 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF;
            bool png = data.Length > 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47;
            if (!jpeg && !png) throw new BusinessRuleException("Only JPEG and PNG images are allowed.");
        }
    }

    public sealed class AuthService
    {
        private readonly UserRepository _users = new UserRepository();

        /// <summary>Returns the user for valid credentials, or null. The caller shows one generic message for both cases.</summary>
        public User Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password)) return null;
            return _users.Login(username.Trim(), password);
        }
    }

    public sealed class InventoryService
    {
        private readonly ProductRepository _products = new ProductRepository();
        private readonly CategoryRepository _categories = new CategoryRepository();

        public List<Product> Search(string text, bool includeInactive)
        {
            return _products.Search(text, includeInactive);
        }

        public Product GetById(int productId)
        {
            return _products.GetById(productId);
        }

        public Product FindByCode(string code)
        {
            return string.IsNullOrWhiteSpace(code) ? null : _products.GetByCode(code.Trim());
        }

        public List<Category> Categories()
        {
            return _categories.List();
        }

        public int AddCategory(User actor, string name, string description)
        {
            Permissions.Require(Permissions.CanManageInventory(actor), "Only managers and admins can manage categories.");
            return _categories.Insert(Input.Required(name, 100, "Category name"),
                                      Input.Optional(description, 200, "Description"));
        }

        /// <summary>Creates (Id = 0) or updates a product. Returns the product Id.</summary>
        public int Save(User actor, Product product, bool changeImage)
        {
            Permissions.Require(Permissions.CanManageInventory(actor), "Only managers and admins can change products.");
            Validate(product, changeImage);
            if (product.Id == 0) return _products.Insert(product, actor.Id);

            _products.Update(product, changeImage);
            return product.Id;
        }

        /// <summary>Soft delete: the product is hidden from sales, its history is kept.</summary>
        public void Delete(User actor, int productId)
        {
            Permissions.Require(Permissions.CanManageInventory(actor), "Only managers and admins can delete products.");
            _products.SoftDelete(productId);
        }

        public int Adjust(User actor, int productId, int change, string reason)
        {
            Permissions.Require(Permissions.CanManageInventory(actor), "Only managers and admins can adjust stock.");
            Permissions.Require(change != 0, "Enter a quantity other than zero.");
            var note = Input.Required(reason, 200, "Reason");
            return _products.Adjust(productId, change, note, actor.Id);
        }

        public List<StockMovement> History(int productId)
        {
            return _products.Movements(productId);
        }

        private static void Validate(Product p, bool changeImage)
        {
            p.Sku = Input.Required(p.Sku, 30, "SKU").ToUpperInvariant();
            if (!Regex.IsMatch(p.Sku, "^[A-Z0-9_-]+$"))
                throw new BusinessRuleException("SKU may only contain letters, digits, '-' and '_'.");

            p.Barcode = Input.Optional(p.Barcode, 50, "Barcode");
            if (p.Barcode != null && !Regex.IsMatch(p.Barcode, "^[A-Za-z0-9-]{4,50}$"))
                throw new BusinessRuleException("Barcode must be 4 to 50 letters or digits.");

            p.Name = Input.Required(p.Name, 150, "Product name");
            if (p.CategoryId <= 0) throw new BusinessRuleException("Select a category.");
            if (p.CostPrice < 0) throw new BusinessRuleException("Cost price cannot be negative.");
            if (p.SellingPrice < 0) throw new BusinessRuleException("Selling price cannot be negative.");
            if (p.StockQuantity < 0) throw new BusinessRuleException("Stock cannot be negative.");
            if (p.ReorderLevel < 0) throw new BusinessRuleException("Reorder level cannot be negative.");

            if ((p.Id == 0 || changeImage) && p.Image != null) ImageRules.Validate(p.Image);
        }
    }

    public sealed class CustomerService
    {
        private readonly CustomerRepository _customers = new CustomerRepository();

        public List<Customer> Search(string text)
        {
            return _customers.Search(Input.Optional(text, 100, "Search"));
        }

        public int Add(Customer customer)
        {
            customer.FullName = Input.Required(customer.FullName, 100, "Customer name");
            customer.Phone = Input.Optional(customer.Phone, 30, "Phone");
            customer.Email = Input.Optional(customer.Email, 120, "Email");
            if (customer.Email != null && !Regex.IsMatch(customer.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new BusinessRuleException("Enter a valid email address or leave it blank.");
            customer.Address = Input.Optional(customer.Address, 250, "Address");
            return _customers.Insert(customer);
        }
    }

    public sealed class SalesService
    {
        public static readonly string[] PaymentMethods = { "Cash", "Card", "Mobile" };

        private readonly OrderRepository _orders = new OrderRepository();

        /// <summary>
        /// Same rule as dbo.usp_Order_Create: tax = ROUND((subtotal - discount) * rate / 100, 2),
        /// rounding half away from zero like T-SQL.
        /// </summary>
        public static SalesTotals Calculate(IEnumerable<CartLine> lines, decimal discount, decimal taxRatePercent)
        {
            var subtotal = lines.Sum(l => l.LineTotal);
            var taxable = subtotal - discount;
            var tax = Math.Round(taxable * taxRatePercent / 100m, 2, MidpointRounding.AwayFromZero);
            return new SalesTotals { Subtotal = subtotal, Discount = discount, Tax = tax, Total = taxable + tax };
        }

        /// <summary>Saves the sale through the transactional procedure and returns the new order Id.</summary>
        public int Checkout(User cashier, IList<CartLine> lines, int? customerId, string paymentMethod,
                            decimal discount, decimal taxRatePercent, decimal tendered)
        {
            Permissions.Require(cashier != null, "Sign in before taking a payment.");
            Permissions.Require(lines != null && lines.Count > 0, "The cart is empty.");
            Permissions.Require(PaymentMethods.Contains(paymentMethod), "Choose a payment method.");
            Permissions.Require(lines.All(l => l.Quantity >= 1 && l.Quantity <= 999),
                                "Each quantity must be between 1 and 999.");
            Permissions.Require(discount >= 0, "Discount cannot be negative.");

            var totals = Calculate(lines, discount, taxRatePercent);
            Permissions.Require(discount <= totals.Subtotal, "Discount cannot be more than the subtotal.");
            Permissions.Require(paymentMethod != "Cash" || tendered >= totals.Total,
                                "Cash tendered is less than the total.");

            var amountPaid = paymentMethod == "Cash" ? tendered : totals.Total;
            return _orders.Create(cashier.Id, customerId, paymentMethod, discount, taxRatePercent, amountPaid, lines);
        }
    }

    /// <summary>Builds receipts from committed orders only, so printed numbers always match the database.</summary>
    public sealed class ReceiptService
    {
        private readonly OrderRepository _orders = new OrderRepository();
        private readonly BusinessProfile _profile;

        public ReceiptService(BusinessProfile profile)
        {
            _profile = profile ?? new BusinessProfile();
        }

        public ReceiptData ForOrderId(int orderId)
        {
            return Build(_orders.Header(orderId, null));
        }

        public ReceiptData ForOrderNumber(string orderNumber)
        {
            var number = Input.Required(orderNumber, 30, "Order number");
            return Build(_orders.Header(null, number));
        }

        private ReceiptData Build(OrderHeader header)
        {
            if (header == null) throw new BusinessRuleException("No order matches that number.");

            var receipt = new ReceiptData
            {
                BusinessName = _profile.Name,
                Address = _profile.Address,
                Phone = _profile.Phone,
                OrderNumber = header.OrderNumber,
                Date = header.OrderDate,
                CashierName = header.CashierName,
                CustomerName = header.CustomerName,
                Subtotal = header.Subtotal,
                Tax = header.TaxAmount,
                Discount = header.DiscountAmount,
                Total = header.Total,
                PaymentMethod = header.PaymentMethod,
                AmountTendered = header.AmountPaid,
                Change = header.ChangeDue,
                ReturnPolicy = _profile.ReturnPolicy
            };

            foreach (var line in _orders.Lines(header.Id))
            {
                receipt.Lines.Add(new ReceiptLine
                {
                    ProductName = line.ProductName,
                    Quantity = line.Quantity,
                    Amount = line.LineTotal
                });
            }
            return receipt;
        }
    }

    public sealed class ReportingService
    {
        private readonly ReportRepository _reports = new ReportRepository();

        /// <summary>Dashboard figures are shown to every signed-in user.</summary>
        public DashboardMetrics Dashboard()
        {
            return _reports.Dashboard();
        }

        public List<Product> LowStock()
        {
            return _reports.LowStock();
        }

        public List<SalesDay> DailySales(User actor, DateTime from, DateTime to)
        {
            CheckRange(actor, from, to);
            return _reports.DailySales(from.Date, to.Date);
        }

        public List<TopProduct> TopProducts(User actor, DateTime from, DateTime to, int top)
        {
            CheckRange(actor, from, to);
            return _reports.TopProducts(from.Date, to.Date, top);
        }

        public List<CategorySales> SalesByCategory(User actor, DateTime from, DateTime to)
        {
            CheckRange(actor, from, to);
            return _reports.SalesByCategory(from.Date, to.Date);
        }

        private static void CheckRange(User actor, DateTime from, DateTime to)
        {
            Permissions.Require(Permissions.CanViewReports(actor), "Only managers and admins can view sales reports.");
            Permissions.Require(to >= from, "The end date must be on or after the start date.");
            Permissions.Require((to - from).TotalDays <= 366, "Choose a date range of one year or less.");
        }
    }

    public sealed class UserService
    {
        private readonly UserRepository _users = new UserRepository();

        public List<UserAccount> List(User actor)
        {
            Permissions.Require(Permissions.CanManageUsers(actor), "Only admins can manage users.");
            return _users.List();
        }

        public List<RoleInfo> Roles()
        {
            return _users.Roles();
        }

        public int Create(User actor, string username, string fullName, int roleId, string password)
        {
            Permissions.Require(Permissions.CanManageUsers(actor), "Only admins can manage users.");
            var name = Input.Required(username, 50, "Username");
            if (!Regex.IsMatch(name, "^[A-Za-z0-9._-]{3,50}$"))
                throw new BusinessRuleException("Username must be 3 to 50 letters, digits, '.', '_' or '-'.");
            Permissions.Require(roleId > 0, "Select a role.");
            ValidatePassword(password);
            return _users.Create(name, Input.Required(fullName, 100, "Full name"), roleId, password);
        }

        public void Update(User actor, UserAccount account)
        {
            Permissions.Require(Permissions.CanManageUsers(actor), "Only admins can manage users.");
            Permissions.Require(!(account.Id == actor.Id && !account.IsActive),
                                "You cannot deactivate your own account.");
            Permissions.Require(!(account.Id == actor.Id && account.RoleId != (int)actor.Role),
                                "You cannot change your own role.");
            _users.Update(account.Id, Input.Required(account.FullName, 100, "Full name"), account.RoleId, account.IsActive);
        }

        public void ResetPassword(User actor, int userId, string newPassword)
        {
            Permissions.Require(Permissions.CanManageUsers(actor), "Only admins can manage users.");
            ValidatePassword(newPassword);
            _users.SetPassword(userId, newPassword);
        }

        private static void ValidatePassword(string password)
        {
            Permissions.Require(!string.IsNullOrEmpty(password) && password.Length >= 8,
                                "Password must be at least 8 characters.");
        }
    }

    /// <summary>Printing is behind an interface so checkout never depends on a specific printer.</summary>
    public interface IReceiptPrinter
    {
        void Print(ReceiptData receipt);
        void Preview(ReceiptData receipt);
    }

    /// <summary>Adapter for future serial or camera scanners. USB keyboard scanners work through the Enter key.</summary>
    public interface IBarcodeScanner
    {
        event EventHandler<string> BarcodeScanned;
        void Start();
        void Stop();
    }
}
