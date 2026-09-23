namespace FootStats.Web.Models;

public class Season
{
    public int Id { get; set; }

    /// <summary>Ex: "2026-2027"</summary>
    public required string Label { get; set; }

    /// <summary>Ex: "U11", "U13"</summary>
    public required string Category { get; set; }

    public int ClubId { get; set; }
    public Club? Club { get; set; }

    public int PlayerId { get; set; }
    public Player? Player { get; set; }

    public Sport Sport { get; set; } = Sport.Football;

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsArchived { get; set; }

    public List<Tournament> Tournaments { get; set; } = [];
    public List<EquestrianCompetition> EquestrianCompetitions { get; set; } = [];
}

public static class SeasonCategoryOptions
{
    public static readonly string[] Presets =
    [
        "U7", "U9", "U11", "U13", "U15", "U17", "U19", "U21", "Seniors", "Loisirs"
    ];

    /// <summary>Liste à afficher dans le menu déroulant : les catégories standard, plus la valeur actuelle
    /// si elle a été saisie librement avant l'introduction de cette liste (pour ne pas la perdre à l'édition).</summary>
    public static IEnumerable<string> ForSelect(string? currentValue)
    {
        if (!string.IsNullOrWhiteSpace(currentValue) && !Presets.Contains(currentValue))
        {
            return [currentValue, .. Presets];
        }
        return Presets;
    }

    /// <summary>Suggère une catégorie standard à partir de l'âge du joueur au début de la saison (convention
    /// suisse : différence d'année civile entre le début de saison et la naissance, pas l'âge exact au jour
    /// près). Toujours une proposition modifiable avant validation, jamais appliquée automatiquement.</summary>
    public static string SuggestForAge(DateOnly birthDate, int seasonStartYear)
    {
        var age = seasonStartYear - birthDate.Year;
        return age switch
        {
            <= 6 => "U7",
            <= 8 => "U9",
            <= 10 => "U11",
            <= 12 => "U13",
            <= 14 => "U15",
            <= 16 => "U17",
            <= 18 => "U19",
            <= 20 => "U21",
            _ => "Seniors"
        };
    }
}
