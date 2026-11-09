using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using KYLMA.Domain;
using KYLMA.Services;

namespace KYLMA.WinForms
{
    /// <summary>
    /// 80 mm thermal receipt on a PrintDocument, with a print preview.
    /// Paper width is 315 hundredths of an inch (about 80 mm). Check the output on your printer.
    /// </summary>
    public sealed class ReceiptPrinter : IReceiptPrinter
    {
        private enum Align { Left, Center, Right }

        private static readonly Font Normal = new Font("Consolas", 8.5f);
        private static readonly Font Bold = new Font("Consolas", 8.5f, FontStyle.Bold);
        private const int NameWidth = 23;

        private readonly PrintDocument document = new PrintDocument();
        private ReceiptData receipt;

        public ReceiptPrinter()
        {
            document.DocumentName = "Enterprise POS Receipt";
            document.DefaultPageSettings.Margins = new Margins(12, 12, 12, 12);
            document.PrintPage += PrintPage;
        }

        public void Print(ReceiptData data)
        {
            Prepare(data);
            using (var dialog = new PrintDialog { Document = document, AllowSomePages = false })
            {
                if (dialog.ShowDialog() == DialogResult.OK) document.Print();
            }
        }

        public void Preview(ReceiptData data)
        {
            Prepare(data);
            using (var preview = new PrintPreviewDialog { Document = document, Width = 900, Height = 700 })
            {
                preview.ShowDialog();
            }
        }

        private void Prepare(ReceiptData data)
        {
            if (data == null) throw new ArgumentNullException("data");
            receipt = data;
            // Enough height for every line plus the header, totals and footer.
            int rows = receipt.Lines.Count + 30;
            document.DefaultPageSettings.PaperSize = new PaperSize("Receipt", 315, 400 + rows * 25);
        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            var g = e.Graphics;
            var left = e.MarginBounds.Left;
            var width = e.MarginBounds.Width;
            var y = e.MarginBounds.Top;
            var rule = new string('-', 40);

            Draw(g, receipt.BusinessName, Bold, ref y, left, width, Align.Center);
            foreach (var line in Wrap(receipt.Address, 40)) Draw(g, line, Normal, ref y, left, width, Align.Center);
            if (!string.IsNullOrWhiteSpace(receipt.Phone)) Draw(g, "Tel: " + receipt.Phone, Normal, ref y, left, width, Align.Center);

            Draw(g, rule, Normal, ref y, left, width, Align.Left);
            Draw(g, "Order:   " + receipt.OrderNumber, Normal, ref y, left, width, Align.Left);
            Draw(g, "Date:    " + receipt.Date.ToString("yyyy-MM-dd HH:mm"), Normal, ref y, left, width, Align.Left);
            Draw(g, "Cashier: " + receipt.CashierName, Normal, ref y, left, width, Align.Left);
            if (!string.IsNullOrWhiteSpace(receipt.CustomerName) && receipt.CustomerName != "Walk-in customer")
                Draw(g, "Customer: " + receipt.CustomerName, Normal, ref y, left, width, Align.Left);

            Draw(g, rule, Normal, ref y, left, width, Align.Left);
            Draw(g, "Item                    Qty      Amount", Bold, ref y, left, width, Align.Left);
            foreach (var line in receipt.Lines)
            {
                var name = line.ProductName ?? "";
                if (name.Length > NameWidth) name = name.Substring(0, NameWidth - 2) + "..";
                Draw(g, string.Format("{0,-" + NameWidth + "} {1,3} {2,10}", name, line.Quantity, line.Amount.ToString("N2")),
                     Normal, ref y, left, width, Align.Left);
            }

            Draw(g, rule, Normal, ref y, left, width, Align.Left);
            Draw(g, "Subtotal: " + receipt.Subtotal.ToString("C2"), Normal, ref y, left, width, Align.Right);
            Draw(g, "Tax:      " + receipt.Tax.ToString("C2"), Normal, ref y, left, width, Align.Right);
            Draw(g, "Discount: " + receipt.Discount.ToString("C2"), Normal, ref y, left, width, Align.Right);
            Draw(g, "TOTAL:    " + receipt.Total.ToString("C2"), Bold, ref y, left, width, Align.Right);

            Draw(g, rule, Normal, ref y, left, width, Align.Left);
            Draw(g, "Payment: " + receipt.PaymentMethod, Normal, ref y, left, width, Align.Left);
            Draw(g, "Tendered: " + receipt.AmountTendered.ToString("C2"), Normal, ref y, left, width, Align.Right);
            Draw(g, "Change:   " + receipt.Change.ToString("C2"), Normal, ref y, left, width, Align.Right);

            y += 12;
            Draw(g, "Thank you for your purchase!", Bold, ref y, left, width, Align.Center);
            foreach (var line in Wrap(receipt.ReturnPolicy, 40)) Draw(g, line, Normal, ref y, left, width, Align.Center);

            e.HasMorePages = false;
        }

        private static void Draw(Graphics g, string text, Font font, ref int y, int left, int width, Align align)
        {
            text = text ?? "";
            var size = g.MeasureString(text, font);
            float x = left;
            if (align == Align.Center) x = left + (width - size.Width) / 2f;
            else if (align == Align.Right) x = left + width - size.Width;

            g.DrawString(text, font, Brushes.Black, x, y);
            y += (int)font.GetHeight(g) + 2;
        }

        private static IEnumerable<string> Wrap(string text, int maxChars)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return result;

            var current = "";
            foreach (var word in text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (current.Length + word.Length + 1 > maxChars && current.Length > 0)
                {
                    result.Add(current);
                    current = "";
                }
                current = current.Length == 0 ? word : current + " " + word;
            }
            if (current.Length > 0) result.Add(current);
            return result;
        }
    }
}
