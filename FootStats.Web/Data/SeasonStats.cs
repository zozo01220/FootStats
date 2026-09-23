using FootStats.Web.Models;

namespace FootStats.Web.Data;

public class TournamentSummary
{
    public int TournamentId { get; set; }
    public string? Name { get; set; }
    public EventType EventType { get; set; }
    public required string City { get; set; }
    public DateOnly Date { get; set; }
    public int MatchCount { get; set; }
    public int PlayerGoals { get; set; }
    public int TeamGoalsFor { get; set; }
    public int? TeamNumber { get; set; }
}

public class SeasonStats
{
    public int MatchCount { get; set; }
    public int TournamentCount { get; set; }
    public int PlayerGoals { get; set; }
    public int PlayerAssists { get; set; }
    public int TeamGoalsFor { get; set; }
    public double GoalsPerMatch { get; set; }
    public List<TournamentSummary> Tournaments { get; set; } = [];
}

/// <summary>Taux de présence aux entraînements d'une saison. Seules les occurrences explicitement pointées
/// (voir <see cref="TrainingAttendance"/>) et non annulées comptent dans le calcul : une séance jamais pointée
/// n'entre ni au numérateur ni au dénominateur.</summary>
public class SeasonAttendanceStats
{
    public int RecordedCount { get; set; }
    public int PresentCount { get; set; }
    public double? Percentage { get; set; }
}
