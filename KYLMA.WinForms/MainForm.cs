using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KYLMA.Domain;
using KYLMA.Services;

namespace KYLMA.WinForms
{
    public sealed class MainForm : Form
    {
        private static readonly Color Navy = Color.FromArgb(20, 40, 90);
        private static readonly Color Surface = Color.FromArgb(245, 247, 250);

        private readonly User currentUser;
        private readonly Panel sidebar = new Panel();
        private readonly Label lblSalesVal = new Label();
        private readonly Label lblOrdersVal = new Label();
        private readonly Label lblLowStockVal = new Label();
        private readonly Label lblActiveProdVal = new Label();
        private readonly DataGridView gridLowStock = new DataGridView();

        public MainForm(User user)
        {
            currentUser = user;
            Text = "Enterprise Store - POS & Inventory | " + user.DisplayName;
            Width = 1200;
            Height = 700;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Surface;
            KeyPreview = true;
            KeyDown += (s, e) => { if (e.KeyCode == Keys.F2) OpenPos(); };

            var content = BuildContent();
            BuildSidebar();

            Controls.Add(content);
            Controls.Add(sidebar);
            sidebar.SendToBack();     // sidebar docks first, content fills the rest
            content.BringToFront();

            Load += (s, e) => LoadDashboard();
        }

        private void BuildSidebar()
        {
            sidebar.Dock = DockStyle.Left;
            sidebar.Width = 240;
            sidebar.BackColor = Navy;

            var logo = new Label
            {
                Text = "ENTERPRISE POS",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 80,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 90, BackColor = Color.FromArgb(15, 30, 70) };
            footer.Controls.Add(new Label
            {
                Text = currentUser.DisplayName + "\n" + currentUser.Role,
                ForeColor = Color.WhiteSmoke,
                Font = new Font("Segoe UI", 9),
                Location = new Point(15, 12),
                AutoSize = true
            });
            var logout = new LinkLabel
            {
                Text = "Log out",
                LinkColor = Color.LightSkyBlue,
                Location = new Point(15, 56),
                AutoSize = true
            };
            logout.LinkClicked += (s, e) => Close();
            footer.Controls.Add(logout);

            sidebar.Controls.Add(footer);
            sidebar.Controls.Add(logo);

            // Navigation: each item is shown only if the role allows it. Services enforce the same rules.
            int y = 90;
            AddNav("Dashboard", ref y, (s, e) => LoadDashboard());
            AddNav("Point of Sale  (F2)", ref y, (s, e) => OpenPos());
            if (Permissions.CanManageInventory(currentUser))
                AddNav("Inventory", ref y, (s, e) => OpenForm(new ProductForm(currentUser)));
            AddNav("Customers", ref y, (s, e) => OpenForm(new CustomersForm(currentUser)));
            AddNav("Reprint Receipt", ref y, (s, e) => OpenForm(new ReceiptHistoryForm(currentUser)));
            if (Permissions.CanViewReports(currentUser))
                AddNav("Sales Analytics", ref y, (s, e) => OpenForm(new AnalyticsForm(currentUser)));
            if (Permissions.CanManageUsers(currentUser))
                AddNav("Users & Roles", ref y, (s, e) => OpenForm(new UserAdminForm(currentUser)));
        }

        private void AddNav(string text, ref int y, EventHandler onClick)
        {
            var button = new Button
            {
                Text = text,
                Location = new Point(0, y),
                Size = new Size(240, 50),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Navy,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 65, 130);
            button.Click += onClick;
            sidebar.Controls.Add(button);
            y += 50;
        }

        private Control BuildContent()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(40),
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Surface
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            table.Controls.Add(new Label
            {
                Text = "Welcome, " + currentUser.DisplayName,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                AutoSize = true
            }, 0, 0);

            var cards = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            cards.Controls.Add(MetricCard("Today's sales", lblSalesVal, Color.FromArgb(0, 150, 136)));
            cards.Controls.Add(MetricCard("Orders today", lblOrdersVal, Color.FromArgb(41, 128, 185)));
            cards.Controls.Add(MetricCard("Low-stock items", lblLowStockVal, Color.FromArgb(192, 57, 43)));
            cards.Controls.Add(MetricCard("Active products", lblActiveProdVal, Color.FromArgb(142, 68, 173)));
            table.Controls.Add(cards, 0, 1);

            table.Controls.Add(new Label
            {
                Text = "Low-stock items requiring attention",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                AutoSize = true,
                Padding = new Padding(0, 10, 0, 0)
            }, 0, 2);

            gridLowStock.Dock = DockStyle.Fill;
            gridLowStock.ReadOnly = true;
            gridLowStock.AllowUserToAddRows = false;
            gridLowStock.AllowUserToDeleteRows = false;
            gridLowStock.RowHeadersVisible = false;
            gridLowStock.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridLowStock.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridLowStock.BackgroundColor = Color.White;
            gridLowStock.BorderStyle = BorderStyle.None;
            table.Controls.Add(gridLowStock, 0, 3);

            return table;
        }

        private static Panel MetricCard(string title, Label value, Color accent)
        {
            var card = new Panel { Width = 200, Height = 100, BackColor = Color.White, Margin = new Padding(0, 0, 20, 0) };

            card.Controls.Add(new Panel { Dock = DockStyle.Left, Width = 6, BackColor = accent });
            card.Controls.Add(new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.Gray,
                Location = new Point(20, 15),
                AutoSize = true
            });

            value.Text = "0";
            value.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            value.ForeColor = Color.FromArgb(40, 40, 40);
            value.Location = new Point(20, 45);
            value.AutoSize = true;
            card.Controls.Add(value);
            return card;
        }

        private void OpenPos()
        {
            OpenForm(new CheckoutForm(currentUser));
        }

        /// <summary>Shows a child window, then refreshes the dashboard so new sales and stock changes appear.</summary>
        private void OpenForm(Form form)
        {
            using (form)
            {
                form.ShowDialog(this);
            }
            LoadDashboard();
        }

        private void LoadDashboard()
        {
            try
            {
                var reporting = new ReportingService();
                var metrics = reporting.Dashboard();
                lblSalesVal.Text = metrics.TodaySales.ToString("C2");
                lblOrdersVal.Text = metrics.TodayOrders.ToString();
                lblLowStockVal.Text = metrics.LowStockCount.ToString();
                lblActiveProdVal.Text = metrics.ActiveProducts.ToString();

                gridLowStock.DataSource = reporting.LowStock()
                    .Select(p => new { p.Id, p.Sku, p.Name, Stock = p.StockQuantity, Reorder = p.ReorderLevel })
                    .ToList();
                if (gridLowStock.Columns.Contains("Id")) gridLowStock.Columns["Id"].Visible = false;
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Dashboard", ex);
            }
        }
    }
}
