using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KYLMA.Domain;
using KYLMA.Services;

namespace KYLMA.WinForms
{
    /// <summary>Customer list with search, and a form to add new customers.</summary>
    public sealed class CustomersForm : Form
    {
        private readonly CustomerService customers = new CustomerService();
        private readonly TextBox txtSearch = new TextBox { Width = 240 };
        private readonly DataGridView grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None
        };
        private readonly TextBox txtName = new TextBox { Width = 230, MaxLength = 100 };
        private readonly TextBox txtPhone = new TextBox { Width = 230, MaxLength = 30 };
        private readonly TextBox txtEmail = new TextBox { Width = 230, MaxLength = 120 };
        private readonly TextBox txtAddress = new TextBox { Width = 230, MaxLength = 250 };

        public CustomersForm(User user)
        {
            Text = "Customers";
            Width = 960;
            Height = 520;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;

            var search = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            search.Controls.Add(new Label { Text = "Name or phone", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            search.Controls.Add(txtSearch);
            var btnSearch = new Button { Text = "Search", AutoSize = true };
            btnSearch.Click += (s, e) => LoadRows();
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadRows(); } };
            search.Controls.Add(btnSearch);

            var addPanel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 7 };
            addPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            addPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            addPanel.Controls.Add(new Label { Text = "Add customer", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 0);
            addPanel.SetColumnSpan(addPanel.GetControlFromPosition(0, 0), 2);
            AddField(addPanel, "Name", txtName, 1);
            AddField(addPanel, "Phone", txtPhone, 2);
            AddField(addPanel, "Email", txtEmail, 3);
            AddField(addPanel, "Address", txtAddress, 4);
            var add = new Button { Text = "Save customer", AutoSize = true };
            add.Click += (s, e) => AddCustomer();
            addPanel.Controls.Add(add, 1, 5);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(search, 0, 0);
            root.SetColumnSpan(search, 2);
            root.Controls.Add(grid, 0, 1);
            root.Controls.Add(addPanel, 1, 1);
            Controls.Add(root);

            Load += (s, e) => LoadRows();
        }

        private static void AddField(TableLayoutPanel table, string caption, Control control, int row)
        {
            table.Controls.Add(new Label { Text = caption, AutoSize = true }, 0, row);
            table.Controls.Add(control, 1, row);
        }

        private void LoadRows()
        {
            try
            {
                grid.DataSource = customers.Search(txtSearch.Text)
                    .Select(c => new { c.Id, Name = c.FullName, c.Phone, c.Email, c.Address })
                    .ToList();
                if (grid.Columns.Contains("Id")) grid.Columns["Id"].Visible = false;
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Customers", ex);
            }
        }

        private void AddCustomer()
        {
            try
            {
                customers.Add(new Customer
                {
                    FullName = txtName.Text,
                    Phone = txtPhone.Text,
                    Email = txtEmail.Text,
                    Address = txtAddress.Text
                });
                txtName.Clear();
                txtPhone.Clear();
                txtEmail.Clear();
                txtAddress.Clear();
                LoadRows();
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Add customer", ex);
            }
        }
    }
}
