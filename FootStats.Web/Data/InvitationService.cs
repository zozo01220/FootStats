using System.Security.Cryptography;
using FootStats.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FootStats.Web.Data;

public enum InvitationAcceptError
{
    NotFound,
    Expired,
    AlreadyUsed,
    Revoked
}

public class InvitationAcceptResult
{
    public bool Success { get; init; }
    public InvitationAcceptError? Error { get; init; }
    /// <summary>Vrai si le compte visé était déjà actif au moment du clic (lien de changement de mot de passe
    /// envoyé après une réactivation), plutôt qu'une première activation de compte.</summary>
    public bool TargetAlreadyActive { get; init; }
    /// <summary>Nature du jeton accepté/validé, pour adapter le texte affiché (activation, réactivation, mot de passe oublié...).</summary>
    public InvitationPurpose? Purpose { get; init; }

    public static InvitationAcceptResult Ok(bool targetAlreadyActive = false, InvitationPurpose? purpose = null) =>
        new() { Success = true, TargetAlreadyActive = targetAlreadyActive, Purpose = purpose };
    public static InvitationAcceptResult Fail(InvitationAcceptError error, InvitationPurpose? purpose = null) =>
        new() { Success = false, Error = error, Purpose = purpose };
}

public enum PasswordResetRequestResult
{
    /// <summary>Lien envoyé avec succès.</summary>
    Sent,
    /// <summary>Aucun compte actif ne correspond au nom d'utilisateur ou à l'email saisi.</summary>
    NotFound,
    /// <summary>Le compte existe mais n'a aucune adresse email enregistrée : impossible d'envoyer un lien.</summary>
    NoEmailOnFile,
    /// <summary>Le compte et l'email sont valides, mais l'envoi a échoué (SMTP mal configuré, erreur réseau...).</summary>
    SendFailed
}

/// <summary>Gère le cycle de vie des jetons envoyés par email : invitations famille (User/Consultant),
/// notices de réactivation Admin, et confirmations d'email pour l'auto-inscription.</summary>
public class InvitationService(AppDbContext db, IEmailSender emailSender, IConfiguration config, ILogger<InvitationService> logger, IHttpContextAccessor httpContextAccessor)
{
    private const int ExpiryHours = 72;
    private static readonly TimeSpan PasswordResetExpiry = TimeSpan.FromMinutes(30);

    /// <summary>Base URL publique de l'application, utilisée pour construire le lien d'invitation. Dérivée de la
    /// requête en cours quand elle existe (dev comme prod, quel que soit le domaine réel vu par nginx), sinon
    /// repliée sur App:BaseUrl — c'est ce deuxième cas qui s'applique depuis les services d'arrière-plan
    /// (<see cref="InvitationExpiryHostedService"/>, <see cref="MatchReminderHostedService"/>), qui n'ont pas de
    /// requête HTTP en cours.</summary>
    private string BaseUrl
    {
        get
        {
            var request = httpContextAccessor.HttpContext?.Request;
            if (request is not null) return $"{request.Scheme}://{request.Host}";
            return (config["App:BaseUrl"] ?? "https://footstats.lok-izy.fr").TrimEnd('/');
        }
    }

    public async Task CreateAndSendAsync(int userId, int createdByAdminId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException("Utilisateur introuvable.");

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException("Cet utilisateur n'a pas d'adresse email.");
        }

