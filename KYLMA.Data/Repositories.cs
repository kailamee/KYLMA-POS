using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using KYLMA.Domain;

namespace KYLMA.Data
{
    public sealed class UserRepository
    {
        /// <summary>Returns the user when the password matches and the account is active; otherwise null.</summary>
        public User Login(string username, string password)
        {
            User user = null;
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_Login"))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read()
                        && Db.Bool(r, "IsActive")
                        && PasswordHasher.Verify(password, Db.Str(r, "Salt"), Db.Str(r, "PasswordHash")))
                    {
                        user = new User
                        {
                            Id = Db.Int(r, "UserId"),
                            Username = Db.Str(r, "Username"),
                            DisplayName = Db.Str(r, "FullName"),
                            Role = (UserRole)Db.Int(r, "RoleId"),
                            IsActive = true
                        };
                    }
                }
            }
            if (user != null) TouchLogin(user.Id);
            return user;
        }

        public void TouchLogin(int userId)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_User_TouchLogin"))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.ExecuteNonQuery();
            }
        }

        public List<UserAccount> List()
        {
            var list = new List<UserAccount>();
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_User_List"))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new UserAccount
                    {
                        Id = Db.Int(r, "UserId"),
                        Username = Db.Str(r, "Username"),
                        FullName = Db.Str(r, "FullName"),
                        RoleId = Db.Int(r, "RoleId"),
                        RoleName = Db.Str(r, "RoleName"),
                        IsActive = Db.Bool(r, "IsActive"),
                        LastLoginAt = Db.DateOrNull(r, "LastLoginAt"),
                        CreatedAt = Db.Date(r, "CreatedAt")
                    });
                }
            }
            return list;
        }

        public List<RoleInfo> Roles()
        {
            var list = new List<RoleInfo>();
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_Role_List"))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new RoleInfo
                    {
                        Id = Db.Int(r, "RoleId"),
                        Name = Db.Str(r, "RoleName"),
                        Description = Db.Str(r, "Description")
                    });
                }
            }
            return list;
        }

        public int Create(string username, string fullName, int roleId, string password)
        {
            var salt = PasswordHasher.NewSalt();
            var hash = PasswordHasher.Hash(salt, password);
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_User_Create"))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@PasswordHash", hash);
                cmd.Parameters.AddWithValue("@Salt", salt);
                cmd.Parameters.AddWithValue("@FullName", fullName);
                cmd.Parameters.AddWithValue("@RoleId", roleId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void Update(int userId, string fullName, int roleId, bool isActive)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_User_Update"))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@FullName", fullName);
                cmd.Parameters.AddWithValue("@RoleId", roleId);
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                cmd.ExecuteNonQuery();
            }
        }

        public void SetPassword(int userId, string password)
        {
            var salt = PasswordHasher.NewSalt();
            var hash = PasswordHasher.Hash(salt, password);
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_User_SetPassword"))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@PasswordHash", hash);
                cmd.Parameters.AddWithValue("@Salt", salt);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public sealed class ProductRepository
    {
        public List<Product> Search(string search, bool includeInactive)
        {
            var list = new List<Product>();
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_Product_Search"))
            {
                cmd.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? (object)DBNull.Value : search.Trim());
                cmd.Parameters.AddWithValue("@IncludeInactive", includeInactive);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) list.Add(Map(r));
                }
            }
            return list;
        }

        public Product GetById(int productId)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_Product_GetById"))
            {
                cmd.Parameters.AddWithValue("@ProductId", productId);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    var product = Map(r);
                    product.Image = r["Image"] == DBNull.Value ? null : (byte[])r["Image"];
                    return product;
                }
            }
        }

        public Product GetByCode(string code)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_Product_GetByCode"))
            {
                cmd.Parameters.AddWithValue("@Code", code);
                using (var r = cmd.ExecuteReader())
                {
                    return r.Read() ? Map(r) : null;
                }
            }
        }

        public int Insert(Product p, int userId)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_Product_Insert"))
            {
                AddCommon(cmd, p);
                cmd.Parameters.Add(Binary("@Image", p.Image));
                cmd.Parameters.AddWithValue("@StockQty", p.StockQuantity);
                cmd.Parameters.AddWithValue("@UserId", userId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void Update(Product p, bool changeImage)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_Product_Update"))
            {
                cmd.Parameters.AddWithValue("@ProductId", p.Id);
                AddCommon(cmd, p);
                cmd.Parameters.AddWithValue("@IsActive", p.IsActive);
                cmd.Parameters.Add(Binary("@Image", changeImage ? p.Image : null));
                cmd.Parameters.AddWithValue("@ChangeImage", changeImage);
                cmd.ExecuteNonQuery();
            }
        }

        public void SoftDelete(int productId)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_Product_Delete"))
            {
                cmd.Parameters.AddWithValue("@ProductId", productId);
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Applies a stock change through the transactional procedure and returns the new on-hand quantity.</summary>
        public int Adjust(int productId, int change, string reason, int userId)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_Inventory_Adjust"))
            {
                cmd.Parameters.AddWithValue("@ProductId", productId);
                cmd.Parameters.AddWithValue("@ChangeQty", change);
                cmd.Parameters.AddWithValue("@Reason", reason);
                cmd.Parameters.AddWithValue("@UserId", userId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public List<StockMovement> Movements(int productId)
        {
            var list = new List<StockMovement>();
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_InventoryLog_List"))
            {
                cmd.Parameters.AddWithValue("@ProductId", productId);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new StockMovement
                        {
                            ChangeQty = Db.Int(r, "ChangeQty"),
                            Reason = Db.Str(r, "Reason"),
                            Username = Db.Str(r, "Username"),
                            CreatedAt = Db.Date(r, "CreatedAt")
                        });
                    }
                }
            }
            return list;
        }

        private static void AddCommon(SqlCommand cmd, Product p)
        {
            cmd.Parameters.AddWithValue("@Sku", p.Sku);
            cmd.Parameters.AddWithValue("@Barcode", Db.Val(p.Barcode));
            cmd.Parameters.AddWithValue("@Name", p.Name);
            cmd.Parameters.AddWithValue("@CategoryId", p.CategoryId);
            cmd.Parameters.AddWithValue("@Price", p.SellingPrice);
            cmd.Parameters.AddWithValue("@Cost", p.CostPrice);
            cmd.Parameters.AddWithValue("@ReorderLevel", p.ReorderLevel);
        }

        private static SqlParameter Binary(string name, byte[] value)
        {
            return new SqlParameter(name, SqlDbType.VarBinary, -1) { Value = value == null ? (object)DBNull.Value : value };
        }

        private static Product Map(SqlDataReader r)
        {
            return new Product
            {
                Id = Db.Int(r, "ProductId"),
                Sku = Db.Str(r, "Sku"),
                Barcode = Db.Str(r, "Barcode"),
                Name = Db.Str(r, "Name"),
                CategoryId = Db.Int(r, "CategoryId"),
                CategoryName = Db.Str(r, "CategoryName"),
                SellingPrice = Db.Dec(r, "Price"),
                CostPrice = Db.Dec(r, "Cost"),
                StockQuantity = Db.Int(r, "StockQty"),
                ReorderLevel = Db.Int(r, "ReorderLevel"),
                IsActive = Db.Bool(r, "IsActive")
            };
        }
    }

    public sealed class CategoryRepository
    {
        public List<Category> List()
        {
            var list = new List<Category>();
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_Category_List"))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new Category { Id = Db.Int(r, "CategoryId"), Name = Db.Str(r, "Name"), Description = Db.Str(r, "Description") });
                }
            }
            return list;
        }

        public int Insert(string name, string description)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_Category_Insert"))
            {
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@Description", Db.Val(description));
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }

    public sealed class CustomerRepository
    {
        public List<Customer> Search(string search)
        {
            var list = new List<Customer>();
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_Customer_List"))
            {
                cmd.Parameters.AddWithValue("@Search", Db.Val(search));
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new Customer
                        {
                            Id = Db.Int(r, "CustomerId"),
                            FullName = Db.Str(r, "FullName"),
                            Phone = Db.Str(r, "Phone"),
                            Email = Db.Str(r, "Email"),
                            Address = Db.Str(r, "Address")
                        });
                    }
                }
            }
            return list;
        }

        public int Insert(Customer customer)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_Customer_Insert"))
            {
                cmd.Parameters.AddWithValue("@FullName", customer.FullName);
                cmd.Parameters.AddWithValue("@Phone", Db.Val(customer.Phone));
                cmd.Parameters.AddWithValue("@Email", Db.Val(customer.Email));
                cmd.Parameters.AddWithValue("@Address", Db.Val(customer.Address));
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }

    public sealed class OrderRepository
    {
        /// <summary>
        /// Calls dbo.usp_Order_Create, which runs the whole sale in one transaction and returns the new OrderId.
        /// Only product IDs and quantities are sent; SQL Server reads the prices itself.
        /// </summary>
        public int Create(int userId, int? customerId, string paymentMethod, decimal discount,
                          decimal taxRate, decimal amountPaid, IEnumerable<CartLine> lines)
        {
            var xml = new StringBuilder("<Items>");
            foreach (var line in lines)
            {
                xml.Append("<Item ProductId=\"").Append(line.ProductId)
                   .Append("\" Quantity=\"").Append(line.Quantity).Append("\"/>");
            }
            xml.Append("</Items>");

            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_Order_Create"))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@CustomerId", customerId.HasValue ? (object)customerId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@PaymentMethod", paymentMethod);
                cmd.Parameters.AddWithValue("@DiscountAmount", discount);
                cmd.Parameters.AddWithValue("@TaxRate", taxRate);
                cmd.Parameters.AddWithValue("@AmountPaid", amountPaid);
                cmd.Parameters.Add(new SqlParameter("@ItemsXml", SqlDbType.Xml) { Value = xml.ToString() });
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public OrderHeader Header(int? orderId, string orderNumber)
        {
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_Order_Header"))
            {
                cmd.Parameters.AddWithValue("@OrderId", orderId.HasValue ? (object)orderId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@OrderNumber", string.IsNullOrEmpty(orderNumber) ? (object)DBNull.Value : orderNumber);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return new OrderHeader
                    {
                        Id = Db.Int(r, "OrderId"),
                        OrderNumber = Db.Str(r, "OrderNumber"),
                        OrderDate = Db.Date(r, "OrderDate"),
                        Subtotal = Db.Dec(r, "Subtotal"),
                        TaxAmount = Db.Dec(r, "TaxAmount"),
                        DiscountAmount = Db.Dec(r, "DiscountAmount"),
                        Total = Db.Dec(r, "Total"),
                        PaymentMethod = Db.Str(r, "PaymentMethod"),
                        AmountPaid = Db.Dec(r, "AmountPaid"),
                        ChangeDue = Db.Dec(r, "ChangeDue"),
                        Status = Db.Str(r, "Status"),
                        CashierName = Db.Str(r, "CashierName"),
                        CustomerName = Db.Str(r, "CustomerName")
                    };
                }
            }
        }

        public List<OrderLine> Lines(int orderId)
        {
            var list = new List<OrderLine>();
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_Order_Lines"))
            {
                cmd.Parameters.AddWithValue("@OrderId", orderId);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new OrderLine
                        {
                            ProductName = Db.Str(r, "ProductName"),
                            Sku = Db.Str(r, "Sku"),
                            Quantity = Db.Int(r, "Quantity"),
                            UnitPrice = Db.Dec(r, "UnitPrice"),
                            LineTotal = Db.Dec(r, "LineTotal")
                        });
                    }
                }
            }
            return list;
        }
    }

    public sealed class ReportRepository
    {
        public DashboardMetrics Dashboard()
        {
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_Report_Dashboard"))
            using (var r = cmd.ExecuteReader())
            {
                if (!r.Read()) return new DashboardMetrics();
                return new DashboardMetrics
                {
                    TodaySales = Db.Dec(r, "TodaySales"),
                    TodayOrders = Db.Int(r, "TodayOrders"),
                    LowStockCount = Db.Int(r, "LowStockCount"),
                    ActiveProducts = Db.Int(r, "ActiveProducts")
                };
            }
        }

        public List<Product> LowStock()
        {
            var list = new List<Product>();
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_Report_LowStock"))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    list.Add(new Product
                    {
                        Id = Db.Int(r, "ProductId"),
                        Sku = Db.Str(r, "Sku"),
                        Name = Db.Str(r, "Name"),
                        StockQuantity = Db.Int(r, "StockQty"),
                        ReorderLevel = Db.Int(r, "ReorderLevel"),
                        IsActive = true
                    });
                }
            }
            return list;
        }

        public List<SalesDay> DailySales(DateTime from, DateTime to)
        {
            var list = new List<SalesDay>();
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_Report_DailySales"))
            {
                cmd.Parameters.AddWithValue("@From", from);
                cmd.Parameters.AddWithValue("@To", to);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new SalesDay
                        {
                            Date = Db.Date(r, "SaleDate"),
                            OrderCount = Db.Int(r, "OrderCount"),
                            Revenue = Db.Dec(r, "Revenue")
                        });
                    }
                }
            }
            return list;
        }

        public List<TopProduct> TopProducts(DateTime from, DateTime to, int top)
        {
            var list = new List<TopProduct>();
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_Report_TopProducts"))
            {
                cmd.Parameters.AddWithValue("@From", from);
                cmd.Parameters.AddWithValue("@To", to);
                cmd.Parameters.AddWithValue("@Top", top);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new TopProduct
                        {
                            ProductName = Db.Str(r, "ProductName"),
                            QtySold = Db.Int(r, "QtySold"),
                            Revenue = Db.Dec(r, "Revenue")
                        });
                    }
                }
            }
            return list;
        }

        public List<CategorySales> SalesByCategory(DateTime from, DateTime to)
        {
            var list = new List<CategorySales>();
            using (var c = Db.Open())
            using (var cmd = Db.Proc(c, "dbo.usp_Report_SalesByCategory"))
            {
                cmd.Parameters.AddWithValue("@From", from);
                cmd.Parameters.AddWithValue("@To", to);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new CategorySales
                        {
                            CategoryName = Db.Str(r, "CategoryName"),
                            Revenue = Db.Dec(r, "Revenue")
                        });
                    }
                }
            }
            return list;
        }
    }
}
