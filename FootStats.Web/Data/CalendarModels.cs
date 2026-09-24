using FootStats.Web.Models;

namespace FootStats.Web.Data;

public enum CalendarEventKind
{
    Tournament,
    Training
}

/// <summary>Un événement du calendrier, agrégé depuis un <see cref="Tournament"/> (une date) ou depuis
/// l'expansion d'une règle <see cref="Training"/> récurrente (une occurrence par date correspondante).</summary>
public class CalendarEvent
{
    public required CalendarEventKind Kind { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public required string Title { get; set; }
    public EventType? EventType { get; set; }
    public required string Location { get; set; }

    /// <summary>Numéro d'équipe (1 à 5) du tournoi/plateau — absent pour les entraînements.</summary>
    public int? TeamNumber { get; set; }

    public int PlayerId { get; set; }
    public required string PlayerName { get; set; }
    public string? ClubName { get; set; }
    public string? ClubLogoPath { get; set; }
    public required string Category { get; set; }

    public int SeasonId { get; set; }
    public int? TournamentId { get; set; }
    public int? TrainingId { get; set; }
    public int MatchCount { get; set; }

    public bool IsCancelled { get; set; }
    public TrainingCancellationReason? CancellationReason { get; set; }

    /// <summary>Présence pointée pour cette occurrence d'entraînement (null = pas encore pointée).</summary>
    public TrainingAttendanceStatus? AttendanceStatus { get; set; }
}
