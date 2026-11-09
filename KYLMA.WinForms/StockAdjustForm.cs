using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KYLMA.Domain;
using KYLMA.Services;

namespace KYLMA.WinForms
{
    /// <summary>Records a stock change with a reason and shows the movement history. Negative stock is blocked by SQL Server.</summary>
    public sealed class StockAdjustForm : Form
    {
        private readonly User actor;
        private readonly Product product;
        private readonly InventoryService inventory = new InventoryService();
        private readonly Label lblInfo = new Label { AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        private readonly NumericUpDown numChange = new NumericUpDown { Minimum = -100000, Maximum = 100000, Value = 0, Width = 120 };
        private readonly TextBox txtReason = new TextBox { Width = 320, MaxLength = 200 };
        private readonly DataGridView history = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None
        };

        public StockAdjustForm(User user, Product selected)
        {
            actor = user;
            product = selected;
            Text = "Adjust stock";
            Width = 720;
            Height = 560;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;

            var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = 6 };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            table.Controls.Add(lblInfo, 0, 0);
            table.SetColumnSpan(lblInfo, 2);

            table.Controls.Add(new Label { Text = "Change (+ add, - remove)", AutoSize = true }, 0, 1);
            table.Controls.Add(numChange, 1, 1);

            table.Controls.Add(new Label { Text = "Reason", AutoSize = true }, 0, 2);
            table.Controls.Add(txtReason, 1, 2);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            var apply = new Button { Text = "Apply adjustment", AutoSize = true };
            apply.Click += (s, e) => Apply();
            var close = new Button { Text = "Close", AutoSize = true, DialogResult = DialogResult.OK };
            buttons.Controls.Add(apply);
            buttons.Controls.Add(close);
            table.Controls.Add(buttons, 0, 3);
            table.SetColumnSpan(buttons, 2);

            table.Controls.Add(new Label { Text = "Movement history (newest first)", AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) }, 0, 4);
            table.SetColumnSpan(table.GetControlFromPosition(0, 4), 2);
            table.Controls.Add(history, 0, 5);
            table.SetColumnSpan(history, 2);

            AcceptButton = apply;
            CancelButton = close;
            Controls.Add(table);

            ShowInfo();
            Load += (s, e) => LoadHistory();
        }

        private void ShowInfo()
        {
            lblInfo.Text = product.Name + "  (" + product.Sku + ")   On hand: " + product.StockQuantity
                         + "   Reorder level: " + product.ReorderLevel;
        }

        private void Apply()
        {
            try
            {
                int newStock = inventory.Adjust(actor, product.Id, (int)numChange.Value, txtReason.Text);
                product.StockQuantity = newStock;
                numChange.Value = 0;
                txtReason.Clear();
                ShowInfo();
                LoadHistory();
                UiHelper.Info(this, "Stock updated. New on-hand quantity: " + newStock, "Stock adjustment");
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Stock adjustment", ex);
            }
        }

        private void LoadHistory()
        {
            try
            {
                history.DataSource = inventory.History(product.Id)
                    .Select(m => new { When = m.CreatedAt, Change = m.ChangeQty, m.Reason, By = m.Username })
                    .ToList();
                if (history.Columns.Contains("When")) history.Columns["When"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "History", ex);
            }
        }
    }
}
