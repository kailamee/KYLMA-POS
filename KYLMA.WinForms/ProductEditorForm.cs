using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using KYLMA.Domain;
using KYLMA.Services;

namespace KYLMA.WinForms
{
    /// <summary>Create or edit one product. Stock is set on creation only; later changes go through Adjust stock.</summary>
    public sealed class ProductEditorForm : Form
    {
        private const int MaxPixels = 2000;

        private readonly User actor;
        private readonly Product existing;
        private readonly InventoryService inventory = new InventoryService();

        private readonly TextBox txtSku = new TextBox { Width = 230, MaxLength = 30 };
        private readonly TextBox txtBarcode = new TextBox { Width = 230, MaxLength = 50 };
        private readonly TextBox txtName = new TextBox { Width = 230, MaxLength = 150 };
        private readonly ComboBox cboCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230 };
        private readonly TextBox txtCost = new TextBox { Width = 230 };
        private readonly TextBox txtPrice = new TextBox { Width = 230 };
        private readonly TextBox txtStock = new TextBox { Width = 230 };
        private readonly TextBox txtReorder = new TextBox { Width = 230 };
        private readonly CheckBox chkActive = new CheckBox { Text = "Active (can be sold)", AutoSize = true };
        private readonly PictureBox picPreview = new PictureBox
        {
            Width = 180,
            Height = 180,
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle
        };
        private readonly Label lblImage = new Label { AutoSize = true, Text = "No image", ForeColor = Color.DimGray };

        private byte[] newImage;
        private bool imageChanged;

        public ProductEditorForm(User user, Product product)
        {
            actor = user;
            existing = product;
            Text = product == null ? "Add product" : "Edit product";
            Width = 560;
            Height = 640;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 3, RowCount = 13 };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

            int row = 0;
            AddRow(table, "SKU", txtSku, ref row);
            AddRow(table, "Barcode", txtBarcode, ref row);
            AddRow(table, "Product name", txtName, ref row);
            AddRow(table, "Category", cboCategory, ref row);
            AddRow(table, "Cost price", txtCost, ref row);
            AddRow(table, "Selling price", txtPrice, ref row);
            AddRow(table, existing == null ? "Opening stock" : "Stock on hand", txtStock, ref row);
            AddRow(table, "Reorder level", txtReorder, ref row);

            table.Controls.Add(chkActive, 1, row);
            table.SetColumnSpan(chkActive, 2);
            row++;

            var browse = MakeButton("Browse...", (s, e) => BrowseImage());
            var remove = MakeButton("Remove image", (s, e) => ClearImage());
            table.Controls.Add(new Label { Text = "Image (JPG/PNG)", AutoSize = true }, 0, row);
            table.Controls.Add(lblImage, 1, row);
            table.Controls.Add(browse, 2, row);
            row++;
            table.Controls.Add(remove, 2, row);
            row++;

