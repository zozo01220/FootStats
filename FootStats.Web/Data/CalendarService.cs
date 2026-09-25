using Microsoft.EntityFrameworkCore;

namespace FootStats.Web.Data;

/// <summary>Agrège, pour la fenêtre de dates demandée, les événements (tournois/plateaux + occurrences
/// d'entraînements récurrents) de tous les joueurs visibles pour l'utilisateur courant (voir
/// <see cref="CurrentUserService.GetVisiblePlayersAsync"/>) — la brique de données du Calendrier.</summary>
public class CalendarService(AppDbContext db, CurrentUserService currentUser)
{
    public async Task<List<CalendarEvent>> GetEventsAsync(DateOnly from, DateOnly to)
    {
        var visiblePlayers = await currentUser.GetVisiblePlayersAsync();
        var playerIds = await visiblePlayers.Select(p => p.Id).ToListAsync();
        if (playerIds.Count == 0) return [];

        var tournaments = await db.Tournaments
            .Include(t => t.Season).ThenInclude(s => s!.Player)
            .Include(t => t.Season).ThenInclude(s => s!.Club)
            .Include(t => t.Matches)
            .Where(t => playerIds.Contains(t.Season!.PlayerId) && t.Date >= from && t.Date <= to)
            .ToListAsync();

        var trainings = await db.Trainings
            .Include(t => t.Season).ThenInclude(s => s!.Player)
            .Include(t => t.Season).ThenInclude(s => s!.Club)
            .Where(t => playerIds.Contains(t.Season!.PlayerId))
            .ToListAsync();

        var trainingIds = trainings.Select(t => t.Id).ToList();
        var overridesByKey = await db.TrainingOccurrenceOverrides
            .Where(o => trainingIds.Contains(o.TrainingId) && o.Date >= from && o.Date <= to)
            .ToDictionaryAsync(o => (o.TrainingId, o.Date));

        var attendanceByKey = await db.TrainingAttendances
            .Where(a => trainingIds.Contains(a.TrainingId) && a.Date >= from && a.Date <= to)
            .ToDictionaryAsync(a => (a.TrainingId, a.Date));

        var events = new List<CalendarEvent>();

        foreach (var t in tournaments)
        {
            var season = t.Season!;
            events.Add(new CalendarEvent
            {
                Kind = CalendarEventKind.Tournament,
                Date = t.Date,
                StartTime = t.KickoffTime,
                Title = string.IsNullOrWhiteSpace(t.Name) ? t.EventType.ToString() : t.Name,
                EventType = t.EventType,
                Location = t.City,
                TeamNumber = t.TeamNumber,
                PlayerId = season.PlayerId,
                PlayerName = $"{season.Player!.FirstName} {season.Player.LastName}",
                ClubName = season.Club?.Name,
                ClubLogoPath = season.Club?.LogoPath,
                Category = season.Category,
                SeasonId = t.SeasonId,
                TournamentId = t.Id,
                MatchCount = t.Matches.Count,
                AttendanceStatus = t.AttendanceStatus
            });
        }

        foreach (var training in trainings)
        {
            var season = training.Season!;
            foreach (var date in TrainingOccurrenceHelper.Occurrences(training, from, to))
            {
                overridesByKey.TryGetValue((training.Id, date), out var occurrenceOverride);
                attendanceByKey.TryGetValue((training.Id, date), out var attendance);

                events.Add(new CalendarEvent
                {
                    Kind = CalendarEventKind.Training,
                    Date = date,
                    StartTime = occurrenceOverride?.StartTime ?? training.StartTime,
                    EndTime = occurrenceOverride?.EndTime ?? training.EndTime,
                    Title = "Entraînement",
                    Location = occurrenceOverride?.Location ?? training.Location,
                    PlayerId = season.PlayerId,
                    PlayerName = $"{season.Player!.FirstName} {season.Player.LastName}",
                    ClubName = season.Club?.Name,
                    ClubLogoPath = season.Club?.LogoPath,
                    Category = season.Category,
                    SeasonId = training.SeasonId,
                    TrainingId = training.Id,
                    IsCancelled = occurrenceOverride?.IsCancelled == true,
                    CancellationReason = occurrenceOverride?.IsCancelled == true ? occurrenceOverride.CancellationReason : null,
                    AttendanceStatus = attendance?.Status
                });
            }
        }

        return events
            .OrderBy(e => e.Date)
            .ThenBy(e => e.StartTime)
            .ToList();
    }
}
