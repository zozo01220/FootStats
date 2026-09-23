using FootStats.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FootStats.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration config)
    {
        await db.Database.MigrateAsync();

        if (!await db.Users.AnyAsync())
        {
            var username = config["AdminUser:Username"] ?? "admin";
            var password = config["AdminUser:Password"] ?? "ChangeMoi123!";

            db.Users.Add(new AppUser
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FirstName = "Admin",
                LastName = "",
                Role = UserRole.SuperAdmin
            });

            await db.SaveChangesAsync();

            if (config["AdminUser:Password"] is null)
            {
                Console.WriteLine($"[FootStats] Utilisateur super-administrateur créé avec le mot de passe par défaut '{password}'. " +
                                   "Définis AdminUser:Password (variable d'environnement AdminUser__Password) pour le changer.");
            }
        }

        // Ajoute les modèles manquants sans toucher à ceux déjà personnalisés (utile aussi quand un
        // nouveau type de modèle est introduit sur une base existante).
        var existingTypes = await db.EmailTemplates.Select(t => t.Type).ToListAsync();
        foreach (var type in Enum.GetValues<EmailTemplateType>())
        {
            if (existingTypes.Contains(type)) continue;
            var (subject, body) = DefaultTemplates.DefaultFor(type);
            db.EmailTemplates.Add(new EmailTemplate { Type = type, Subject = subject, BodyHtml = body });
        }
        await db.SaveChangesAsync();
    }
}

/// <summary>Contenu par défaut des modèles d'email d'invitation (français), utilisé pour le seed initial et le
/// bouton "Réinitialiser au modèle par défaut" de la page Réglages.</summary>
public static class DefaultTemplates
{
    public const string ManageChildrenSubject = "Invitation à gérer le profil de {{PlayerNames}} sur FootStats";

    public const string ManageChildrenBody = """
Bonjour {{FirstName}},

{{AdminName}} vous invite à gérer le suivi de {{PlayerNames}} sur FootStats.

Une fois votre mot de passe défini, vous pourrez ajouter des matchs, des statistiques et suivre les performances de {{PlayerNames}}.
""";

    public const string ViewStatsSubject = "Invitation à consulter les statistiques de {{PlayerNames}} sur FootStats";

    public const string ViewStatsBody = """
Bonjour {{FirstName}},

{{AdminName}} vous invite à suivre les statistiques de {{PlayerNames}} sur FootStats.

Vous pourrez consulter les matchs, résultats et statistiques de {{PlayerNames}} à tout moment.
""";

    public const string AccountReactivatedSubject = "Votre compte FootStats a été réactivé";

    public const string AccountReactivatedBody = """
Bonjour {{FirstName}},

Votre compte FootStats a été réactivé par {{AdminName}}.

Vous pouvez continuer à utiliser votre mot de passe actuel pour vous connecter : aucune action n'est nécessaire de votre part. Si vous préférez en choisir un nouveau, utilisez le bouton ci-dessous.
""";

    public const string AccountVerificationSubject = "Confirmez votre adresse email — FootStats";

    public const string AccountVerificationBody = """
Bonjour {{FirstName}},

Merci de vous être inscrit sur FootStats ! Confirmez votre adresse email pour activer votre compte.

Une fois votre compte confirmé, vous pourrez ajouter vos enfants, leurs clubs, saisons et matchs.
""";

    public const string PasswordResetSubject = "Réinitialisation de votre mot de passe FootStats";

    public const string PasswordResetBody = """
Bonjour {{FirstName}},

Vous avez demandé la réinitialisation du mot de passe de votre compte FootStats.

Cliquez sur le bouton ci-dessous pour choisir un nouveau mot de passe. Ce lien n'est valable que 30 minutes.
""";

    public const string ShareInviteManageSubject = "{{AdminName}} vous invite à gérer {{PlayerNames}} sur FootStats";

    public const string ShareInviteManageBody = """
Bonjour,

{{AdminName}} vous invite à gérer le suivi de {{PlayerNames}} sur FootStats.

Cliquez sur le bouton ci-dessous pour créer votre compte gratuitement. Vous pourrez ensuite ajouter des matchs, des statistiques et suivre les performances de {{PlayerNames}}.
""";

