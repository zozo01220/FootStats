namespace FootStats.Web.Models;

public class MatchRecord
{
    public int Id { get; set; }

    /// <summary>Numéro d'équipe dans laquelle le joueur a évolué pour ce match (1 à 5).</summary>
    public int TeamNumber { get; set; }

    public string? Opponent { get; set; }

    public int PlayerGoals { get; set; }
    public int PlayerAssists { get; set; }
    public int TeamGoalsFor { get; set; }
    public int TeamGoalsAgainst { get; set; }
    public bool IsCaptain { get; set; }
    public bool IsStarter { get; set; } = true;
    public bool PlayedFullMatch { get; set; }

    /// <summary>Poste joué sur ce match précis (peut différer du poste habituel du joueur).</summary>
    public string? Position { get; set; }

    /// <summary>Anecdote/souvenir du match, libre et facultatif — pas une mesure de performance.</summary>
    public string? Note { get; set; }

    public int TournamentId { get; set; }
    public Tournament? Tournament { get; set; }
}
