using FootStats.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FootStats.Web.Data;

/// <summary>Résout l'utilisateur connecté et le périmètre de joueurs qu'il a le droit de voir/gérer.</summary>
public class CurrentUserService(AuthenticationStateProvider authStateProvider, AppDbContext db)
{
    private AppUser? _cached;

    public async Task<AppUser> GetCurrentUserAsync()
    {
        if (_cached is not null) return _cached;

        var state = await authStateProvider.GetAuthenticationStateAsync();
        var username = state.User.Identity?.Name
            ?? throw new InvalidOperationException("Aucun utilisateur authentifié.");

        _cached = await db.Users.Include(u => u.Players).FirstAsync(u => u.Username == username);
        return _cached;
    }

    /// <summary>Les joueurs visibles pour l'utilisateur courant : tous pour un SuperAdmin, sa propre famille pour un Admin, ses joueurs liés pour un User.</summary>
    public async Task<IQueryable<Player>> GetVisiblePlayersAsync()
    {
        var user = await GetCurrentUserAsync();
        return user.Role switch
        {
            UserRole.SuperAdmin => db.Players,
            UserRole.Admin => db.Players.Where(p => p.OwnerAdminId == user.Id),
            _ => db.Players.Where(p => p.Users.Any(u => u.Id == user.Id))
        };
    }

    /// <summary>Vrai si l'utilisateur courant peut gérer (créer/modifier/supprimer) les données de la famille (joueurs, saisons, événements, matchs, clubs).
    /// Admin et User (compte famille) peuvent gérer ; SuperAdmin (gère les administrateurs, pas les données) et Consultant (lecture seule) ne le peuvent pas.</summary>
    public async Task<bool> CanManageFamilyDataAsync()
    {
        var user = await GetCurrentUserAsync();
        return user.Role is UserRole.Admin or UserRole.User;
    }

    public async Task<bool> CanSeePlayerAsync(int playerId)
    {
        var players = await GetVisiblePlayersAsync();
        return await players.AnyAsync(p => p.Id == playerId);
    }

    /// <summary>Vrai si la session courante correspond toujours à un compte existant et actif. Utilisé par
    /// <c>MainLayout</c> pour couper une session en cours dès qu'un compte est supprimé ou révoqué pendant qu'il
    /// est connecté, plutôt que de le laisser planter sur la prochaine requête. Vrai aussi si personne n'est
    /// authentifié (rien à couper).</summary>
    public async Task<bool> IsCurrentSessionValidAsync()
    {
        var state = await authStateProvider.GetAuthenticationStateAsync();
        var username = state.User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return true;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is not { IsActive: true }) return false;

        // Le cookie porte la version de session en cours au moment de la connexion : un "sv" absent ou différent
        // signifie que le compte a été forcé à se déconnecter (bouton "Déconnecter") depuis.
        var claimVersion = state.User.FindFirst("sv")?.Value;
        if (!int.TryParse(claimVersion, out var version) || version != user.SessionVersion) return false;

        user.LastSeenAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>Fait échouer la prochaine vérification de session de ce compte (bouton "Déconnecter") sans toucher
    /// à <see cref="AppUser.IsActive"/> : contrairement à une révocation, l'accès reste entier, seule la session en
    /// cours est coupée.</summary>
    public async Task ForceLogoutAsync(int userId)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null) return;
        user.SessionVersion++;
        await db.SaveChangesAsync();
    }
}
