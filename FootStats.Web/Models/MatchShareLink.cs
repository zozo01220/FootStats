namespace FootStats.Web.Models;

/// <summary>Lien de suivi en direct d'un évènement (tournoi/plateau), généré depuis sa fiche et envoyé par
/// WhatsApp : ouvre une page en lecture seule, sans connexion, valable 48h (voir <c>MatchShareLinkService</c>),
/// avec tous les matchs de l'évènement consultables. Contrairement à <see cref="ShareInvite"/>, ne crée jamais
/// de compte — juste une consultation temporaire.</summary>
public class MatchShareLink
{
    public int Id { get; set; }

    /// <summary>Empreinte SHA-256 (hex) du jeton brut inclus dans le lien. Le jeton brut n'est jamais stocké.</summary>
    public required string TokenHash { get; set; }

    public int TournamentId { get; set; }
    public Tournament? Tournament { get; set; }

    /// <summary>Nullable pour permettre la suppression du compte qui a généré ce lien sans bloquer sur une
    /// contrainte de clé étrangère (l'historique "qui a partagé" n'est qu'informatif).</summary>
    public int? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}
