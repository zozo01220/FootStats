using System.Security.Claims;
using FootStats.Web.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;

namespace FootStats.Web.Data;

/// <summary>Logique partagée par Google et Microsoft : relie le compte externe à un AppUser existant (par email) ou en crée
/// un nouveau (rôle Admin, comme l'auto-inscription), puis remplace les claims du fournisseur par les nôtres avant que
/// le handler ne pose le cookie d'authentification.</summary>
public static class ExternalAuthEvents
{
    public static OAuthEvents Build() => new()
    {
        OnCreatingTicket = async context =>
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

            // Un compte réclamé via un lien de partage ("Partager" sur une carte saison ou depuis /users) passe
            // le jeton dans returnUrl (/rejoindre/{token}), posé comme RedirectUri par /account/external-login
            // avant le challenge OAuth : on le récupère ici, avant qu'il ne soit éventuellement écrasé plus bas.
            const string sharePrefix = "/rejoindre/";
            var returnUrl = context.Properties?.RedirectUri;
            var shareToken = !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith(sharePrefix, StringComparison.Ordinal)
                ? returnUrl[sharePrefix.Length..]
                : null;

            // Certains comptes Microsoft (notamment personnels/outlook.com selon les scopes) ne remplissent pas
            // toujours le claim email standard : on retombe sur les claims équivalents avant d'abandonner.
            var email = context.Principal?.FindFirstValue(ClaimTypes.Email)
                ?? context.Principal?.FindFirstValue("email")
                ?? context.Principal?.FindFirstValue(ClaimTypes.Upn)
                ?? context.Principal?.FindFirstValue("preferred_username");
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("NO_EMAIL: le fournisseur n'a transmis aucune adresse email exploitable.");
            }
            email = email.Trim();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower());
            var isNewUser = false;

            if (user is null)
            {
                var firstName = context.Principal?.FindFirstValue(ClaimTypes.GivenName)?.Trim();
                var lastName = context.Principal?.FindFirstValue(ClaimTypes.Surname)?.Trim();
                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    var fullName = context.Principal?.FindFirstValue(ClaimTypes.Name)?.Trim();
                    var parts = fullName?.Split(' ', 2) ?? [];
                    firstName = string.IsNullOrWhiteSpace(firstName) ? (parts.Length > 0 ? parts[0] : "Utilisateur") : firstName;
                    lastName = string.IsNullOrWhiteSpace(lastName) ? (parts.Length > 1 ? parts[1] : "FootStats") : lastName;
                }

                var existingUsernames = await db.Users.Select(u => u.Username).ToListAsync();
                var username = UsernameGenerator.Generate(firstName, lastName, existingUsernames);

                user = new AppUser
                {
                    Username = username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Role = UserRole.Admin,
                    IsActive = true,
                    HasLoggedInAt = DateTime.UtcNow
                };
                db.Users.Add(user);
                await db.SaveChangesAsync();
                isNewUser = true;
            }
            else if (!user.IsActive)
            {
                throw new InvalidOperationException("REVOKED: ce compte a été révoqué.");
            }
            else if (user.HasLoggedInAt is null)
            {
                user.HasLoggedInAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            if (shareToken is not null)
            {
                var shareInviteSvc = context.HttpContext.RequestServices.GetRequiredService<ShareInviteService>();
                // Compte tout juste créé pour ce lien : on applique directement son rôle/enfants (aucun conflit
                // possible, contrairement à un compte OAuth déjà existant où le rôle en place fait foi).
                if (isNewUser)
                {
                    await shareInviteSvc.ApplyToNewExternalUserAsync(shareToken, user.Id);
                }
                else
                {
                    await shareInviteSvc.AttachToExistingUserAsync(shareToken, user.Id);
                }
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.GivenName, user.FirstName),
                new(ClaimTypes.Surname, user.LastName),
                new(ClaimTypes.Role, user.Role.ToString()),
                new("sv", user.SessionVersion.ToString())
            };
            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

            if (shareToken is not null)
            {
                // Contrairement à l'auto-inscription classique, l'utilisateur est déjà connecté via OAuth : pas de
                // mot de passe à définir, on l'envoie directement dans l'app plutôt que vers /bienvenue.
                context.Properties!.RedirectUri = "/";
            }
            else if (isNewUser && user.Role == UserRole.Admin)
            {
                context.Properties!.RedirectUri = "/bienvenue";
            }
        },
        OnRemoteFailure = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ExternalAuthEvents");
            logger.LogError(context.Failure, "Échec de la connexion externe (Google/Microsoft).");

            var message = context.Failure?.Message ?? "";
            var errorCode = message.StartsWith("REVOKED", StringComparison.Ordinal) ? "revoked"
                : message.StartsWith("NO_EMAIL", StringComparison.Ordinal) ? "noemail"
                : "oauth";
            context.Response.Redirect($"/login?error={errorCode}");
            context.HandleResponse();
            return Task.CompletedTask;
        }
    };
}
