using FootStats.Web.Models;

namespace FootStats.Web.Data;

/// <summary>Une <see cref="Season"/> n'est pas recréée chaque année : la même ligne est prolongée (un joueur
/// ne peut avoir qu'une seule saison par club, voir la contrainte dans Home.razor). Ce helper détecte les
/// saisons dont la fin approche ou est dépassée pour un joueur toujours actif, et prépare les valeurs
/// suggérées pour la prolongation — toujours une proposition à valider, jamais une écriture automatique.</summary>
public static class SeasonRenewalHelper
{
    private const int RenewalWindowDays = 60;

    public static bool NeedsRenewal(Season season, DateOnly today) =>
        season.Player is { IsActive: true } && season.EndDate is { } end && end <= today.AddDays(RenewalWindowDays);

    /// <summary>Calcule les champs suggérés pour prolonger la saison d'un an : mêmes club/sport (repris tels
    /// quels, car c'est la même ligne), dates décalées d'un an, catégorie recalculée depuis l'âge du joueur.</summary>
    public static (string StartMonth, string EndMonth, string Category) SuggestRenewal(Season season)
    {
        var previousStart = season.StartDate ?? season.EndDate ?? DateOnly.FromDateTime(DateTime.Today);
        var previousEnd = season.EndDate ?? previousStart.AddMonths(10);

        var nextStart = previousStart.AddYears(1);
        var nextEnd = previousEnd.AddYears(1);

        var category = season.Player is { } player
            ? SeasonCategoryOptions.SuggestForAge(player.BirthDate, nextStart.Year)
            : season.Category;

        return ($"{nextStart.Year:0000}-{nextStart.Month:00}", $"{nextEnd.Year:0000}-{nextEnd.Month:00}", category);
    }
}
