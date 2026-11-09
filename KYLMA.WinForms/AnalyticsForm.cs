using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using KYLMA.Domain;
using KYLMA.Services;

namespace KYLMA.WinForms
{
    /// <summary>Sales by day, top products and sales by category for a date range. Managers and admins only.</summary>
    public sealed class AnalyticsForm : Form
    {
        private readonly User actor;
        private readonly ReportingService reporting = new ReportingService();
        private readonly DateTimePicker dtFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30), Width = 130 };
        private readonly DateTimePicker dtTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today, Width = 130 };
        private readonly Label lblSummary = new Label { AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        private readonly DataGridView gridDaily = NewGrid();
        private readonly DataGridView gridTop = NewGrid();
        private readonly DataGridView gridCategory = NewGrid();
        private System.Collections.Generic.List<SalesDay> daily = new System.Collections.Generic.List<SalesDay>();

        public AnalyticsForm(User user)
        {
            actor = user;
            Text = "Sales Analytics";
            Width = 960;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;

            var top = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            top.Controls.Add(new Label { Text = "From", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            top.Controls.Add(dtFrom);
            top.Controls.Add(new Label { Text = "To", AutoSize = true, Padding = new Padding(8, 6, 0, 0) });
            top.Controls.Add(dtTo);
            var refresh = new Button { Text = "Refresh", AutoSize = true };
            refresh.Click += (s, e) => Refresh_Data();
            var export = new Button { Text = "Export daily sales (CSV)", AutoSize = true };
            export.Click += (s, e) => ExportCsv();
            top.Controls.Add(refresh);
            top.Controls.Add(export);

            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(Page("Sales by day", gridDaily));
            tabs.TabPages.Add(Page("Top products", gridTop));
            tabs.TabPages.Add(Page("By category", gridCategory));

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(top, 0, 0);
            root.Controls.Add(lblSummary, 0, 1);
            root.Controls.Add(tabs, 0, 2);
            Controls.Add(root);

            Load += (s, e) => Refresh_Data();
        }

        private static DataGridView NewGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None
            };
        }

        private static TabPage Page(string title, Control content)
        {
            var page = new TabPage(title) { Padding = new Padding(6) };
            page.Controls.Add(content);
            return page;
        }

        private void Refresh_Data()
        {
            try
            {
                var from = dtFrom.Value.Date;
                var to = dtTo.Value.Date;

                daily = reporting.DailySales(actor, from, to);
                gridDaily.DataSource = daily
                    .Select(d => new { Date = d.Date.ToString("yyyy-MM-dd"), Orders = d.OrderCount, Revenue = d.Revenue })
                    .ToList();
                if (gridDaily.Columns.Contains("Revenue")) gridDaily.Columns["Revenue"].DefaultCellStyle.Format = "N2";

                gridTop.DataSource = reporting.TopProducts(actor, from, to, 10)
                    .Select(t => new { Product = t.ProductName, Qty = t.QtySold, Revenue = t.Revenue })
                    .ToList();
                if (gridTop.Columns.Contains("Revenue")) gridTop.Columns["Revenue"].DefaultCellStyle.Format = "N2";

                gridCategory.DataSource = reporting.SalesByCategory(actor, from, to)
                    .Select(c => new { Category = c.CategoryName, Revenue = c.Revenue })
                    .ToList();
                if (gridCategory.Columns.Contains("Revenue")) gridCategory.Columns["Revenue"].DefaultCellStyle.Format = "N2";

                var orders = daily.Sum(d => d.OrderCount);
                var revenue = daily.Sum(d => d.Revenue);
                lblSummary.Text = "Orders: " + orders + "     Revenue (incl. tax): " + revenue.ToString("C2");
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Sales analytics", ex);
            }
        }

        private void ExportCsv()
        {
            if (daily.Count == 0) { UiHelper.Info(this, "There is no data to export for this range.", "Export"); return; }

            using (var dialog = new SaveFileDialog { Filter = "CSV file (*.csv)|*.csv", FileName = "daily-sales.csv" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("Date,Orders,Revenue");
                    foreach (var d in daily)
                    {
                        csv.AppendLine(d.Date.ToString("yyyy-MM-dd") + "," + d.OrderCount + ","
                                       + d.Revenue.ToString("0.00", CultureInfo.InvariantCulture));
                    }
                    File.WriteAllText(dialog.FileName, csv.ToString(), Encoding.UTF8);
                    UiHelper.Info(this, "Saved " + daily.Count + " day(s) to " + dialog.FileName, "Export");
                }
                catch (Exception ex)
                {
                    UiHelper.ShowError(this, "Export", ex);
                }
            }
        }
    }
}
