using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Windows.Forms;
using KYLMA.Services;

namespace KYLMA.WinForms
{
    /// <summary>Shared error display and input parsing. Users see plain messages; details go to the log.</summary>
    internal static class UiHelper
    {
        public static void ShowError(IWin32Window owner, string title, Exception ex)
        {
            bool expected = ex is BusinessRuleException || IsProcedureMessage(ex);
            if (!expected) AppLogger.LogException(title, ex);

            MessageBox.Show(owner, FriendlyMessage(ex), title, MessageBoxButtons.OK,
                            expected ? MessageBoxIcon.Warning : MessageBoxIcon.Error);
        }

        public static void Info(IWin32Window owner, string text, string title)
        {
            MessageBox.Show(owner, text, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static string FriendlyMessage(Exception ex)
        {
            if (ex is BusinessRuleException) return ex.Message;

            var sql = ex as SqlException;
            if (sql != null)
            {
                if (sql.Number >= 50000) return sql.Message;                 // THROW messages from stored procedures
                if (sql.Number == 2601 || sql.Number == 2627) return "That SKU, barcode or username already exists.";
                if (sql.Number == 547) return "This record is used by other data and cannot be changed or removed.";
                if (sql.Number == -1 || sql.Number == 53) return "Cannot reach the database server. Check the connection or ask your administrator.";
                return "The database could not complete the request. Please try again.";
            }

            return "Something went wrong. Please try again, or contact your administrator if it continues.";
        }

        /// <summary>Parses a money amount of zero or more, rounded to 2 decimals.</summary>
        public static decimal ParseMoney(string text, string field)
        {
            decimal value;
            if (!TryMoney(text, out value))
                throw new BusinessRuleException(field + " must be an amount of zero or more.");
            return decimal.Round(value, 2);
        }

        public static bool TryMoney(string text, out decimal value)
        {
            return decimal.TryParse((text ?? "").Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out value)
                   && value >= 0;
        }

        public static int ParseWhole(string text, string field)
        {
            int value;
            if (!int.TryParse((text ?? "").Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out value) || value < 0)
                throw new BusinessRuleException(field + " must be a whole number of zero or more.");
            return value;
        }

        private static bool IsProcedureMessage(Exception ex)
        {
            var sql = ex as SqlException;
            return sql != null && sql.Number >= 50000;
        }
    }
}
