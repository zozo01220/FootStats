using System.ComponentModel.DataAnnotations.Schema;

namespace FootStats.Web.Models;

public class MatchRecord
{
    public int Id { get; set; }

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

    /// <summary>Durée du match en minutes, saisie manuellement (pas de détection automatique de la mi-temps).</summary>
    public int? DurationMinutes { get; set; }

    /// <summary>Coup d'envoi cliqué depuis la fiche match (suivi live, mobile uniquement) ; null tant que le match
    /// n'a pas démarré. Avec <see cref="EndedAt"/>, détermine seul le statut du match : pas besoin d'un enum à part
    /// à garder synchronisé.</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>Renseigné au clic sur "Arrêter le match".</summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>Le joueur est actuellement sur le terrain (suivi live) ; recalculé à "Démarrer" depuis <see
    /// cref="IsStarter"/>, puis bascule avec les boutons Entrer/Sortir.</summary>
    public bool OnField { get; set; } = true;

    public List<MatchEvent> Events { get; set; } = [];

    [NotMapped]
    public bool IsLive => StartedAt is not null && EndedAt is null;

    public int TournamentId { get; set; }
    public Tournament? Tournament { get; set; }
}
