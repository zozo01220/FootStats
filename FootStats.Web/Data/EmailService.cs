using System.Net;
using System.Net.Http.Json;
using FootStats.Web.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace FootStats.Web.Data;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string bodyHtml);

    /// <summary>Envoie un email de test simple à l'adresse donnée, en utilisant les réglages enregistrés.</summary>
    Task SendTestAsync(string toEmail);
}

/// <summary>Envoi d'emails via l'API transactionnelle Brevo (HTTPS, pas de SMTP), à partir des réglages stockés
/// dans <see cref="SmtpSettings"/>. Habille chaque email avec la charte graphique FootStats (en-tête, bouton
/// d'action, pied de page) commune à tous les types d'email envoyés.</summary>
public class EmailService(AppDbContext db, IDataProtectionProvider dataProtectionProvider, HttpClient httpClient) : IEmailSender
{
    private const string ProtectorPurpose = "FootStats.SmtpAppPassword";
    private const string BrevoEndpoint = "https://api.brevo.com/v3/smtp/email";

    public async Task SendAsync(string toEmail, string subject, string bodyHtml)
    {
        var settings = await db.SmtpSettings.FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Les réglages d'envoi ne sont pas configurés.");

        if (string.IsNullOrWhiteSpace(settings.SenderEmail) || string.IsNullOrWhiteSpace(settings.ApiKeyEncrypted))
        {
            throw new InvalidOperationException("Les réglages sont incomplets (email expéditeur ou clé API manquants).");
        }

        var protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        var apiKey = protector.Unprotect(settings.ApiKeyEncrypted);

        var payload = new
        {
            sender = new { name = settings.SenderDisplayName ?? settings.SenderEmail, email = settings.SenderEmail },
            to = new[] { new { email = toEmail } },
            subject,
            htmlContent = bodyHtml
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, BrevoEndpoint)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("api-key", apiKey);
        request.Headers.Add("Accept", "application/json");

        using var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Brevo a refusé l'envoi ({(int)response.StatusCode}) : {body}");
        }
    }

    public async Task SendTestAsync(string toEmail)
    {
        var body = BuildBrandedEmail(
            eyebrow: "Email de test",
            introHtml: "<p style=\"margin: 0; font-size: 15px; line-height: 1.6; color: #141B18;\">Ceci est un email de test envoyé depuis les réglages FootStats. Si vous le recevez, l'envoi fonctionne correctement.</p>",
            username: null,
            buttonLabel: null,
            link: null,
            expirationDate: null,
            disclaimer: "Cet email de test peut être ignoré.");
        await SendAsync(toEmail, "Test FootStats", body);
    }

    public static string EncryptApiKey(IDataProtectionProvider dataProtectionProvider, string rawApiKey)
    {
        var protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        return protector.Protect(rawApiKey);
    }

    /// <summary>Remplace les placeholders {{Xxx}} dans le sujet et le corps (texte simple, potentiellement
    /// multi-paragraphes) du modèle, puis habille le résultat dans la charte graphique commune de l'email.</summary>
    public static string RenderTemplate(EmailTemplate template, Dictionary<string, string> placeholders, out string subject)
    {
        subject = ReplacePlaceholders(template.Subject, placeholders);
        var introText = ReplacePlaceholders(template.BodyHtml, placeholders);

        var introHtml = string.Join("", introText
            .Replace("\r\n", "\n")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(paragraph =>
                $"<p style=\"margin: 0 0 18px; font-size: 15px; line-height: 1.6; color: #141B18;\">{WebUtility.HtmlEncode(paragraph.Trim()).Replace("\n", "<br>")}</p>"));

        placeholders.TryGetValue("Username", out var username);
        placeholders.TryGetValue("InvitationLink", out var link);
        placeholders.TryGetValue("ExpirationDate", out var expirationDate);

        var (eyebrow, buttonLabel, disclaimer) = template.Type switch
        {
            EmailTemplateType.ManageChildren => ("Invitation à gérer les enfants", "Activer mon compte",
                "Si vous n'êtes pas à l'origine de cette demande, ignorez simplement cet email."),
            EmailTemplateType.ViewStats => ("Invitation à consulter les statistiques", "Activer mon compte",
                "Si vous n'êtes pas à l'origine de cette demande, ignorez simplement cet email."),
            EmailTemplateType.AccountReactivated => ("Compte réactivé", "Changer mon mot de passe",
                "Si vous n'êtes pas à l'origine de cette réactivation, contactez votre administrateur."),
            EmailTemplateType.AccountVerification => ("Confirmation d'inscription", "Confirmer mon email",
                "Si vous n'êtes pas à l'origine de cette inscription, ignorez simplement cet email."),
            EmailTemplateType.PasswordReset => ("Mot de passe oublié", "Choisir un nouveau mot de passe",
                "Si vous n'êtes pas à l'origine de cette demande, ignorez simplement cet email : votre mot de passe actuel reste valable."),
            EmailTemplateType.ShareInviteManage => ("Invitation à gérer les enfants", "Créer mon compte",
                "Si vous n'êtes pas à l'origine de cette demande, ignorez simplement cet email."),
            EmailTemplateType.ShareInviteConsultant => ("Invitation à consulter les statistiques", "Créer mon compte",
                "Si vous n'êtes pas à l'origine de cette demande, ignorez simplement cet email."),
            EmailTemplateType.ShareInviteWelcome => ("Bienvenue sur FootStats", "Me connecter",
                "Si vous n'êtes pas à l'origine de cette inscription, contactez la personne qui vous a invité(e)."),
            EmailTemplateType.MatchReminder => ("Rappel", null,
                "Ce rappel est envoyé automatiquement à toute la famille la veille de l'évènement."),
            _ => ("FootStats", "Ouvrir le lien", "Si vous n'êtes pas à l'origine de cette demande, ignorez simplement cet email.")
        };

        return BuildBrandedEmail(eyebrow, introHtml, username, buttonLabel, link, expirationDate, disclaimer);
    }

    /// <summary>Construit le HTML complet (habillage + contenu) d'un email FootStats : en-tête avec le logo,
    /// corps, encart nom d'utilisateur optionnel, bouton d'action optionnel avec lien de secours, note
    /// d'expiration et pied de page. Table-based + styles inline pour rester compatible avec les clients mail.</summary>
    private static string BuildBrandedEmail(
        string eyebrow, string introHtml, string? username, string? buttonLabel, string? link, string? expirationDate, string disclaimer)
    {
        var usernameBlock = string.IsNullOrWhiteSpace(username) ? "" : $"""
            <table role="presentation" cellpadding="0" cellspacing="0" style="margin: 0 0 26px; background: #F6F4EC; border-radius: 10px; border: 1px solid #E2DFD3;">
              <tr><td style="padding: 12px 18px; font-size: 12.5px; color: #726F66; font-weight: 700; text-transform: uppercase; letter-spacing: 0.04em;">Nom d'utilisateur</td></tr>
              <tr><td style="padding: 0 18px 14px; font-size: 16px; color: #141B18; font-weight: 700;">{WebUtility.HtmlEncode(username)}</td></tr>
            </table>
            """;

        var buttonBlock = string.IsNullOrWhiteSpace(link) || string.IsNullOrWhiteSpace(buttonLabel) ? "" : $"""
            <table role="presentation" cellpadding="0" cellspacing="0" style="margin: 0 auto 28px;">
              <tr><td style="border-radius: 10px; background: #E8B330;">
                <a href="{link}" style="display: inline-block; padding: 14px 36px; font-size: 15px; font-weight: 700; color: #0F3D24; text-decoration: none; border-radius: 10px;">{WebUtility.HtmlEncode(buttonLabel)}</a>
              </td></tr>
            </table>
            <p style="margin: 0 0 6px; font-size: 12.5px; line-height: 1.6; color: #726F66; text-align: center;">Le bouton ne fonctionne pas ? Copiez ce lien dans votre navigateur :</p>
            <p style="margin: 0 0 26px; font-size: 12px; line-height: 1.5; color: #C4941F; text-align: center; word-break: break-all;">{WebUtility.HtmlEncode(link)}</p>
            """;

        var expirationText = string.IsNullOrWhiteSpace(expirationDate) ? "" : $"Ce lien expirera le {expirationDate}. ";

        return $"""
            <!doctype html>
            <html lang="fr">
            <body style="margin: 0; font-family: 'Segoe UI', Arial, sans-serif; background: #E2DFD3;">
              <div style="width: 100%; background: #E2DFD3; padding: 32px 16px; box-sizing: border-box;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width: 560px; margin: 0 auto; background: #FFFFFF; border-radius: 16px; overflow: hidden;">
                  <tr>
                    <td style="background: linear-gradient(135deg, #2F7A4A, #0F3D24); padding: 32px 40px; text-align: center;">
                      <div style="font-family: Arial, sans-serif; font-size: 28px; font-weight: 800; letter-spacing: 0.02em; color: #ffffff;">FOOT<span style="color: #E8B330;">STATS</span></div>
                      <div style="margin-top: 6px; font-size: 11.5px; font-weight: 700; letter-spacing: 0.18em; text-transform: uppercase; color: rgba(255,255,255,0.65);">{WebUtility.HtmlEncode(eyebrow)}</div>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding: 40px 40px 8px;">
                      {introHtml}
                      {usernameBlock}
                      {buttonBlock}
                    </td>
                  </tr>
                  <tr><td style="padding: 0 40px;"><div style="border-top: 1px solid #E2DFD3;"></div></td></tr>
                  <tr>
                    <td style="padding: 20px 40px 30px;">
                      <p style="margin: 0; font-size: 12.5px; line-height: 1.6; color: #726F66; text-align: center;">{expirationText}{WebUtility.HtmlEncode(disclaimer)}</p>
                    </td>
                  </tr>
                  <tr>
                    <td style="background: #F6F4EC; padding: 22px 40px; text-align: center;">
                      <div style="font-family: Arial, sans-serif; font-size: 15px; font-weight: 800; letter-spacing: 0.03em; color: #726F66;">FOOT<span style="color: #C4941F;">STATS</span></div>
                      <p style="margin: 8px 0 0; font-size: 11.5px; color: #726F66;">Chaque match, chaque but, chaque saison.</p>
                    </td>
                  </tr>
                </table>
              </div>
            </body>
            </html>
            """;
    }

    private static string ReplacePlaceholders(string text, Dictionary<string, string> placeholders)
    {
        foreach (var (key, value) in placeholders)
        {
            text = text.Replace("{{" + key + "}}", value);
        }
        return text;
    }
}
