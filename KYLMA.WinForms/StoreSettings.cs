using System;
using System.Configuration;
using System.Globalization;
using KYLMA.Domain;

namespace KYLMA.WinForms
{
    /// <summary>Store details and tax rate from App.config (appSettings).</summary>
    internal static class StoreSettings
    {
        public const decimal DefaultTaxPercent = 12m;

        public static BusinessProfile Profile()
        {
            return new BusinessProfile
            {
                Name = Get("BusinessName", "Enterprise POS"),
                Address = Get("BusinessAddress", ""),
                Phone = Get("BusinessPhone", ""),
                ReturnPolicy = Get("ReturnPolicy", "")
            };
        }

        /// <summary>Tax percentage applied to (subtotal - discount). Falls back to 12 if the setting is invalid.</summary>
        public static decimal TaxRatePercent()
        {
            decimal value;
            if (decimal.TryParse(Get("TaxRatePercent", "12"), NumberStyles.Number, CultureInfo.InvariantCulture, out value)
                && value >= 0 && value <= 100)
            {
                return value;
            }
            return DefaultTaxPercent;
        }

        private static string Get(string key, string fallback)
        {
            var value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
