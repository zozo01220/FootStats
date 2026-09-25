using System.Security.Cryptography;
using System.Web;
using FootStats.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FootStats.Web.Data;

public enum ShareInviteTokenState
{
    Valid,
    NotFound,
    Expired,
    AlreadyClaimed,
    Revoked
}

public class ShareInviteValidationResult
{
    public required ShareInviteTokenState State { get; init; }
    public ShareInvite? Invite { get; init; }
}

public enum ShareInviteAttachError
{
    RoleMismatch
}

/// <summary>Gère les liens de partage générés par un Admin (bouton "Partager" sur une carte saison ou sur la page
/// Utilisateurs) : le rôle et les enfants concernés sont figés à la création du lien, avant même qu'un compte
/// existe pour le destinataire. Miroir simplifié d'<see cref="InvitationService"/>, mais sans job de relance
/// automatique (l'email n'est envoyé qu'une fois, à la demande explicite de l'Admin).</summary>
public class ShareInviteService(AppDbContext db, IEmailSender emailSender, IConfiguration config, IHttpContextAccessor httpContextAccessor)
{
    private const int ExpiryHours = 72;

    /// <summary>Dérivée de la requête en cours quand elle existe (dev comme prod, quel que soit le domaine réel
    /// vu par nginx), sinon repliée sur App:BaseUrl (utile pour les services d'arrière-plan sans requête HTTP).</summary>
    private string BaseUrl
    {
        get
        {
            var request = httpContextAccessor.HttpContext?.Request;
            if (request is not null) return $"{request.Scheme}://{request.Host}";
            return (config["App:BaseUrl"] ?? "https://footstats.lok-izy.fr").TrimEnd('/');
        }
    }

    public async Task<(ShareInvite Invite, string RawToken)> CreateAsync(int adminId, UserRole role, List<int> playerIds, string? relationship)
    {
        var players = await db.Players.Where(p => playerIds.Contains(p.Id)).ToListAsync();
        var (rawToken, tokenHash) = GenerateToken();

        var invite = new ShareInvite
        {
            TokenHash = tokenHash,
            Role = role,
            Relationship = string.IsNullOrWhiteSpace(relationship) ? null : relationship.Trim(),
            Players = players,
            CreatedByAdminId = adminId,
            ExpiresAt = DateTime.UtcNow.AddHours(ExpiryHours)
        };
        db.ShareInvites.Add(invite);
        await db.SaveChangesAsync();
        return (invite, rawToken);
    }

    public string BuildLink(string rawToken) => $"{BaseUrl}/rejoindre/{rawToken}";

    public string BuildWhatsAppShareUrl(string rawToken, string adminName, string playerNames)
    {
        var text = $"{adminName} vous invite à suivre {playerNames} sur FootStats : {BuildLink(rawToken)}";
        return $"https://wa.me/?text={HttpUtility.UrlEncode(text)}";
    }

    /// <summary>Envoie l'email d'invitation via Brevo (même mécanisme que les invitations classiques), en choisissant
    /// le modèle selon le rôle du lien. Le destinataire n'a pas encore de compte : le placeholder {{FirstName}}
    /// n'est jamais renseigné, seuls {{AdminName}}, {{PlayerNames}}, {{InvitationLink}} et {{ExpirationDate}} le sont.</summary>
    public async Task SendEmailAsync(int shareInviteId, string recipientEmail, string rawToken)
    {
        var invite = await db.ShareInvites
            .Include(i => i.Players)
            .Include(i => i.CreatedByAdmin)
            .FirstOrDefaultAsync(i => i.Id == shareInviteId)
            ?? throw new InvalidOperationException("Lien de partage introuvable.");

        var templateType = invite.Role == UserRole.Consultant
            ? EmailTemplateType.ShareInviteConsultant
            : EmailTemplateType.ShareInviteManage;
        var template = await db.EmailTemplates.FirstOrDefaultAsync(t => t.Type == templateType)
            ?? throw new InvalidOperationException("Modèle d'email introuvable.");

        var adminName = invite.CreatedByAdmin is not null ? $"{invite.CreatedByAdmin.FirstName} {invite.CreatedByAdmin.LastName}" : "Un administrateur";
        var playerNames = PlayerNames(invite.Players);

        var placeholders = new Dictionary<string, string>
        {
            ["AdminName"] = adminName,
            ["PlayerNames"] = playerNames,
            ["InvitationLink"] = BuildLink(rawToken),
            ["ExpirationDate"] = invite.ExpiresAt.ToString("dd/MM/yyyy HH:mm")
        };

        var body = EmailService.RenderTemplate(template, placeholders, out var subject);
        await emailSender.SendAsync(recipientEmail, subject, body);

        invite.RecipientEmailHint = recipientEmail;
        await db.SaveChangesAsync();
    }

    public async Task<ShareInviteValidationResult> ValidateTokenAsync(string rawToken)
    {
        var invite = await FindByTokenAsync(rawToken);
        if (invite is null) return new ShareInviteValidationResult { State = ShareInviteTokenState.NotFound };
        if (invite.Status == ShareInviteStatus.Revoked) return new ShareInviteValidationResult { State = ShareInviteTokenState.Revoked, Invite = invite };
        if (invite.Status == ShareInviteStatus.Claimed) return new ShareInviteValidationResult { State = ShareInviteTokenState.AlreadyClaimed, Invite = invite };
        if (invite.Status == ShareInviteStatus.Expired || invite.ExpiresAt <= DateTime.UtcNow)
        {
            return new ShareInviteValidationResult { State = ShareInviteTokenState.Expired, Invite = invite };
        }
        return new ShareInviteValidationResult { State = ShareInviteTokenState.Valid, Invite = invite };
    }

