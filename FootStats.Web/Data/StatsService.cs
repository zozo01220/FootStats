using FootStats.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FootStats.Web.Data;

public class StatsService(AppDbContext db)
{
    public async Task<SeasonStats> GetSeasonStatsAsync(int seasonId)
    {
        var matches = await db.Matches
            .Include(m => m.Tournament)
            .Where(m => m.Tournament!.SeasonId == seasonId)
            .ToListAsync();

        var tournaments = await db.Tournaments
            .Where(t => t.SeasonId == seasonId)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

        var matchCount = matches.Count;
        var playerGoals = matches.Sum(m => m.PlayerGoals);

        var tournamentSummaries = tournaments.Select(t =>
        {
            var tMatches = matches.Where(m => m.TournamentId == t.Id).ToList();
            return new TournamentSummary
            {
                TournamentId = t.Id,
                Name = t.Name,
                EventType = t.EventType,
                City = t.City,
                Date = t.Date,
                MatchCount = tMatches.Count,
                PlayerGoals = tMatches.Sum(m => m.PlayerGoals),
                TeamGoalsFor = tMatches.Sum(m => m.TeamGoalsFor),
                TeamNumber = tMatches
                    .GroupBy(m => m.TeamNumber)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .Select(g => (int?)g.Key)
                    .FirstOrDefault()
            };
        }).ToList();

        return new SeasonStats
        {
            MatchCount = matchCount,
            TournamentCount = tournaments.Count,
            PlayerGoals = playerGoals,
            PlayerAssists = matches.Sum(m => m.PlayerAssists),
            TeamGoalsFor = matches.Sum(m => m.TeamGoalsFor),
            GoalsPerMatch = matchCount == 0 ? 0 : Math.Round(playerGoals / (double)matchCount, 2),
            Tournaments = tournamentSummaries
        };
    }

    /// <summary>Taux de présence aux entraînements de la saison, calculé sur toutes les occurrences passées
    /// (jusqu'à aujourd'hui) de chaque règle <see cref="Training"/> de la saison. <c>null</c> si la saison n'a
    /// aucun entraînement configuré.</summary>
    public async Task<SeasonAttendanceStats?> GetSeasonAttendanceAsync(int seasonId)
    {
        var trainings = await db.Trainings.Where(t => t.SeasonId == seasonId).ToListAsync();
        if (trainings.Count == 0) return null;

        var trainingIds = trainings.Select(t => t.Id).ToList();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var overridesByKey = await db.TrainingOccurrenceOverrides
            .Where(o => trainingIds.Contains(o.TrainingId))
            .ToDictionaryAsync(o => (o.TrainingId, o.Date));

        var attendanceByKey = await db.TrainingAttendances
            .Where(a => trainingIds.Contains(a.TrainingId))
            .ToDictionaryAsync(a => (a.TrainingId, a.Date));

        var recorded = 0;
        var present = 0;
        foreach (var training in trainings)
        {
            if (training.StartDate > today) continue;
            foreach (var date in TrainingOccurrenceHelper.Occurrences(training, training.StartDate, today))
            {
                overridesByKey.TryGetValue((training.Id, date), out var occurrenceOverride);
                if (occurrenceOverride?.IsCancelled == true) continue;

                if (attendanceByKey.TryGetValue((training.Id, date), out var attendance))
                {
                    recorded++;
                    if (attendance.Status == TrainingAttendanceStatus.Present) present++;
                }
            }
        }

        return new SeasonAttendanceStats
        {
            RecordedCount = recorded,
            PresentCount = present,
            Percentage = recorded == 0 ? null : Math.Round(present * 100.0 / recorded, 0)
        };
    }

