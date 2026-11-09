using System.Drawing;
using System.Windows.Forms;

namespace KYLMA.WinForms
{
    /// <summary>A small modal dialog that asks for one line of text, optionally masked.</summary>
    public sealed class PromptForm : Form
    {
        private readonly TextBox input = new TextBox { Width = 280 };

        private PromptForm(string title, string prompt, bool masked)
        {
            Text = title;
            Width = 400;
            Height = 180;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;

            if (masked) input.PasswordChar = '*';

            var label = new Label { Text = prompt, AutoSize = true, Location = new Point(14, 16) };
            input.Location = new Point(14, 48);
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(200, 92), Width = 90 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(298, 92), Width = 90 };

            Controls.Add(label);
            Controls.Add(input);
            Controls.Add(ok);
            Controls.Add(cancel);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        /// <summary>Returns the entered text, or null if the user cancelled.</summary>
        public static string Ask(IWin32Window owner, string title, string prompt, bool masked)
        {
            using (var form = new PromptForm(title, prompt, masked))
            {
                return form.ShowDialog(owner) == DialogResult.OK ? form.input.Text : null;
            }
        }
    }
}
