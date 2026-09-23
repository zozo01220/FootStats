using FootStats.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FootStats.Web.Data;

/// <summary>Vérifie périodiquement les invitations expirées : relance automatiquement une fois (nouveau jeton, +72h),
/// puis les marque définitivement comme expirées après ce second délai.</summary>
public class InvitationExpiryHostedService(IServiceProvider serviceProvider, ILogger<InvitationExpiryHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcessOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessOnceAsync(stoppingToken);
        }
    }

    private async Task ProcessOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var invitationService = scope.ServiceProvider.GetRequiredService<InvitationService>();

            var now = DateTime.UtcNow;
            var expiredCandidates = await db.Invitations
                .Include(i => i.User)
                .Where(i => i.Status == InvitationStatus.Sent && i.ExpiresAt < now)
                .ToListAsync(stoppingToken);

            foreach (var invitation in expiredCandidates)
            {
                // Les demandes de "mot de passe oublié" ne sont jamais relancées automatiquement : elles expirent
                // simplement après 30 minutes, l'utilisateur devant en redemander une s'il le souhaite.
                if (invitation.Purpose != InvitationPurpose.PasswordReset && invitation.AutoResendCount < 1)
                {
                    await invitationService.ResendWithFreshTokenAsync(invitation);
                }
                else
                {
                    invitation.Status = InvitationStatus.Expired;
                    await db.SaveChangesAsync(stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors du traitement des invitations expirées.");
        }
    }
}
