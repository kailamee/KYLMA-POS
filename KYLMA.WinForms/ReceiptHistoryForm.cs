using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KYLMA.Domain;
using KYLMA.Services;

namespace KYLMA.WinForms
{
    /// <summary>Finds a committed order by number and reprints its receipt. Read-only: it never creates a sale.</summary>
    public sealed class ReceiptHistoryForm : Form
    {
        private readonly ReceiptService receipts = new ReceiptService(StoreSettings.Profile());
        private readonly IReceiptPrinter printer = new ReceiptPrinter();
        private readonly TextBox txtNumber = new TextBox { Width = 260 };
        private readonly Label lblInfo = new Label { AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
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
        private readonly Button btnPreview = new Button { Text = "Preview", AutoSize = true, Enabled = false };
        private readonly Button btnPrint = new Button { Text = "Print", AutoSize = true, Enabled = false };
        private ReceiptData current;

        public ReceiptHistoryForm(User user)
        {
            Text = "Reprint Receipt";
            Width = 760;
            Height = 520;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;

            var find = new Button { Text = "Find", AutoSize = true };
            find.Click += (s, e) => Find();
            txtNumber.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Find(); } };
            btnPreview.Click += (s, e) => printer.Preview(current);
            btnPrint.Click += (s, e) => printer.Print(current);

            var top = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            top.Controls.Add(new Label { Text = "Order number", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            top.Controls.Add(txtNumber);
            top.Controls.Add(find);

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
            bottom.Controls.Add(btnPrint);
            bottom.Controls.Add(btnPreview);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            root.Controls.Add(top, 0, 0);
            root.Controls.Add(lblInfo, 0, 1);
            root.Controls.Add(grid, 0, 2);
            root.Controls.Add(bottom, 0, 3);
            Controls.Add(root);
        }

        private void Find()
        {
            try
            {
                current = receipts.ForOrderNumber(txtNumber.Text);
                lblInfo.Text = current.OrderNumber + "   " + current.Date.ToString("yyyy-MM-dd HH:mm")
                             + "   Total " + current.Total.ToString("C2") + "   " + current.PaymentMethod;
                grid.DataSource = current.Lines
                    .Select(l => new { Product = l.ProductName, Qty = l.Quantity, Amount = l.Amount })
                    .ToList();
                if (grid.Columns.Contains("Amount")) grid.Columns["Amount"].DefaultCellStyle.Format = "N2";
                btnPreview.Enabled = true;
                btnPrint.Enabled = true;
            }
            catch (Exception ex)
            {
                current = null;
                btnPreview.Enabled = false;
                btnPrint.Enabled = false;
                lblInfo.Text = "";
                grid.DataSource = null;
                UiHelper.ShowError(this, "Find receipt", ex);
            }
        }
    }
}
