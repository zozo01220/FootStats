namespace FootStats.Web.Models;

public class EquestrianEntry
{
    public int Id { get; set; }

    /// <summary>Ex: "Dressage", "Saut d'obstacles"</summary>
    public required string Discipline { get; set; }

    /// <summary>Ex: "3ème/12"</summary>
    public required string Ranking { get; set; }

    public int CompetitionId { get; set; }
    public EquestrianCompetition? Competition { get; set; }
}
