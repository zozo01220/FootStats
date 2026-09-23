namespace FootStats.Web.Models;

public class EquestrianCompetition
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public required string City { get; set; }
    public DateOnly Date { get; set; }

    public int SeasonId { get; set; }
    public Season? Season { get; set; }

    /// <summary>Empêche de renvoyer plusieurs fois le rappel par email de cet évènement (voir MatchReminderHostedService).</summary>
    public bool ReminderSent { get; set; }

    public List<EquestrianEntry> Entries { get; set; } = [];
}
