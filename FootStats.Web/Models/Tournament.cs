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

    public int SeasonId { get; set; }
    public Season? Season { get; set; }

    /// <summary>Empêche de renvoyer plusieurs fois le rappel par email de cet évènement (voir MatchReminderHostedService).</summary>
    public bool ReminderSent { get; set; }

    public List<MatchRecord> Matches { get; set; } = [];
}
