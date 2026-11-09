using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KYLMA.Domain;
using KYLMA.Services;

namespace KYLMA.WinForms
{
    /// <summary>Lists product categories and adds new ones.</summary>
    public sealed class CategoriesForm : Form
    {
        private readonly User actor;
        private readonly InventoryService inventory = new InventoryService();
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
        private readonly TextBox txtDescription = new TextBox { Width = 230, MaxLength = 200 };

        public CategoriesForm(User user)
        {
            actor = user;
            Text = "Categories";
            Width = 760;
            Height = 460;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;

            var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 1 };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            table.Controls.Add(grid, 0, 0);

            var form = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(8, 0, 0, 0) };
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            form.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            form.Controls.Add(new Label { Text = "New category", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 0);
            form.SetColumnSpan(form.GetControlFromPosition(0, 0), 2);
            form.Controls.Add(new Label { Text = "Name", AutoSize = true }, 0, 1);
            form.Controls.Add(txtName, 1, 1);
            form.Controls.Add(new Label { Text = "Description", AutoSize = true }, 0, 2);
            form.Controls.Add(txtDescription, 1, 2);
            var add = new Button { Text = "Add category", AutoSize = true };
            add.Click += (s, e) => AddCategory();
            form.Controls.Add(add, 1, 3);
            table.Controls.Add(form, 1, 0);

            Controls.Add(table);
            Load += (s, e) => LoadRows();
        }

        private void LoadRows()
        {
            try
            {
                grid.DataSource = inventory.Categories()
                    .Select(c => new { c.Id, c.Name, c.Description })
                    .ToList();
                if (grid.Columns.Contains("Id")) grid.Columns["Id"].Visible = false;
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Categories", ex);
            }
        }

        private void AddCategory()
        {
            try
            {
                inventory.AddCategory(actor, txtName.Text, txtDescription.Text);
                txtName.Clear();
                txtDescription.Clear();
                LoadRows();
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Add category", ex);
            }
        }
    }
}
