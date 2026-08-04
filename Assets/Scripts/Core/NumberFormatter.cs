using System;

namespace ClickerGenesis.Core
{
    /// <summary>
    /// Formats large Ink/cost values per the player's chosen NumberNotation
    /// (GameSettings.Notation): standard idle-game abbreviations (K/M/B/T/...), scientific, or
    /// engineering. Every screen should format through here rather than ToString("F1") directly,
    /// so the notation setting actually applies everywhere at once.
    /// </summary>
    public static class NumberFormatter
    {
        // Short-scale suffixes. Named range covers up to 10^63 (Vigintillion); anything larger
        // falls back to scientific notation rather than inventing further suffixes.
        private static readonly string[] Suffixes =
        {
            "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc",
            "Ud", "Dd", "Td", "Qad", "Qid", "Sxd", "Spd", "Ocd", "Nod", "Vg"
        };

        public static string Format(double value) => Format(value, GameSettings.Notation);

        public static string Format(double value, NumberNotation notation)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return value.ToString();

            switch (notation)
            {
                case NumberNotation.Scientific:
                    return FormatScientific(value);
                case NumberNotation.Engineering:
                    return FormatEngineering(value);
                default:
                    return FormatStandard(value);
            }
        }

        private static string FormatStandard(double value)
        {
            double abs = Math.Abs(value);
            if (abs < 1000) return value.ToString("F1");

            int tier = 0;
            double scaled = value;
            while (Math.Abs(scaled) >= 1000 && tier < Suffixes.Length - 1)
            {
                scaled /= 1000;
                tier++;
            }

            // Rounding to 2 decimals can push e.g. 999999 (scaled=999.999) up to display as
            // "1000.00" - bump to the next tier so it reads "1.00M" instead of "1000.00K".
            if (Math.Round(scaled, 2) >= 1000 && tier < Suffixes.Length - 1)
            {
                scaled /= 1000;
                tier++;
            }

            if (Math.Abs(scaled) >= 1000)
                return FormatScientific(value); // past our named range

            return scaled.ToString("F2") + Suffixes[tier];
        }

        private static string FormatScientific(double value)
        {
            double abs = Math.Abs(value);
            if (abs < 1000) return value.ToString("F1");
            return value.ToString("0.###e+0");
        }

        private static string FormatEngineering(double value)
        {
            double abs = Math.Abs(value);
            if (abs < 1000) return value.ToString("F1");

            int exponent = (int)Math.Floor(Math.Log10(abs));
            int engineeringExponent = exponent - (((exponent % 3) + 3) % 3);
            double mantissa = value / Math.Pow(10, engineeringExponent);

            // Same rounding-boundary issue as standard notation: 999.999 can round to "1000.00".
            if (Math.Round(Math.Abs(mantissa), 2) >= 1000)
            {
                mantissa /= 1000;
                engineeringExponent += 3;
            }

            return mantissa.ToString("F2") + "e" + (engineeringExponent >= 0 ? "+" : "") + engineeringExponent;
        }
    }
}
