namespace FootStats.Web.Models;

public enum InvitationStatus
{
    Sent = 0,
    Accepted = 1,
    Expired = 2,
    Revoked = 3
}

public enum InvitationPurpose
{
    /// <summary>Invitation envoyée à un compte famille (User/Consultant) : le lien fait choisir un mot de passe.</summary>
    FamilyInvite = 0,
    /// <summary>Notice envoyée à un Admin réactivé : le lien permet de changer de mot de passe, l'ancien reste valable.</summary>
    AdminReactivation = 1,
    /// <summary>Confirmation d'adresse email après une auto-inscription : le lien active le compte sans toucher au mot de passe.</summary>
    EmailVerification = 2,
    /// <summary>Demande de "mot de passe oublié" : le lien (30 minutes) permet de choisir un nouveau mot de passe.</summary>
    PasswordReset = 3
}

/// <summary>Invitation par email envoyée à un utilisateur pour qu'il active son compte et choisisse son mot de passe.</summary>
public class Invitation
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public AppUser? User { get; set; }

    /// <summary>Empreinte SHA-256 (hex) du jeton brut envoyé par email. Le jeton brut n'est jamais stocké.</summary>
    public required string TokenHash { get; set; }

    public InvitationStatus Status { get; set; } = InvitationStatus.Sent;
    public InvitationPurpose Purpose { get; set; } = InvitationPurpose.FamilyInvite;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime SentAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }

    /// <summary>Nombre de fois où l'invitation a été relancée automatiquement à l'expiration (max 1).</summary>
    public int AutoResendCount { get; set; }

    public int CreatedByAdminId { get; set; }
    public AppUser? CreatedByAdmin { get; set; }
}
