# KYLMA-POS: Enterprise POS & Inventory Management

A Windows Forms point-of-sale and inventory system built with C#, .NET Framework 4.8, and SQL Server.

## Project naming
- Solution / project: `KYLMA-POS` (first 3 letters of first name + first 2 letters of last name + POS)
- Database: `Mangubat` (last name)

## Features
- **Login** with salted password hashing, active-account check, and generic error messages.
- **Dashboard** with role-aware navigation, today's sales, orders, low-stock count, and a low-stock list.
- **Inventory**: product search by SKU, barcode, or name; add, edit, delete (deactivate); image upload (JPG/PNG, 2 MB, 2000 px max); low-stock highlighting; stock adjustments with reasons and a movement history; category management.
- **Point of sale**: scan or type a SKU/barcode and press Enter, set quantities, remove lines, customer selection, discount, tax, Cash/Card/Mobile payment, change due. Stock and prices are re-checked in one SQL transaction.
- **Receipts**: 80 mm layout, print preview and printing, and reprint by order number (read-only).
- **Customers**: search and add.
- **Sales analytics**: sales by day, top products, sales by category, CSV export. Managers and admins only.
- **User administration**: create users, assign roles, activate or deactivate, reset passwords, view last login. Admins only.
- **Error handling**: friendly messages on screen; details written to `%ProgramData%\KYLMA\Logs` (kept 30 days).

## Roles
| Role | Access |
|---|---|
| Admin | Everything |
| Manager | Dashboard, POS, inventory, customers, reprints, analytics |
| Cashier | Dashboard, POS, customers, reprints |

## Setup
1. Open SQL Server Management Studio and run `Database/01_CreateDatabase.sql`. It creates the `Mangubat` database, tables, procedures, and seed data. It can be run again safely.
2. Open `KYLMA-POS.slnx` in Visual Studio 2022 (with the .NET desktop workload and the .NET Framework 4.8 Developer Pack).
3. Set `KYLMA.WinForms` as the startup project.
4. Edit `KYLMA.WinForms/App.config`:
   - Set the `Mangubat` connection string for your server (use `App.config.example` as a template).
   - Set the store name, address, phone, return policy, and tax rate.
5. Build (Release for deployment) and run.

## Seed accounts (change before going live)
| Username | Password | Role |
|---|---|---|
| admin | Admin@123 | Admin |
| manager | Manager@123 | Manager |
| cashier | Cashier@123 | Cashier |

## Security notes
- Passwords are stored as salted SHA-256 hashes, as defined in the database schema. For stronger protection, move to PBKDF2 or Argon2 with a schema change.
- All SQL uses stored procedures with parameters. The UI never runs SQL directly.
- Business rules are enforced in the services, and stock and price rules are enforced again in SQL Server.
- Product images are checked by file signature (JPEG or PNG only).
- Do not commit real passwords, `App.config` with secrets, customer data, or backups.

## Testing checklist
- Database: run the script twice; duplicate SKU and barcode are rejected; negative stock is blocked; a failed sale leaves stock unchanged.
- Login: correct password succeeds; wrong password and inactive users fail with the same message; cashiers do not see Inventory or Analytics.
- Inventory: add, edit, delete (deactivate), search, image upload (valid and invalid files), stock adjustment, low-stock highlight.
- POS: empty cart is blocked; cash below total is blocked; change is correct; discount and tax round correctly; stock decreases exactly once.
- Receipts: preview and print; reprint by order number; long product names are truncated.
- Analytics: daily totals match the sales made in the test.
- Users: create, deactivate, reset password, and the admin cannot deactivate their own account.

## Known limitations
- No void or refund workflow (it is not in the required build order of the tutorial).
- No audit table for admin actions (SQL.docx has none). Stock changes are logged in `InventoryLog`.
- Scanner hardware: USB keyboard scanners work. Serial or camera scanners need an `IBarcodeScanner` implementation.

## Source control
Do not commit `bin/`, `obj/`, `.vs/`, `App.config` with real credentials, or `*.bak` files.