    public async Task<AppUser> ClaimAsNewAccountAsync(
        string rawToken, string firstName, string lastName, string? email, string password)
    {
        var invite = await FindByTokenAsync(rawToken) ?? throw new InvalidOperationException("Lien de partage introuvable.");

        if (!string.IsNullOrWhiteSpace(email))
        {
            var wantedEmail = email.Trim();
            var emailTaken = await db.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == wantedEmail.ToLower());
            if (emailTaken)
            {
                throw new InvalidOperationException("EMAIL_TAKEN: cette adresse email est déjà utilisée par un compte existant. Connectez-vous plutôt avec ce compte pour y ajouter cet accès.");
            }
        }

        var existingUsernames = await db.Users.Select(u => u.Username).ToListAsync();
        var username = UsernameGenerator.Generate(firstName, lastName, existingUsernames);

        var user = new AppUser
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            Relationship = invite.Relationship,
            Role = invite.Role,
            CreatedByAdminId = invite.CreatedByAdminId,
            Players = invite.Players
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        invite.Status = ShareInviteStatus.Claimed;
        invite.ClaimedByUserId = user.Id;
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            try
            {
                await SendWelcomeEmailAsync(user);
            }
            catch
            {
                // Non bloquant : le compte est déjà créé et utilisable, seul le rappel par email échoue
                // (Brevo mal configuré, etc.) — cohérent avec le traitement des erreurs d'envoi dans Register.razor.
            }
        }

        return user;
    }

    /// <summary>Confirmation envoyée juste après la création d'un compte via un lien de partage : rappelle le nom
    /// d'utilisateur, avec un lien direct vers la connexion.</summary>
    private async Task SendWelcomeEmailAsync(AppUser user)
    {
        var template = await db.EmailTemplates.FirstOrDefaultAsync(t => t.Type == EmailTemplateType.ShareInviteWelcome)
            ?? throw new InvalidOperationException("Modèle d'email introuvable.");

        var placeholders = new Dictionary<string, string>
        {
            ["FirstName"] = user.FirstName,
            ["Username"] = user.Username,
            ["InvitationLink"] = $"{BaseUrl}/login"
        };

        var body = EmailService.RenderTemplate(template, placeholders, out var subject);
        await emailSender.SendAsync(user.Email!, subject, body);
    }

    /// <summary>Applique le périmètre d'un lien de partage à un compte tout juste créé par une connexion externe
    /// (Google/Microsoft) initiée depuis <c>/rejoindre/{token}</c> : contrairement à un compte déjà existant, il n'y
    /// a aucun rôle préalable à respecter, donc on écrase directement Role/Relationship/Players plutôt que de
    /// vérifier une correspondance. Ne fait rien si le lien n'est plus valable (expiré/révoqué/déjà réclamé).</summary>
    public async Task ApplyToNewExternalUserAsync(string rawToken, int newUserId)
    {
        var invite = await FindByTokenAsync(rawToken);
        if (invite is null || invite.Status != ShareInviteStatus.Pending || invite.ExpiresAt <= DateTime.UtcNow) return;

        var user = await db.Users.Include(u => u.Players).FirstOrDefaultAsync(u => u.Id == newUserId);
        if (user is null) return;

        user.Role = invite.Role;
        user.Relationship = invite.Relationship;
        user.Players = invite.Players;
        user.CreatedByAdminId = invite.CreatedByAdminId;

        invite.Status = ShareInviteStatus.Claimed;
        invite.ClaimedByUserId = user.Id;
        await db.SaveChangesAsync();
    }

    /// <summary>Rattache le périmètre d'un second lien de partage (ex. un autre enfant de la même famille) à un
    /// compte déjà créé via un précédent lien. N'ajoute que les joueurs (union, sans doublon) ; refuse si le rôle
    /// du lien diffère de celui du compte plutôt que de changer silencieusement les droits.</summary>
    public async Task<ShareInviteAttachError?> AttachToExistingUserAsync(string rawToken, int existingUserId)
    {
        var invite = await FindByTokenAsync(rawToken) ?? throw new InvalidOperationException("Lien de partage introuvable.");
        var user = await db.Users.Include(u => u.Players).FirstOrDefaultAsync(u => u.Id == existingUserId)
            ?? throw new InvalidOperationException("Utilisateur introuvable.");

        if (user.Role != invite.Role)
        {
            return ShareInviteAttachError.RoleMismatch;
        }

        var existingPlayerIds = user.Players.Select(p => p.Id).ToHashSet();
        foreach (var player in invite.Players.Where(p => !existingPlayerIds.Contains(p.Id)))
        {
            user.Players.Add(player);
        }

        invite.Status = ShareInviteStatus.Claimed;
        invite.ClaimedByUserId = user.Id;
        await db.SaveChangesAsync();
        return null;
    }

    public async Task RevokeAsync(int shareInviteId)
    {
        var invite = await db.ShareInvites.FindAsync(shareInviteId);
        if (invite is null) return;
        invite.Status = ShareInviteStatus.Revoked;
        await db.SaveChangesAsync();
    }

    public static string PlayerNames(IEnumerable<Player> players)
    {
        var names = players.Select(p => $"{p.FirstName} {p.LastName}").ToList();
        return names.Count > 0 ? string.Join(", ", names) : "vos enfants";
    }

    private async Task<ShareInvite?> FindByTokenAsync(string rawToken)
    {
        var tokenHash = HashToken(rawToken);
        return await db.ShareInvites
            .Include(i => i.Players)
            .Include(i => i.CreatedByAdmin)
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash);
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