    /// <summary>Tableau de bord d'un joueur (page d'accueil) : chiffres clés, graphiques et records, calculés
    /// à la volée comme <see cref="GetSeasonStatsAsync"/>. Le comparatif entre saisons et les records portent
    /// toujours sur la carrière complète du joueur ; seuls les chiffres clés, la courbe et la répartition par
    /// poste respectent <paramref name="allSeasons"/> (saison en cours vs cumul).</summary>
    public async Task<PlayerDashboardStats?> GetPlayerDashboardAsync(int playerId, bool allSeasons)
    {
        var seasons = await db.Seasons
            .Where(s => s.PlayerId == playerId)
            .OrderBy(s => s.StartDate)
            .ToListAsync();
        if (seasons.Count == 0) return null;

        var seasonIds = seasons.Select(s => s.Id).ToHashSet();
        var allMatches = await db.Matches
            .Include(m => m.Tournament)
            .Where(m => seasonIds.Contains(m.Tournament!.SeasonId))
            .OrderBy(m => m.Tournament!.Date)
            .ToListAsync();
        if (allMatches.Count == 0) return null;

        var currentSeason = seasons.FirstOrDefault(s => !s.IsArchived) ?? seasons[^1];
        var scopedMatches = allSeasons
            ? allMatches
            : allMatches.Where(m => m.Tournament!.SeasonId == currentSeason.Id).ToList();

        var matchCount = scopedMatches.Count;
        var goals = scopedMatches.Sum(m => m.PlayerGoals);

        var positionShares = scopedMatches
            .GroupBy(m => string.IsNullOrWhiteSpace(m.Position) ? "Non renseigné" : m.Position!)
            .Select(g => new PositionShare
            {
                Position = g.Key,
                Count = g.Count(),
                Percentage = matchCount == 0 ? 0 : Math.Round(g.Count() * 100.0 / matchCount, 0)
            })
            .OrderByDescending(p => p.Count)
            .ToList();

        var mainTeam = scopedMatches
            .GroupBy(m => m.TeamNumber)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .FirstOrDefault();

        var seasonComparison = seasons
            .Select(s =>
            {
                var sMatches = allMatches.Where(m => m.Tournament!.SeasonId == s.Id).ToList();
                return new SeasonGoalsPerMatch
                {
                    Label = s.Label,
                    GoalsPerMatch = sMatches.Count == 0 ? 0 : Math.Round(sMatches.Sum(m => m.PlayerGoals) / (double)sMatches.Count, 2)
                };
            })
            .ToList();

        BestEventRecord? bestEvent = allMatches
            .GroupBy(m => m.TournamentId)
            .Select(g => new BestEventRecord
            {
                TournamentId = g.Key,
                Name = g.First().Tournament!.Name,
                City = g.First().Tournament!.City,
                Date = g.First().Tournament!.Date,
                Goals = g.Sum(m => m.PlayerGoals)
            })
            .Where(e => e.Goals > 0)
            .OrderByDescending(e => e.Goals)
            .FirstOrDefault();

        var bestMatchRecord = allMatches
            .Where(m => m.PlayerGoals > 0)
            .OrderByDescending(m => m.PlayerGoals)
            .ThenByDescending(m => m.Tournament!.Date)
            .FirstOrDefault();
        BestMatchRecord? bestMatch = bestMatchRecord is null ? null : new BestMatchRecord
        {
            Opponent = bestMatchRecord.Opponent ?? "?",
            Date = bestMatchRecord.Tournament!.Date,
            Goals = bestMatchRecord.PlayerGoals
        };

        var bestStreak = 0;
        var currentStreak = 0;
        foreach (var m in allMatches)
        {
            if (m.PlayerGoals > 0)
            {
                currentStreak++;
                bestStreak = Math.Max(bestStreak, currentStreak);
            }
            else
            {
                currentStreak = 0;
            }
        }

        return new PlayerDashboardStats
        {
            MatchCount = matchCount,
            Goals = goals,
            GoalsPerMatch = matchCount == 0 ? 0 : Math.Round(goals / (double)matchCount, 2),
            StarterRate = matchCount == 0 ? 0 : Math.Round(scopedMatches.Count(m => m.IsStarter) * 100.0 / matchCount, 0),
            MainTeamNumber = mainTeam?.Key,
            MainTeamPercentage = mainTeam is null || matchCount == 0 ? 0 : Math.Round(mainTeam.Count() * 100.0 / matchCount, 0),
            GoalsTrend = scopedMatches.TakeLast(7).Select(m => new MatchTrendPoint
            {
                Opponent = m.Opponent ?? "?",
                Goals = m.PlayerGoals,
                Date = m.Tournament!.Date
            }).ToList(),
            PositionShares = positionShares,
            SeasonComparison = seasonComparison,
            BestEvent = bestEvent,
            BestScoringStreak = bestStreak,
            BestMatch = bestMatch
        };
    }
}
