using System;
using System.Drawing;
using System.Windows.Forms;
using KYLMA.Domain;
using KYLMA.Services;

namespace KYLMA.WinForms
{
    public sealed class LoginForm : Form
    {
        private readonly TextBox txtUsername = new TextBox();
        private readonly TextBox txtPassword = new TextBox { PasswordChar = '*' };
        private readonly Label lblStatus = new Label();
        private readonly Button btnLogin = new Button();

        public LoginForm()
        {
            Text = "Enterprise POS - Log In";
            Width = 700;
            Height = 450;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.White;
            AutoScaleMode = AutoScaleMode.Font;

            BuildUI();
        }

        private void BuildUI()
        {
            var pnlBrand = new Panel
            {
                Dock = DockStyle.Left,
                Width = 300,
                BackColor = Color.FromArgb(20, 40, 90)
            };

            pnlBrand.Controls.Add(new Label
            {
                Text = "ENTERPRISE\nPOS",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            });

            var pnlLogin = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var lblTitle = new Label
            {
                Text = "Welcome Back",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                Location = new Point(50, 60),
                AutoSize = true
            };

            var lblSub = new Label
            {
                Text = "Please log in to your account",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.Gray,
                Location = new Point(50, 95),
                AutoSize = true
            };

            var lblUser = new Label { Text = "Username", Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true, Location = new Point(50, 150) };
            txtUsername.Location = new Point(50, 175);
            txtUsername.Width = 280;
            txtUsername.Font = new Font("Segoe UI", 12);
            txtUsername.BorderStyle = BorderStyle.FixedSingle;

            var lblPass = new Label { Text = "Password", Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true, Location = new Point(50, 220) };
            txtPassword.Location = new Point(50, 245);
            txtPassword.Width = 280;
            txtPassword.Font = new Font("Segoe UI", 12);
            txtPassword.BorderStyle = BorderStyle.FixedSingle;

            btnLogin.Text = "Log in";
            btnLogin.Location = new Point(50, 300);
            btnLogin.Width = 280;
            btnLogin.Height = 45;
            btnLogin.BackColor = Color.FromArgb(20, 40, 90);
            btnLogin.ForeColor = Color.White;
            btnLogin.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Cursor = Cursors.Hand;
            btnLogin.Click += BtnLogin_Click;
            AcceptButton = btnLogin;

            // Generic message only: never reveal whether the username exists.
            lblStatus.Location = new Point(50, 355);
            lblStatus.Size = new Size(280, 40);
            lblStatus.ForeColor = Color.Firebrick;
            lblStatus.Font = new Font("Segoe UI", 9);

            pnlLogin.Controls.Add(lblTitle);
            pnlLogin.Controls.Add(lblSub);
            pnlLogin.Controls.Add(lblUser);
            pnlLogin.Controls.Add(txtUsername);
            pnlLogin.Controls.Add(lblPass);
            pnlLogin.Controls.Add(txtPassword);
            pnlLogin.Controls.Add(btnLogin);
            pnlLogin.Controls.Add(lblStatus);

            Controls.Add(pnlLogin);
            Controls.Add(pnlBrand);
            pnlBrand.SendToBack();
            pnlLogin.BringToFront();
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            var username = txtUsername.Text.Trim();
            if (username.Length == 0 || txtPassword.Text.Length == 0)
            {
                lblStatus.Text = "Enter your username and password.";
                return;
            }

            User user;
            try
            {
                user = new AuthService().Login(username, txtPassword.Text);
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Sign-in", ex);
                return;
            }

            if (user == null)
            {
                lblStatus.Text = "Invalid username or password, or the account is inactive.";
                txtPassword.Clear();
                return;
            }

            lblStatus.Text = "";
            txtPassword.Clear();
            var main = new MainForm(user);
            main.FormClosed += (s, args) => Close();   // closing the main window ends the session
            Hide();
            main.Show();
        }
    }
}
