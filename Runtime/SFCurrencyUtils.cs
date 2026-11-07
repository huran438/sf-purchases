using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;

namespace SFramework.Purchases.Runtime
{
    public static class SFCurrencyUtils
    {
        // Cache results to avoid repeated culture scans.
        private static readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

        // Fallbacks for common currencies (extend as needed).
        private static readonly Dictionary<string, string> _fallback =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["USD"] = "$",
                ["EUR"] = "€",
                ["GBP"] = "£",
                ["JPY"] = "¥",
                ["CNY"] = "¥",
                ["RUB"] = "₽",
                ["UAH"] = "₴",
                ["KZT"] = "₸",
                ["INR"] = "₹",
                ["KRW"] = "₩",
                ["VND"] = "₫",
                ["THB"] = "฿",
                ["TRY"] = "₺",
                ["ILS"] = "₪",
                ["CHF"] = "CHF", // many locales use ISO text instead of a symbol
                ["AUD"] = "$",
                ["CAD"] = "$",
                ["NZD"] = "$",
                ["SGD"] = "$",
                ["HKD"] = "$",
                ["AED"] = "د.إ",
                ["SAR"] = "﷼",
                ["EGP"] = "E£",
                ["PLN"] = "zł",
                ["CZK"] = "Kč",
                ["SEK"] = "kr",
                ["NOK"] = "kr",
                ["DKK"] = "kr",
                ["HUF"] = "Ft",
                ["RON"] = "lei",
                ["BGN"] = "лв.",
                ["GEL"] = "₾",
                ["BRL"] = "R$",
                ["MXN"] = "$",
                ["ARS"] = "$",
                ["CLP"] = "$",
                ["COP"] = "$",
                ["PEN"] = "S/",
                ["ZAR"] = "R",
                ["IDR"] = "Rp",
                ["PHP"] = "₱",
                ["MYR"] = "RM",
                ["NGN"] = "₦",
            };

        /// <summary>
        /// Returns a currency symbol for a given ISO 4217 code (e.g., "USD").
        /// If no symbol is found, returns the input code (e.g., "USD").
        /// </summary>
        /// <param name="currencyCode">Currency code like "USD", "EUR".</param>
        /// <param name="preferLocalized">
        /// If true, try to get the symbol from the user's current culture first (may yield locale-specific symbols).
        /// </param>
        public static string GetCurrencySymbol(string currencyCode, bool preferLocalized = false)
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
                return string.Empty;

            // 1) Optional: try user's current culture (can differ by locale)
            if (preferLocalized)
            {
                try
                {
                    var current = CultureInfo.CurrentCulture;
                    var region = new RegionInfo(current.Name);
                    if (string.Equals(region.ISOCurrencySymbol, currencyCode, StringComparison.OrdinalIgnoreCase))
                        return current.NumberFormat.CurrencySymbol;
                }
                catch (ArgumentException) { /* some cultures have neutral names without region */ }
            }

            return _cache.GetOrAdd(currencyCode, FindFallbackSymbol);
        }

        private static string FindFallbackSymbol(string currencyCode)
        {
            // 2) Scan all specific cultures to find a matching region.
            try
            {
                foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
                {
                    try
                    {
                        var region = new RegionInfo(culture.Name);
                        if (string.Equals(region.ISOCurrencySymbol, currencyCode, StringComparison.OrdinalIgnoreCase))
                            return region.CurrencySymbol;
                    }
                    catch { /* ignore invalid/neutral cultures */ }
                }
            }
            catch
            {
                // Some platforms restrict CultureInfo enumeration.
            }

            // 3) Fallback map (stable across platforms).
            if (_fallback.TryGetValue(currencyCode, out var symbol))
                return symbol;

            // 4) Last resort: return the code itself.
            return currencyCode.ToUpperInvariant();
        }
    }
}
