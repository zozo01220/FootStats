namespace FootStats.Web.Models;

public enum EventType
{
    Tournoi,
    Plateau
}

public class Tournament
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public EventType EventType { get; set; } = EventType.Tournoi;
    public required string City { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? KickoffTime { get; set; }

    /// <summary>Numéro d'équipe (1 à 5) pour tous les matchs de cet événement — fixé une fois à la création,
    /// plutôt que choisi match par match (un tournoi/plateau se joue avec une seule équipe).</summary>
    public int TeamNumber { get; set; } = 1;

    public int SeasonId { get; set; }
    public Season? Season { get; set; }

    /// <summary>Empêche de renvoyer plusieurs fois le rappel par email de cet évènement (voir MatchReminderHostedService).</summary>
    public bool ReminderSent { get; set; }

    /// <summary>Présence pointée à ce tournoi/plateau. Un seul champ (pas de table à part comme
    /// <see cref="TrainingAttendance"/>) puisqu'un tournoi n'est pas récurrent : une ligne = un événement.</summary>
    public AttendanceStatus? AttendanceStatus { get; set; }

    /// <summary>Masque le bloc "Événement en cours" de l'accueil une fois cliqué sur "Événement terminé" (suivi
    /// live, mobile uniquement), sans affecter les matchs ni les statistiques déjà enregistrés.</summary>
    public bool DismissedFromHome { get; set; }

    public List<MatchRecord> Matches { get; set; } = [];
}