        var (invitation, rawToken) = await CreateInvitationAsync(user.Id, createdByAdminId, InvitationPurpose.FamilyInvite);
        await SendPurposedEmailAsync(invitation, user, rawToken);
    }

    /// <summary>Envoyée quand un SuperAdmin réactive un compte Admin précédemment révoqué : l'ancien mot de passe
    /// reste valable, le lien ne sert qu'à en choisir un nouveau si l'administrateur le souhaite.</summary>
    public async Task SendReactivationNoticeAsync(int userId, int reactivatedByAdminId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException("Utilisateur introuvable.");

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException("Cet administrateur n'a pas d'adresse email renseignée.");
        }

        var (invitation, rawToken) = await CreateInvitationAsync(user.Id, reactivatedByAdminId, InvitationPurpose.AdminReactivation);
        await SendPurposedEmailAsync(invitation, user, rawToken);
    }

    /// <summary>Envoyée juste après une auto-inscription publique : confirme l'adresse email et active le compte,
    /// sans jamais redemander de mot de passe (déjà choisi au moment de l'inscription).</summary>
    public async Task CreateAndSendEmailVerificationAsync(int userId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException("Utilisateur introuvable.");

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new InvalidOperationException("Cet utilisateur n'a pas d'adresse email.");
        }

        // Pas de tiers créateur pour une auto-inscription : on référence l'utilisateur lui-même.
        var (invitation, rawToken) = await CreateInvitationAsync(user.Id, user.Id, InvitationPurpose.EmailVerification);
        await SendPurposedEmailAsync(invitation, user, rawToken);
    }

    /// <summary>Déclenchée depuis "Mot de passe oublié" : envoie un lien de réinitialisation valable 30 minutes
    /// si le nom d'utilisateur ou l'email correspond à un compte actif. Contrairement aux autres flux d'invitation,
    /// indique explicitement si aucun compte ne correspond (choix assumé sur cette application familiale, pour
    /// éviter la confusion d'un message de succès trompeur) plutôt que de renvoyer un message générique.</summary>
    public async Task<PasswordResetRequestResult> RequestPasswordResetAsync(string usernameOrEmail)
    {
        var normalized = usernameOrEmail.Trim();
        if (normalized.Length == 0) return PasswordResetRequestResult.NotFound;

        var user = await db.Users.FirstOrDefaultAsync(u =>
            u.IsActive && (u.Username == normalized || (u.Email != null && u.Email.ToLower() == normalized.ToLower())));

        if (user is null) return PasswordResetRequestResult.NotFound;
        if (string.IsNullOrWhiteSpace(user.Email)) return PasswordResetRequestResult.NoEmailOnFile;

        // Invalide les demandes de reset encore valides pour ce compte : un seul lien actif à la fois.
        var pendingResets = await db.Invitations
            .Where(i => i.UserId == user.Id && i.Purpose == InvitationPurpose.PasswordReset && i.Status == InvitationStatus.Sent)
            .ToListAsync();
        foreach (var pending in pendingResets)
        {
            pending.Status = InvitationStatus.Revoked;
        }
        await db.SaveChangesAsync();

        var (invitation, rawToken) = await CreateInvitationAsync(user.Id, user.Id, InvitationPurpose.PasswordReset, PasswordResetExpiry);
        try
        {
            await SendPurposedEmailAsync(invitation, user, rawToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Échec de l'envoi de l'email de réinitialisation de mot de passe pour l'utilisateur {UserId}.", user.Id);
            return PasswordResetRequestResult.SendFailed;
        }
        return PasswordResetRequestResult.Sent;
    }

    public async Task<bool> CanResendAsync(int userId)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return false;
        // Un compte déjà connecté et actif n'a plus besoin d'invitation ; il faut d'abord le révoquer.
        return !(user.HasLoggedInAt != null && user.IsActive);
    }

    public async Task RevokeAsync(int userId)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null) return;
        user.IsActive = false;

        var invitation = await db.Invitations
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync();
        if (invitation is not null && invitation.Status == InvitationStatus.Sent)
        {
            invitation.Status = InvitationStatus.Revoked;
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Vérifie l'état d'un jeton d'invitation (famille ou réactivation) sans le consommer, pour décider
    /// quoi afficher à l'ouverture de la page d'activation. Exclut les jetons de confirmation d'email
    /// (traités séparément par ConfirmEmailAsync, qui ne demande jamais de mot de passe).</summary>
    public async Task<InvitationAcceptResult> ValidateTokenAsync(string rawToken)
    {
        var tokenHash = HashToken(rawToken);
        var invitation = await db.Invitations
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash && i.Purpose != InvitationPurpose.EmailVerification);

        if (invitation is null) return InvitationAcceptResult.Fail(InvitationAcceptError.NotFound);
        if (invitation.Status == InvitationStatus.Revoked) return InvitationAcceptResult.Fail(InvitationAcceptError.Revoked, invitation.Purpose);
        if (invitation.Status == InvitationStatus.Accepted) return InvitationAcceptResult.Fail(InvitationAcceptError.AlreadyUsed, invitation.Purpose);
        if (invitation.Status == InvitationStatus.Expired || invitation.ExpiresAt <= DateTime.UtcNow)
        {
            return InvitationAcceptResult.Fail(InvitationAcceptError.Expired, invitation.Purpose);
        }
        return InvitationAcceptResult.Ok(invitation.User?.IsActive ?? false, invitation.Purpose);
    }

    public async Task<InvitationAcceptResult> AcceptAsync(string rawToken, string newPassword)
    {
        var tokenHash = HashToken(rawToken);
        var invitation = await db.Invitations
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash && i.Purpose != InvitationPurpose.EmailVerification);

        if (invitation is null)
        {
            return InvitationAcceptResult.Fail(InvitationAcceptError.NotFound);
        }
        if (invitation.Status == InvitationStatus.Revoked)
        {
            return InvitationAcceptResult.Fail(InvitationAcceptError.Revoked, invitation.Purpose);
        }
        if (invitation.Status == InvitationStatus.Accepted)
        {
            return InvitationAcceptResult.Fail(InvitationAcceptError.AlreadyUsed, invitation.Purpose);
        }
        if (invitation.Status == InvitationStatus.Expired || invitation.ExpiresAt <= DateTime.UtcNow)
        {
            if (invitation.Status != InvitationStatus.Expired)
            {
                invitation.Status = InvitationStatus.Expired;
                await db.SaveChangesAsync();
            }
            return InvitationAcceptResult.Fail(InvitationAcceptError.Expired, invitation.Purpose);
        }

        var user = invitation.User ?? throw new InvalidOperationException("Utilisateur de l'invitation introuvable.");
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.IsActive = true;

        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return InvitationAcceptResult.Ok(purpose: invitation.Purpose);
    }

    /// <summary>Confirme une adresse email après auto-inscription : active le compte sans toucher au mot de passe
    /// (déjà choisi à l'inscription). N'accepte que les jetons créés avec Purpose=EmailVerification.</summary>
    public async Task<InvitationAcceptResult> ConfirmEmailAsync(string rawToken)
    {
        var tokenHash = HashToken(rawToken);
        var invitation = await db.Invitations
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash && i.Purpose == InvitationPurpose.EmailVerification);

        if (invitation is null)
        {
            return InvitationAcceptResult.Fail(InvitationAcceptError.NotFound);
        }
        if (invitation.Status == InvitationStatus.Revoked)
        {
            return InvitationAcceptResult.Fail(InvitationAcceptError.Revoked);
        }
        if (invitation.Status == InvitationStatus.Accepted)
        {
            return InvitationAcceptResult.Fail(InvitationAcceptError.AlreadyUsed);
        }
        if (invitation.Status == InvitationStatus.Expired || invitation.ExpiresAt <= DateTime.UtcNow)
        {
            if (invitation.Status != InvitationStatus.Expired)
            {
                invitation.Status = InvitationStatus.Expired;
                await db.SaveChangesAsync();
            }
            return InvitationAcceptResult.Fail(InvitationAcceptError.Expired);
        }

        var user = invitation.User ?? throw new InvalidOperationException("Utilisateur de l'invitation introuvable.");
        user.IsActive = true;

        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return InvitationAcceptResult.Ok();
    }

    /// <summary>Utilisée par le service d'arrière-plan pour relancer un jeton expiré (quel que soit son type)
    /// avec un nouveau jeton.</summary>
    internal async Task ResendWithFreshTokenAsync(Invitation invitation)
    {
        var user = invitation.User ?? await db.Users.FirstOrDefaultAsync(u => u.Id == invitation.UserId);
        if (user is null || string.IsNullOrWhiteSpace(user.Email)) return;

        var (rawToken, tokenHash) = GenerateToken();
        invitation.TokenHash = tokenHash;
        invitation.ExpiresAt = DateTime.UtcNow.AddHours(ExpiryHours);
        invitation.AutoResendCount += 1;
        invitation.SentAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await SendPurposedEmailAsync(invitation, user, rawToken);
    }

    private async Task<(Invitation invitation, string rawToken)> CreateInvitationAsync(int userId, int createdByAdminId, InvitationPurpose purpose, TimeSpan? expiry = null)
    {
        var (rawToken, tokenHash) = GenerateToken();
        var now = DateTime.UtcNow;
        var invitation = new Invitation
        {
            UserId = userId,
            TokenHash = tokenHash,
            Status = InvitationStatus.Sent,
            Purpose = purpose,
            CreatedAt = now,
            SentAt = now,
            ExpiresAt = now.Add(expiry ?? TimeSpan.FromHours(ExpiryHours)),
            CreatedByAdminId = createdByAdminId
        };
        db.Invitations.Add(invitation);
        await db.SaveChangesAsync();
        return (invitation, rawToken);
    }

    /// <summary>Construit et envoie l'email correspondant au Purpose de l'invitation, pour le jeton en clair donné.</summary>
    private async Task SendPurposedEmailAsync(Invitation invitation, AppUser user, string rawToken)
    {
        EmailTemplateType templateType;
        var adminName = "";
        var playerNames = "";

        switch (invitation.Purpose)
        {
            case InvitationPurpose.EmailVerification:
                templateType = EmailTemplateType.AccountVerification;
                break;

            case InvitationPurpose.AdminReactivation:
                templateType = EmailTemplateType.AccountReactivated;
                var reactivatedBy = await db.Users.FirstOrDefaultAsync(a => a.Id == invitation.CreatedByAdminId);
                adminName = reactivatedBy is not null ? $"{reactivatedBy.FirstName} {reactivatedBy.LastName}" : "un administrateur";
                break;

            case InvitationPurpose.PasswordReset:
                templateType = EmailTemplateType.PasswordReset;
                break;

            default: // FamilyInvite
                templateType = user.Role == UserRole.Consultant ? EmailTemplateType.ViewStats : EmailTemplateType.ManageChildren;
                var admin = user.CreatedByAdminId.HasValue
                    ? await db.Users.FirstOrDefaultAsync(a => a.Id == user.CreatedByAdminId.Value)
                    : null;
                adminName = admin is not null ? $"{admin.FirstName} {admin.LastName}" : "Un administrateur";
                var players = await db.Players
                    .Where(p => p.Users.Any(u => u.Id == user.Id))
                    .OrderBy(p => p.FirstName)
                    .ToListAsync();
                playerNames = players.Count > 0
                    ? string.Join(", ", players.Select(p => $"{p.FirstName} {p.LastName}"))
                    : "vos enfants";
                break;
        }

        var template = await db.EmailTemplates.FirstOrDefaultAsync(t => t.Type == templateType)
            ?? throw new InvalidOperationException("Modèle d'email introuvable.");

        var link = invitation.Purpose == InvitationPurpose.EmailVerification
            ? $"{BaseUrl}/confirmer/{rawToken}"
            : $"{BaseUrl}/invitation/{rawToken}";

        var placeholders = new Dictionary<string, string>
        {
            ["FirstName"] = user.FirstName,
            ["Username"] = user.Username,
            ["AdminName"] = adminName,
            ["PlayerNames"] = playerNames,
            ["InvitationLink"] = link,
            ["ExpirationDate"] = invitation.ExpiresAt.ToString("dd/MM/yyyy HH:mm")
        };

        var body = EmailService.RenderTemplate(template, placeholders, out var subject);
        await emailSender.SendAsync(user.Email!, subject, body);
    }

    private static (string rawToken, string tokenHash) GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Base64UrlEncode(bytes);
        return (rawToken, HashToken(rawToken));
    }

    private static string HashToken(string rawToken)
    {
        var hashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
