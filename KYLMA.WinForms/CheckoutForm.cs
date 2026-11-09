using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KYLMA.Domain;
using KYLMA.Services;

namespace KYLMA.WinForms
{
    /// <summary>
    /// Point of sale. Scan or type a SKU/barcode and press Enter (USB scanners send Enter).
    /// The total shown here is a preview; SQL Server recalculates everything at checkout.
    /// </summary>
    public sealed class CheckoutForm : Form
    {
        private readonly User cashier;
        private readonly InventoryService inventory = new InventoryService();
        private readonly CustomerService customers = new CustomerService();
        private readonly SalesService sales = new SalesService();
        private readonly ReceiptService receipts = new ReceiptService(StoreSettings.Profile());
        private readonly IReceiptPrinter printer = new ReceiptPrinter();
        private readonly decimal taxRate = StoreSettings.TaxRatePercent();
        private readonly List<CartLine> cart = new List<CartLine>();
        private ReceiptData lastReceipt;

        private readonly TextBox txtCode = new TextBox { Width = 240 };
        private readonly NumericUpDown numQty = new NumericUpDown { Minimum = 1, Maximum = 999, Value = 1, Width = 70 };
        private readonly ComboBox cboCustomer = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        private readonly TextBox txtDiscount = new TextBox { Width = 90, Text = "0.00", TextAlign = HorizontalAlignment.Right };
        private readonly ComboBox cboPayment = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
        private readonly TextBox txtTendered = new TextBox { Width = 110, Text = "0.00", TextAlign = HorizontalAlignment.Right };
        private readonly DataGridView grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None
        };
        private readonly Label lblSubtotal = new Label { AutoSize = true, Font = new Font("Segoe UI", 10) };
        private readonly Label lblTax = new Label { AutoSize = true, Font = new Font("Segoe UI", 10) };
        private readonly Label lblTotal = new Label { AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold) };
        private readonly Label lblChange = new Label { AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
        private readonly Button btnCheckout = new Button { Text = "Checkout", AutoSize = true, Padding = new Padding(10, 4, 10, 4) };
        private readonly Button btnPreview = new Button { Text = "Preview receipt", AutoSize = true, Enabled = false };
        private readonly Button btnPrint = new Button { Text = "Print receipt", AutoSize = true, Enabled = false };

        public CheckoutForm(User user)
        {
            cashier = user;
            Text = "Point of Sale - " + user.DisplayName;
            Width = 1100;
            Height = 720;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;

            cboPayment.Items.AddRange(new object[] { "Cash", "Card", "Mobile" });
            cboPayment.SelectedIndex = 0;
            cboPayment.SelectedIndexChanged += (s, e) => { txtTendered.Enabled = IsCash; Recalculate(); };
            txtDiscount.TextChanged += (s, e) => Recalculate();
            txtTendered.TextChanged += (s, e) => Recalculate();
            txtCode.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddItem(); }
            };

            btnCheckout.Click += (s, e) => Checkout();
            btnPreview.Click += (s, e) => PreviewReceipt();
            btnPrint.Click += (s, e) => PrintReceipt();

            Controls.Add(BuildLayout());
            Load += (s, e) => LoadCustomers();
            RefreshCart();
        }

        private bool IsCash
        {
            get { return cboPayment.SelectedItem != null && cboPayment.SelectedItem.ToString() == "Cash"; }
        }

        private Control BuildLayout()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(8) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));   // scan bar
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // cart
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));   // line actions
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));   // customer / payment
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));   // totals and buttons

            var scan = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            scan.Controls.Add(new Label { Text = "SKU / barcode", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            scan.Controls.Add(txtCode);
            scan.Controls.Add(new Label { Text = "Qty", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });
            scan.Controls.Add(numQty);
            var add = new Button { Text = "Add  (Enter)", AutoSize = true };
            add.Click += (s, e) => AddItem();
            scan.Controls.Add(add);

            var lineActions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            var updateQty = new Button { Text = "Set qty of selected line", AutoSize = true };
            updateQty.Click += (s, e) => UpdateSelectedQuantity();
            var remove = new Button { Text = "Remove selected line", AutoSize = true };
            remove.Click += (s, e) => RemoveSelectedLine();
            var clear = new Button { Text = "Clear cart", AutoSize = true };
            clear.Click += (s, e) => ClearCart();
            lineActions.Controls.Add(updateQty);
            lineActions.Controls.Add(remove);
            lineActions.Controls.Add(clear);

            var payment = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            payment.Controls.Add(new Label { Text = "Customer", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            payment.Controls.Add(cboCustomer);
            payment.Controls.Add(new Label { Text = "Discount", AutoSize = true, Padding = new Padding(12, 6, 0, 0) });
            payment.Controls.Add(txtDiscount);
            payment.Controls.Add(new Label { Text = "Payment", AutoSize = true, Padding = new Padding(12, 6, 0, 0) });
            payment.Controls.Add(cboPayment);
            payment.Controls.Add(new Label { Text = "Cash tendered", AutoSize = true, Padding = new Padding(12, 6, 0, 0) });
            payment.Controls.Add(txtTendered);

            var summary = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            var figures = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown };
            figures.Controls.Add(lblSubtotal);
            figures.Controls.Add(lblTax);
            figures.Controls.Add(lblTotal);
            figures.Controls.Add(lblChange);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            actions.Controls.Add(btnCheckout);
            actions.Controls.Add(btnPrint);
            actions.Controls.Add(btnPreview);
            summary.Controls.Add(figures, 0, 0);
            summary.Controls.Add(actions, 1, 0);

            root.Controls.Add(scan, 0, 0);
            root.Controls.Add(grid, 0, 1);
            root.Controls.Add(lineActions, 0, 2);
            root.Controls.Add(payment, 0, 3);
            root.Controls.Add(summary, 0, 4);
            return root;
        }

        private void LoadCustomers()
        {
            try
            {
                var list = new List<Customer> { new Customer { Id = 0, FullName = "Walk-in customer" } };
                list.AddRange(customers.Search(null));
                cboCustomer.DisplayMember = "FullName";
                cboCustomer.DataSource = list;
                cboCustomer.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Customers", ex);
            }
        }

        private void AddItem()
        {
            var code = txtCode.Text.Trim();
            if (code.Length == 0) return;

            try
            {
                var product = inventory.FindByCode(code);
                if (product == null) throw new BusinessRuleException("No active product matches \"" + code + "\".");

                int qty = (int)numQty.Value;
                var line = cart.FirstOrDefault(l => l.ProductId == product.Id);
                int newQty = (line == null ? 0 : line.Quantity) + qty;
                EnsureStock(product, newQty);

                if (line == null)
                {
                    cart.Add(new CartLine
                    {
                        ProductId = product.Id,
                        Sku = product.Sku,
                        ProductName = product.Name,
                        UnitPrice = product.SellingPrice,
                        Quantity = qty,
                        StockOnHand = product.StockQuantity
                    });
                }
                else
                {
                    line.Quantity = newQty;
                }

                txtCode.Clear();
                numQty.Value = 1;
                RefreshCart();
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Add item", ex);
            }
            txtCode.Focus();
        }

        private static void EnsureStock(Product product, int quantity)
        {
            if (quantity > 999) throw new BusinessRuleException("The maximum quantity per line is 999.");
            if (quantity > product.StockQuantity)
                throw new BusinessRuleException("Not enough stock for " + product.Name + ". Only " + product.StockQuantity + " available.");
        }

        private CartLine SelectedLine()
        {
            if (grid.CurrentRow == null || grid.CurrentRow.Index >= cart.Count) return null;
            return cart[grid.CurrentRow.Index];
        }

        private void UpdateSelectedQuantity()
        {
            var line = SelectedLine();
            if (line == null) { UiHelper.Info(this, "Select a line in the cart first.", "Cart"); return; }

            try
            {
                int qty = (int)numQty.Value;
                var product = inventory.FindByCode(line.Sku);
                if (product == null) throw new BusinessRuleException(line.ProductName + " is no longer available.");
                EnsureStock(product, qty);
                line.Quantity = qty;
                RefreshCart();
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Update quantity", ex);
            }
        }

        private void RemoveSelectedLine()
        {
            var line = SelectedLine();
            if (line == null) { UiHelper.Info(this, "Select a line in the cart first.", "Cart"); return; }
            cart.Remove(line);
            RefreshCart();
        }

        private void ClearCart()
        {
            if (cart.Count == 0) return;
            if (MessageBox.Show(this, "Remove all items from the cart?", "Clear cart",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            cart.Clear();
            RefreshCart();
        }

        private void RefreshCart()
        {
            int selected = grid.CurrentRow == null ? -1 : grid.CurrentRow.Index;

            grid.DataSource = null;
            grid.DataSource = cart.Select(l => new
            {
                l.ProductId,
                l.Sku,
                Product = l.ProductName,
                Qty = l.Quantity,
                Unit = l.UnitPrice,
                Total = l.LineTotal
            }).ToList();

            if (grid.Columns.Contains("ProductId")) grid.Columns["ProductId"].Visible = false;
            if (grid.Columns.Contains("Unit")) grid.Columns["Unit"].DefaultCellStyle.Format = "N2";
            if (grid.Columns.Contains("Total")) grid.Columns["Total"].DefaultCellStyle.Format = "N2";
            if (selected >= 0 && selected < grid.Rows.Count) grid.Rows[selected].Selected = true;

            Recalculate();
        }

        /// <summary>Updates the totals preview. Invalid discount or tender are ignored here and reported at checkout.</summary>
        private void Recalculate()
        {
            var subtotal = cart.Sum(l => l.LineTotal);

            decimal discount;
            if (!UiHelper.TryMoney(txtDiscount.Text, out discount) || discount > subtotal) discount = 0m;

            var totals = SalesService.Calculate(cart, discount, taxRate);
            lblSubtotal.Text = "Subtotal: " + totals.Subtotal.ToString("C2");
            lblTax.Text = "Tax (" + taxRate.ToString("0.##") + "%): " + totals.Tax.ToString("C2");
            lblTotal.Text = "TOTAL: " + totals.Total.ToString("C2");

            if (IsCash)
            {
                decimal tendered;
                if (UiHelper.TryMoney(txtTendered.Text, out tendered))
                {
                    var change = tendered - totals.Total;
                    lblChange.Text = change < 0 ? "Short by " + (-change).ToString("C2") : "Change: " + change.ToString("C2");
                    lblChange.ForeColor = change < 0 ? Color.Firebrick : Color.DarkGreen;
                }
                else
                {
                    lblChange.Text = "Enter the cash tendered";
                    lblChange.ForeColor = Color.Firebrick;
                }
            }
            else
            {
                lblChange.Text = "Card / mobile: exact amount";
                lblChange.ForeColor = Color.DimGray;
            }

            btnCheckout.Enabled = cart.Count > 0;
        }

        private void Checkout()
        {
            try
            {
                var discount = UiHelper.ParseMoney(txtDiscount.Text, "Discount");
                var method = cboPayment.SelectedItem.ToString();
                var totals = SalesService.Calculate(cart, discount, taxRate);
                decimal tendered = method == "Cash"
                    ? UiHelper.ParseMoney(txtTendered.Text, "Amount tendered")
                    : totals.Total;

                var customer = cboCustomer.SelectedItem as Customer;
                int? customerId = customer == null || customer.Id == 0 ? (int?)null : customer.Id;

                // The database is the source of truth: it re-checks stock and prices and commits atomically.
                int orderId = sales.Checkout(cashier, cart.ToList(), customerId, method, discount, taxRate, tendered);

                string receiptNote;
                try
                {
                    lastReceipt = receipts.ForOrderId(orderId);
                    btnPreview.Enabled = true;
                    btnPrint.Enabled = true;
                    receiptNote = "";
                }
                catch (Exception ex)
                {
                    // The sale is saved. Only the receipt failed, so do not create a second order.
                    AppLogger.LogException("Receipt load after checkout", ex);
                    lastReceipt = null;
                    btnPreview.Enabled = false;
                    btnPrint.Enabled = false;
                    receiptNote = "\n\nThe sale was saved, but the receipt could not be loaded. Use Reprint Receipt from the menu.";
                }

                var orderLabel = lastReceipt != null ? lastReceipt.OrderNumber : "#" + orderId;
                var change = tendered - totals.Total;
                var message = "Order " + orderLabel + " saved.\nTotal: " + totals.Total.ToString("C2")
                            + "\nPaid by " + method + ": " + tendered.ToString("C2")
                            + (method == "Cash" ? "\nChange: " + change.ToString("C2") : "")
                            + receiptNote;

                cart.Clear();
                txtDiscount.Text = "0.00";
                txtTendered.Text = "0.00";
                RefreshCart();
                UiHelper.Info(this, message, "Checkout complete");
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Checkout", ex);
            }
        }

        private void PreviewReceipt()
        {
            if (lastReceipt == null) { UiHelper.Info(this, "Complete a checkout first.", "Receipt"); return; }
            printer.Preview(lastReceipt);
        }

        private void PrintReceipt()
        {
            if (lastReceipt == null) { UiHelper.Info(this, "Complete a checkout first.", "Receipt"); return; }
            try
            {
                printer.Print(lastReceipt);
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Print receipt", ex);
            }
        }
    }
}
