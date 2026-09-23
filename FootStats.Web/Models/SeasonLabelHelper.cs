namespace FootStats.Web.Models;

/// <summary>Calcule le libellé d'une saison (ex: "2026" ou "2026/2027") à partir de mois "yyyy-MM" saisis
/// librement dans les formulaires de création/édition de saison.</summary>
public static class SeasonLabelHelper
{
    private static readonly string[] MonthFormats = ["yyyy-MM", "yyyy-M", "MM/yyyy", "M/yyyy"];

    public static bool TryParseMonth(string? value, out DateTime date)
    {
        date = default;
        return !string.IsNullOrWhiteSpace(value)
            && DateTime.TryParseExact(value.Trim(), MonthFormats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out date);
    }

    public static string? ComputeLabel(string? startMonth, string? endMonth)
    {
        if (!TryParseMonth(startMonth, out var start)) return null;
        if (!TryParseMonth(endMonth, out var end) || end.Year == start.Year) return start.Year.ToString();
        return $"{start.Year}/{end.Year}";
    }
}
