using System.Globalization;
using FootStats.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FootStats.Web.Data;

/// <summary>Envoie un rappel par email la veille de chaque match/tournoi/plateau/concours, à tous les
/// utilisateurs du compte famille du joueur concerné (l'admin propriétaire, les comptes qu'il a créés,
/// et les comptes liés au joueur via un partage). Un évènement n'est jamais rappelé deux fois
/// (<see cref="Tournament.ReminderSent"/> / <see cref="EquestrianCompetition.ReminderSent"/>).</summary>
public class MatchReminderHostedService(IServiceProvider serviceProvider, ILogger<MatchReminderHostedService> logger) : BackgroundService
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

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
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            var tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

            var tournaments = await db.Tournaments
                .Include(t => t.Season).ThenInclude(s => s!.Player)
                .Include(t => t.Season).ThenInclude(s => s!.Club)
                .Where(t => !t.ReminderSent && t.Date == tomorrow)
                .ToListAsync(stoppingToken);

            foreach (var tournament in tournaments)
            {
                var label = tournament.EventType == EventType.Tournoi ? "Tournoi" : "Plateau";
                if (!string.IsNullOrWhiteSpace(tournament.Name)) label += $" « {tournament.Name} »";

                await SendEventReminderAsync(db, emailSender, tournament.Season, label,
                    tournament.Date, tournament.KickoffTime, tournament.City, stoppingToken);
                tournament.ReminderSent = true;
            }

            var competitions = await db.EquestrianCompetitions
                .Include(c => c.Season).ThenInclude(s => s!.Player)
                .Include(c => c.Season).ThenInclude(s => s!.Club)
                .Where(c => !c.ReminderSent && c.Date == tomorrow)
                .ToListAsync(stoppingToken);

            foreach (var competition in competitions)
            {
                var label = "Concours";
                if (!string.IsNullOrWhiteSpace(competition.Name)) label += $" « {competition.Name} »";

                await SendEventReminderAsync(db, emailSender, competition.Season, label,
                    competition.Date, null, competition.City, stoppingToken);
                competition.ReminderSent = true;
            }

            if (tournaments.Count > 0 || competitions.Count > 0)
            {
                await db.SaveChangesAsync(stoppingToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de l'envoi des rappels de match/tournoi.");
        }
    }

    private async Task SendEventReminderAsync(AppDbContext db, IEmailSender emailSender, Season? season,
        string eventLabel, DateOnly date, TimeOnly? time, string city, CancellationToken stoppingToken)
    {
        if (season?.Player is null || season.Player.OwnerAdminId is not { } adminId) return;

        var recipients = await GetFamilyMembersAsync(db, adminId, stoppingToken);
        if (recipients.Count == 0) return;

        var template = await db.EmailTemplates.FirstOrDefaultAsync(t => t.Type == EmailTemplateType.MatchReminder, stoppingToken);
        if (template is null) return;

        var placeholders = new Dictionary<string, string>
        {
            ["PlayerNames"] = $"{season.Player.FirstName} {season.Player.LastName}",
            ["EventLabel"] = eventLabel,
            ["EventDate"] = date.ToString("dddd d MMMM yyyy", French),
            ["EventTime"] = time is { } t ? t.ToString("HH:mm") : "heure à confirmer",
            ["Location"] = city,
            ["ClubName"] = season.Club?.Name ?? "Club inconnu"
        };

        var bodyHtml = EmailService.RenderTemplate(template, placeholders, out var subject);

        foreach (var recipient in recipients)
        {
            try
            {
                await emailSender.SendAsync(recipient.Email!, subject, bodyHtml);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Échec de l'envoi du rappel à {Email}", recipient.Email);
            }
        }
    }

    /// <summary>Tous les comptes du "foyer" d'un admin : lui-même, les comptes qu'il a créés (partage direct),
    /// et les comptes liés à l'un de ses joueurs via un lien de partage — chacun avec une adresse email valide.</summary>
    private static async Task<List<AppUser>> GetFamilyMembersAsync(AppDbContext db, int adminId, CancellationToken stoppingToken)
    {
        var members = await db.Users
            .Where(u => u.Id == adminId || u.CreatedByAdminId == adminId || u.Players.Any(p => p.OwnerAdminId == adminId))
            .Where(u => u.IsActive && u.Email != null && u.Email != "")
            .Distinct()
            .ToListAsync(stoppingToken);

        return members;
    }
}