            table.Controls.Add(picPreview, 0, row);
            table.SetColumnSpan(picPreview, 3);
            row++;

            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true };
            var save = MakeButton("Save", Save);
            var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(save);
            table.Controls.Add(buttons, 0, row);
            table.SetColumnSpan(buttons, 3);

            AcceptButton = save;
            CancelButton = cancel;
            Controls.Add(table);

            if (existing != null)
            {
                txtSku.Text = existing.Sku;
                txtBarcode.Text = existing.Barcode;
                txtName.Text = existing.Name;
                txtCost.Text = existing.CostPrice.ToString("0.00");
                txtPrice.Text = existing.SellingPrice.ToString("0.00");
                txtStock.Text = existing.StockQuantity.ToString();
                txtStock.Enabled = false;
                txtReorder.Text = existing.ReorderLevel.ToString();
                chkActive.Checked = existing.IsActive;
            }
            else
            {
                txtCost.Text = "0.00";
                txtPrice.Text = "0.00";
                txtStock.Text = "0";
                txtReorder.Text = "5";
                chkActive.Checked = true;
                chkActive.Enabled = false;
            }

            Load += (s, e) => LoadLookups();
        }

        private static Button MakeButton(string text, EventHandler onClick)
        {
            var button = new Button { Text = text, AutoSize = true };
            button.Click += onClick;
            return button;
        }

        private static void AddRow(TableLayoutPanel table, string caption, Control control, ref int row)
        {
            table.Controls.Add(new Label { Text = caption, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            table.Controls.Add(control, 1, row);
            row++;
        }

        private void LoadLookups()
        {
            try
            {
                var categories = inventory.Categories();
                cboCategory.DisplayMember = "Name";
                cboCategory.DataSource = categories;

                if (existing != null)
                {
                    var match = categories.FirstOrDefault(c => c.Id == existing.CategoryId);
                    if (match != null) cboCategory.SelectedItem = match;

                    // The grid list does not include images, so load the full product for the preview.
                    var full = inventory.GetById(existing.Id);
                    if (full != null && full.Image != null) ShowPreview(LoadBitmap(full.Image));
                }
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Product", ex);
            }
        }

        private void BrowseImage()
        {
            using (var dialog = new OpenFileDialog
            {
                Filter = "Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                CheckFileExists = true,
                Title = "Choose a product image"
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    var bytes = File.ReadAllBytes(dialog.FileName);
                    ImageRules.Validate(bytes);

                    var bitmap = LoadBitmap(bytes);
                    if (bitmap.Width > MaxPixels || bitmap.Height > MaxPixels)
                    {
                        bitmap.Dispose();
                        throw new BusinessRuleException("Image must be " + MaxPixels + " pixels or less on each side.");
                    }

                    ShowPreview(bitmap);
                    newImage = bytes;
                    imageChanged = true;
                    lblImage.Text = Path.GetFileName(dialog.FileName);
                }
                catch (BusinessRuleException ex)
                {
                    UiHelper.ShowError(this, "Image", ex);
                }
                catch (ArgumentException)
                {
                    UiHelper.ShowError(this, "Image", new BusinessRuleException("That file is not a valid image."));
                }
                catch (Exception ex)
                {
                    UiHelper.ShowError(this, "Image", ex);
                }
            }
        }

        private void ClearImage()
        {
            newImage = null;
            imageChanged = true;
            ShowPreview(null);
            lblImage.Text = "Image will be removed when you save";
        }

        private void ShowPreview(Image image)
        {
            var old = picPreview.Image;
            picPreview.Image = image;
            if (old != null) old.Dispose();
        }

        /// <summary>Copies the image into a new bitmap so the source stream can be closed.</summary>
        private static Bitmap LoadBitmap(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            using (var image = Image.FromStream(stream))
            {
                return new Bitmap(image);
            }
        }

        private void Save(object sender, EventArgs e)
        {
            try
            {
                var category = cboCategory.SelectedItem as Category;
                var product = new Product
                {
                    Id = existing == null ? 0 : existing.Id,
                    Sku = txtSku.Text,
                    Barcode = txtBarcode.Text,
                    Name = txtName.Text,
                    CategoryId = category == null ? 0 : category.Id,
                    CostPrice = UiHelper.ParseMoney(txtCost.Text, "Cost price"),
                    SellingPrice = UiHelper.ParseMoney(txtPrice.Text, "Selling price"),
                    StockQuantity = existing == null ? UiHelper.ParseWhole(txtStock.Text, "Opening stock") : existing.StockQuantity,
                    ReorderLevel = UiHelper.ParseWhole(txtReorder.Text, "Reorder level"),
                    IsActive = existing == null || chkActive.Checked,
                    Image = imageChanged ? newImage : null
                };

                bool changeImage = existing == null || imageChanged;
                inventory.Save(actor, product, changeImage);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Save product", ex);
            }
        }
    }
}
