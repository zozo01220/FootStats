namespace FootStats.Web.Models;

public enum ShareInviteStatus
{
    Pending = 0,
    Claimed = 1,
    Expired = 2,
    Revoked = 3
}

/// <summary>Lien de partage généré par un Admin (carte saison ou page Utilisateurs) : porte un rôle et un ou
/// plusieurs joueurs déjà décidés, envoyé par email (Brevo) ou WhatsApp (partage manuel). Le destinataire clique
/// le lien et crée son propre compte (ou rattache ce périmètre à un compte déjà créé par un lien précédent),
/// sans jamais avoir été saisi manuellement par l'Admin.</summary>
public class ShareInvite
{
    public int Id { get; set; }

    /// <summary>Empreinte SHA-256 (hex) du jeton brut inclus dans le lien. Le jeton brut n'est jamais stocké.</summary>
    public required string TokenHash { get; set; }

    public UserRole Role { get; set; }

    public string? Relationship { get; set; }

    public List<Player> Players { get; set; } = [];

    /// <summary>Nullable pour permettre la suppression de l'Admin qui a créé ce lien sans bloquer sur une
    /// contrainte de clé étrangère (l'historique "qui a partagé" n'est qu'informatif).</summary>
    public int? CreatedByAdminId { get; set; }
    public AppUser? CreatedByAdmin { get; set; }

    public ShareInviteStatus Status { get; set; } = ShareInviteStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    /// <summary>Adresse saisie côté PC au moment de l'envoi, mémorisée uniquement pour l'affichage dans la liste
    /// de gestion (l'Admin se souvient à qui il a envoyé) ; jamais utilisée pour la sécurité du jeton.</summary>
    public string? RecipientEmailHint { get; set; }

    public int? ClaimedByUserId { get; set; }
    public AppUser? ClaimedByUser { get; set; }
}
