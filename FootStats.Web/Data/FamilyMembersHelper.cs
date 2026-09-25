using FootStats.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FootStats.Web.Data;

/// <summary>Comptes du "foyer" d'un admin (lui-même, comptes qu'il a créés, comptes liés à l'un de ses joueurs par
/// partage), pour proposer une liste de destinataires au partage WhatsApp du suivi live (voir
/// <c>MatchShareLinkService</c>). Variante sans le filtre "email requis" du même calcul dans
/// <c>MatchReminderHostedService</c> : ici on affiche juste des noms, pas d'envoi d'email.</summary>
public static class FamilyMembersHelper
{
    public static async Task<List<AppUser>> GetForAdminAsync(AppDbContext db, int adminId)
    {
        return await db.Users
            .Where(u => u.Id == adminId || u.CreatedByAdminId == adminId || u.Players.Any(p => p.OwnerAdminId == adminId))
            .Where(u => u.IsActive)
            .Distinct()
            .ToListAsync();
    }
}
