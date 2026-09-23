namespace FootStats.Web.Models;

public enum EmailTemplateType
{
    ManageChildren = 0,
    ViewStats = 1,
    AccountReactivated = 2,
    AccountVerification = 3,
    PasswordReset = 4,
    /// <summary>Lien de partage (bouton "Partager") donnant un rôle Utilisateur : le destinataire n'a pas encore de compte.</summary>
    ShareInviteManage = 5,
    /// <summary>Lien de partage (bouton "Partager") donnant un rôle Consultant : le destinataire n'a pas encore de compte.</summary>
    ShareInviteConsultant = 6,
    /// <summary>Confirmation envoyée juste après la création d'un compte via un lien de partage (email/mot de passe) : rappelle le nom d'utilisateur.</summary>
    ShareInviteWelcome = 7,
    /// <summary>Rappel automatique la veille d'un match/tournoi/concours, envoyé à toute la famille (voir MatchReminderHostedService).</summary>
    MatchReminder = 8
}

/// <summary>Modèle d'email d'invitation, un par type d'invitation (gestion ou consultation).</summary>
public class EmailTemplate
{
    public int Id { get; set; }

    public EmailTemplateType Type { get; set; }

    public required string Subject { get; set; }
    public required string BodyHtml { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
