using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KYLMA.Domain;
using KYLMA.Services;

namespace KYLMA.WinForms
{
    /// <summary>Inventory list with full CRUD: Create (Add), Read (grid and search), Update (Edit), Delete (deactivate).</summary>
    public sealed class ProductForm : Form
    {
        private readonly User actor;
        private readonly InventoryService inventory = new InventoryService();
        private readonly TextBox txtSearch = new TextBox { Width = 240 };
        private readonly CheckBox chkInactive = new CheckBox { Text = "Show inactive", AutoSize = true };
        private readonly DataGridView grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoGenerateColumns = true,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None
        };
        private List<Product> rows = new List<Product>();

        public ProductForm(User user)
        {
            actor = user;
            Text = "Inventory Management";
            Width = 1150;
            Height = 680;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;

            var commands = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(6, 8, 6, 0), WrapContents = false };
            commands.Controls.Add(new Label { Text = "SKU / barcode / name", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            commands.Controls.Add(txtSearch);
            commands.Controls.Add(chkInactive);
            commands.Controls.Add(MakeButton("Search", (s, e) => LoadRows()));
            commands.Controls.Add(MakeButton("Add product", (s, e) => AddProduct()));
            commands.Controls.Add(MakeButton("Edit", (s, e) => EditSelected()));
            commands.Controls.Add(MakeButton("Adjust stock", (s, e) => AdjustSelected()));
            commands.Controls.Add(MakeButton("Delete", (s, e) => DeleteSelected()));
            commands.Controls.Add(MakeButton("Categories", (s, e) => OpenCategories()));

            var legend = new Label
            {
                Text = "Rows in red are at or below their reorder level. Inactive products are hidden from sales.",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            root.Controls.Add(commands, 0, 0);
            root.Controls.Add(grid, 0, 1);
            root.Controls.Add(legend, 0, 2);
            Controls.Add(root);

            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadRows(); }
            };
            Load += (s, e) => LoadRows();
        }

        private static Button MakeButton(string text, EventHandler onClick)
        {
            var button = new Button { Text = text, AutoSize = true, Padding = new Padding(4, 0, 4, 0) };
            button.Click += onClick;
            return button;
        }

        private void LoadRows()
        {
            try
            {
                rows = inventory.Search(txtSearch.Text, chkInactive.Checked);
                grid.DataSource = rows.Select(p => new
                {
                    p.Id,
                    p.Sku,
                    p.Barcode,
                    p.Name,
                    Category = p.CategoryName,
                    Cost = p.CostPrice,
                    Price = p.SellingPrice,
                    Stock = p.StockQuantity,
                    Reorder = p.ReorderLevel,
                    Status = p.IsActive ? "Active" : "Inactive",
                    Alert = p.IsLowStock ? "LOW" : ""
                }).ToList();
                FormatGrid();
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Inventory", ex);
            }
        }

        private void FormatGrid()
        {
            if (grid.Columns.Contains("Id")) grid.Columns["Id"].Visible = false;
            SetFormat("Cost", "N2");
            SetFormat("Price", "N2");

            // grid row order matches rows, because the grid is bound to the same list
            foreach (DataGridViewRow row in grid.Rows)
            {
                var product = rows[row.Index];
                if (product.IsLowStock) row.DefaultCellStyle.BackColor = Color.MistyRose;
                if (!product.IsActive) row.DefaultCellStyle.ForeColor = Color.Gray;
            }
        }

        private void SetFormat(string column, string format)
        {
            if (grid.Columns.Contains(column)) grid.Columns[column].DefaultCellStyle.Format = format;
        }

        private Product SelectedProduct()
        {
            if (grid.CurrentRow == null || grid.CurrentRow.Index >= rows.Count) return null;
            return rows[grid.CurrentRow.Index];
        }

        private void AddProduct()
        {
            using (var editor = new ProductEditorForm(actor, null))
            {
                if (editor.ShowDialog(this) == DialogResult.OK) LoadRows();
            }
        }

        private void EditSelected()
        {
            var product = SelectedProduct();
            if (product == null) { UiHelper.Info(this, "Select a product first.", "Inventory"); return; }

            using (var editor = new ProductEditorForm(actor, product))
            {
                if (editor.ShowDialog(this) == DialogResult.OK) LoadRows();
            }
        }

        private void AdjustSelected()
        {
            var product = SelectedProduct();
            if (product == null) { UiHelper.Info(this, "Select a product first.", "Inventory"); return; }

            using (var form = new StockAdjustForm(actor, product))
            {
                form.ShowDialog(this);
            }
            LoadRows();
        }

        private void DeleteSelected()
        {
            var product = SelectedProduct();
            if (product == null) { UiHelper.Info(this, "Select a product first.", "Inventory"); return; }

            var answer = MessageBox.Show(this,
                "Delete \"" + product.Name + "\"? It will be hidden from sales, but its sales and stock history are kept.",
                "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;

            try
            {
                inventory.Delete(actor, product.Id);
                LoadRows();
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Delete product", ex);
            }
        }

        private void OpenCategories()
        {
            using (var form = new CategoriesForm(actor))
            {
                form.ShowDialog(this);
            }
        }
    }
}