    public const string ShareInviteConsultantSubject = "{{AdminName}} vous invite à suivre {{PlayerNames}} sur FootStats";

    public const string ShareInviteConsultantBody = """
Bonjour,

{{AdminName}} vous invite à suivre les statistiques de {{PlayerNames}} sur FootStats.

Cliquez sur le bouton ci-dessous pour créer votre compte gratuitement. Vous pourrez consulter les matchs, résultats et statistiques de {{PlayerNames}} à tout moment.
""";

    public const string ShareInviteWelcomeSubject = "Bienvenue sur FootStats, {{FirstName}} !";

    public const string ShareInviteWelcomeBody = """
Bonjour {{FirstName}},

Votre compte FootStats a bien été créé.

Vous pouvez vous connecter dès maintenant avec le nom d'utilisateur ci-dessous et le mot de passe que vous venez de choisir.
""";

    public const string MatchReminderSubject = "Rappel : {{EventLabel}} de {{PlayerNames}} demain";

    public const string MatchReminderBody = """
Bonjour,

Petit rappel : {{EventLabel}} de {{PlayerNames}} a lieu demain, {{EventDate}} à {{EventTime}}, avec {{ClubName}}.

Lieu : {{Location}}.
""";

    public static List<EmailTemplate> All =>
    [
        new EmailTemplate
        {
            Type = EmailTemplateType.ManageChildren,
            Subject = ManageChildrenSubject,
            BodyHtml = ManageChildrenBody
        },
        new EmailTemplate
        {
            Type = EmailTemplateType.ViewStats,
            Subject = ViewStatsSubject,
            BodyHtml = ViewStatsBody
        },
        new EmailTemplate
        {
            Type = EmailTemplateType.AccountReactivated,
            Subject = AccountReactivatedSubject,
            BodyHtml = AccountReactivatedBody
        },
        new EmailTemplate
        {
            Type = EmailTemplateType.AccountVerification,
            Subject = AccountVerificationSubject,
            BodyHtml = AccountVerificationBody
        },
        new EmailTemplate
        {
            Type = EmailTemplateType.PasswordReset,
            Subject = PasswordResetSubject,
            BodyHtml = PasswordResetBody
        },
        new EmailTemplate
        {
            Type = EmailTemplateType.ShareInviteManage,
            Subject = ShareInviteManageSubject,
            BodyHtml = ShareInviteManageBody
        },
        new EmailTemplate
        {
            Type = EmailTemplateType.ShareInviteConsultant,
            Subject = ShareInviteConsultantSubject,
            BodyHtml = ShareInviteConsultantBody
        },
        new EmailTemplate
        {
            Type = EmailTemplateType.ShareInviteWelcome,
            Subject = ShareInviteWelcomeSubject,
            BodyHtml = ShareInviteWelcomeBody
        },
        new EmailTemplate
        {
            Type = EmailTemplateType.MatchReminder,
            Subject = MatchReminderSubject,
            BodyHtml = MatchReminderBody
        }
    ];

    public static (string Subject, string BodyHtml) DefaultFor(EmailTemplateType type) => type switch
    {
        EmailTemplateType.ManageChildren => (ManageChildrenSubject, ManageChildrenBody),
        EmailTemplateType.ViewStats => (ViewStatsSubject, ViewStatsBody),
        EmailTemplateType.AccountReactivated => (AccountReactivatedSubject, AccountReactivatedBody),
        EmailTemplateType.AccountVerification => (AccountVerificationSubject, AccountVerificationBody),
        EmailTemplateType.PasswordReset => (PasswordResetSubject, PasswordResetBody),
        EmailTemplateType.ShareInviteManage => (ShareInviteManageSubject, ShareInviteManageBody),
        EmailTemplateType.ShareInviteConsultant => (ShareInviteConsultantSubject, ShareInviteConsultantBody),
        EmailTemplateType.ShareInviteWelcome => (ShareInviteWelcomeSubject, ShareInviteWelcomeBody),
        EmailTemplateType.MatchReminder => (MatchReminderSubject, MatchReminderBody),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
