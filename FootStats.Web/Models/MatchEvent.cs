namespace FootStats.Web.Models;

public enum MatchEventType
{
    Kickoff,
    Goal,
    Assist,
    OpponentGoal,
    PlayerIn,
    PlayerOut,
    FullTime,
    /// <summary>But marqué par l'équipe sans que ce soit le fils suivi (pas de passe D. de sa part non plus) —
    /// compte pour <see cref="MatchRecord.TeamGoalsFor"/> mais jamais pour <see cref="MatchRecord.PlayerGoals"/>.
    /// Ajouté en dernier dans l'enum : l'ordre des valeurs précédentes est la représentation stockée en base.</summary>
    TeammateGoal
}

/// <summary>Horodatage d'un évènement du suivi live d'un match (but, passe décisive, entrée/sortie du joueur…),
/// utilisé pour le fil du match affiché à la famille pendant et après le direct (voir <c>MatchLiveGuest.razor</c>).
/// Ne remplace pas les totaux (<see cref="MatchRecord.PlayerGoals"/> etc.), qui restent la source de vérité pour
/// les statistiques de saison.</summary>
public class MatchEvent
{
    public int Id { get; set; }

    public int MatchRecordId { get; set; }
    public MatchRecord? MatchRecord { get; set; }

    public MatchEventType Type { get; set; }

    /// <summary>Minutes écoulées depuis le coup d'envoi (<see cref="MatchRecord.StartedAt"/>) au moment de l'action.</summary>
    public int MinuteMark { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
