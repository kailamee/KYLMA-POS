using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KYLMA.Domain;
using KYLMA.Services;

namespace KYLMA.WinForms
{
    /// <summary>Admin only. Create users, assign roles, activate or deactivate accounts, reset passwords.</summary>
    public sealed class UserAdminForm : Form
    {
        private readonly User actor;
        private readonly UserService users = new UserService();
        private List<UserAccount> rows = new List<UserAccount>();
        private List<RoleInfo> roles = new List<RoleInfo>();

        private readonly DataGridView grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None
        };

        private readonly TextBox txtUser = new TextBox { Width = 250, MaxLength = 50 };
        private readonly TextBox txtFull = new TextBox { Width = 250, MaxLength = 100 };
        private readonly TextBox txtPassword = new TextBox { Width = 250, PasswordChar = '*', MaxLength = 100 };
        private readonly ComboBox cboNewRole = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 250 };
        private readonly Label lblSelected = new Label { AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        private readonly ComboBox cboEditRole = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 250 };
        private readonly CheckBox chkActive = new CheckBox { Text = "Account active", AutoSize = true };

        public UserAdminForm(User user)
        {
            actor = user;
            Text = "Users & Roles";
            Width = 1000;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(8) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(grid, 0, 0);
            root.Controls.Add(BuildSidePanel(), 1, 0);
            Controls.Add(root);

            grid.SelectionChanged += (s, e) => ShowSelected();
            Load += (s, e) => { LoadRoles(); LoadUsers(); };
        }

        private Control BuildSidePanel()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(10)
            };

            panel.Controls.Add(Heading("Create user"));
            panel.Controls.Add(new Label { Text = "Username (3-50: letters, digits, . _ -)", AutoSize = true });
            panel.Controls.Add(txtUser);
            panel.Controls.Add(new Label { Text = "Full name", AutoSize = true });
            panel.Controls.Add(txtFull);
            panel.Controls.Add(new Label { Text = "Password (at least 8 characters)", AutoSize = true });
            panel.Controls.Add(txtPassword);
            panel.Controls.Add(new Label { Text = "Role", AutoSize = true });
            panel.Controls.Add(cboNewRole);
            var create = new Button { Text = "Create user", AutoSize = true, Margin = new Padding(0, 8, 0, 18) };
            create.Click += (s, e) => CreateUser();
            panel.Controls.Add(create);

            panel.Controls.Add(Heading("Selected user"));
            panel.Controls.Add(lblSelected);
            panel.Controls.Add(new Label { Text = "Role", AutoSize = true, Margin = new Padding(0, 8, 0, 0) });
            panel.Controls.Add(cboEditRole);
            panel.Controls.Add(chkActive);
            var save = new Button { Text = "Save role and status", AutoSize = true, Margin = new Padding(0, 8, 0, 8) };
            save.Click += (s, e) => SaveSelected();
            panel.Controls.Add(save);
            var reset = new Button { Text = "Reset password...", AutoSize = true };
            reset.Click += (s, e) => ResetPassword();
            panel.Controls.Add(reset);

            return panel;
        }

        private static Label Heading(string text)
        {
            return new Label { Text = text, AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold), Margin = new Padding(0, 6, 0, 6) };
        }

        private void LoadRoles()
        {
            try
            {
                roles = users.Roles();
                cboNewRole.DisplayMember = "Name";
                cboNewRole.DataSource = roles.ToList();
                cboEditRole.DisplayMember = "Name";
                cboEditRole.DataSource = roles.ToList();
                if (roles.Count > 0) cboNewRole.SelectedIndex = roles.Count - 1;   // default to the lowest role
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Roles", ex);
            }
        }

        private void LoadUsers()
        {
            try
            {
                rows = users.List(actor);
                grid.DataSource = rows.Select(u => new
                {
                    u.Id,
                    u.Username,
                    Name = u.FullName,
                    Role = u.RoleName,
                    Active = u.IsActive ? "Yes" : "No",
                    LastLogin = u.LastLoginAt.HasValue ? u.LastLoginAt.Value.ToString("yyyy-MM-dd HH:mm") : "Never",
                    Created = u.CreatedAt.ToString("yyyy-MM-dd")
                }).ToList();
                if (grid.Columns.Contains("Id")) grid.Columns["Id"].Visible = false;
                ShowSelected();
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Users", ex);
            }
        }

        private UserAccount Selected()
        {
            if (grid.CurrentRow == null || grid.CurrentRow.Index >= rows.Count) return null;
            return rows[grid.CurrentRow.Index];
        }

        private void ShowSelected()
        {
            var account = Selected();
            if (account == null)
            {
                lblSelected.Text = "Select a user in the list";
                return;
            }
            lblSelected.Text = account.Username + (account.Id == actor.Id ? "  (you)" : "");
            cboEditRole.SelectedItem = roles.FirstOrDefault(r => r.Id == account.RoleId);
            chkActive.Checked = account.IsActive;
        }

        private void CreateUser()
        {
            try
            {
                var role = cboNewRole.SelectedItem as RoleInfo;
                users.Create(actor, txtUser.Text, txtFull.Text, role == null ? 0 : role.Id, txtPassword.Text);
                txtUser.Clear();
                txtFull.Clear();
                txtPassword.Clear();
                LoadUsers();
                UiHelper.Info(this, "User created.", "Users & Roles");
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Create user", ex);
            }
        }

        private void SaveSelected()
        {
            var account = Selected();
            var role = cboEditRole.SelectedItem as RoleInfo;
            if (account == null) { UiHelper.Info(this, "Select a user first.", "Users & Roles"); return; }
            if (role == null) { UiHelper.Info(this, "Choose a role.", "Users & Roles"); return; }

            try
            {
                users.Update(actor, new UserAccount
                {
                    Id = account.Id,
                    FullName = account.FullName,
                    RoleId = role.Id,
                    IsActive = chkActive.Checked
                });
                LoadUsers();
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Save user", ex);
            }
        }

        private void ResetPassword()
        {
            var account = Selected();
            if (account == null) { UiHelper.Info(this, "Select a user first.", "Users & Roles"); return; }

            var password = PromptForm.Ask(this, "Reset password",
                "New password for " + account.Username + " (at least 8 characters):", true);
            if (password == null) return;

            try
            {
                users.ResetPassword(actor, account.Id, password);
                UiHelper.Info(this, "Password updated for " + account.Username + ".", "Users & Roles");
            }
            catch (Exception ex)
            {
                UiHelper.ShowError(this, "Reset password", ex);
            }
        }
    }
}
