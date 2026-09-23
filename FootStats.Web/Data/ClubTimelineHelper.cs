using FootStats.Web.Models;

namespace FootStats.Web.Data;

/// <summary>Résout le(s) club(s) d'un joueur à partir de l'historique réel (PlayerClub), pour que
/// les saisons et leurs évènements suivent les transferts au lieu de rester figés sur Season.ClubId.
///
/// Depuis qu'un joueur peut appartenir à plusieurs clubs en même temps (ex: foot + futsal), on ne peut plus
/// se contenter de prendre "le club actif à cette date" dans tout l'historique du joueur : un club parallèle
/// sans rapport avec cette saison pourrait s'y intercaler. On reconstruit donc la chaîne de transferts en
/// partant du club de la saison (Season.ClubId) et en ne suivant que les changements consécutifs et avérés
/// (fin d'une adhésion = début exact de la suivante) ; un club qui ne s'enchaîne pas ainsi est ignoré pour
/// cette saison, même s'il est actif au même moment.</summary>
public static class ClubTimelineHelper
{
    private static List<PlayerClub> BuildChain(IEnumerable<PlayerClub> memberships, Club? seasonClub)
    {
        var chain = new List<PlayerClub>();
        if (seasonClub is null) return chain;

        // Trié chronologiquement ; l'Id (ordre de création) départage les égalités de date, pour refléter
        // l'ordre réel de plusieurs changements de club faits le même jour (StartDate seule ne le peut pas).
        var list = memberships.OrderBy(m => m.StartDate).ThenBy(m => m.Id).ToList();

        // Un aller-retour vers le même club le même jour peut faire boucler la marche sur elle-même : on ne
        // suit chaque adhésion qu'une seule fois pour ne jamais tourner indéfiniment.
        var visitedIds = new HashSet<int>();
        var current = list.FirstOrDefault(m => m.ClubId == seasonClub.Id);
        while (current is not null && visitedIds.Add(current.Id))
        {
            chain.Add(current);
            if (current.EndDate is not { } end) break;
            current = list.FirstOrDefault(m => !visitedIds.Contains(m.Id) && m.StartDate == end);
        }
        return chain;
    }

    /// <summary>Le club du joueur à une date précise pour cette saison : suit la chaîne de transferts à partir
    /// du club de la saison, et se replie sur ce dernier si l'historique ne couvre pas la date (saisons
    /// existantes créées avant le bouton Transférer, ou historique incomplet). Quand plusieurs changements de
    /// club tombent le même jour, celui fait en dernier (Id le plus élevé) l'emporte.</summary>
    public static Club? ClubAt(IEnumerable<PlayerClub> memberships, DateOnly date, Club? seasonClub)
    {
        var chain = BuildChain(memberships, seasonClub);
        var match = chain
            .Where(m => m.StartDate <= date)
            .OrderByDescending(m => m.StartDate)
            .ThenByDescending(m => m.Id)
            .FirstOrDefault();
        return match?.Club ?? seasonClub;
    }

    /// <summary>Chronologie dédoublonnée des clubs de la saison (StartDate–EndDate, ou aujourd'hui si la saison
    /// est toujours en cours). Se replie sur le club statique de la saison quand l'historique PlayerClub ne
    /// couvre pas cette période.</summary>
    public static List<Club> SeasonTimeline(IEnumerable<PlayerClub> memberships, DateOnly? seasonStart, DateOnly? seasonEnd, Club? seasonClub)
    {
        var chain = BuildChain(memberships, seasonClub);
        if (chain.Count == 0)
        {
            return seasonClub is not null ? [seasonClub] : [];
        }

        var start = seasonStart ?? DateOnly.MinValue;
        var end = seasonEnd ?? DateOnly.FromDateTime(DateTime.Today);

        var overlapping = chain
            .Where(m => m.StartDate <= end && (m.EndDate == null || m.EndDate >= start))
            .OrderBy(m => m.StartDate).ThenBy(m => m.Id)
            .Select(m => m.Club)
            .Where(c => c is not null)
            .Select(c => c!)
            .ToList();

        if (overlapping.Count == 0)
        {
            return seasonClub is not null ? [seasonClub] : [];
        }

        var timeline = new List<Club>();
        foreach (var club in overlapping)
        {
            if (timeline.Count == 0 || timeline[^1].Id != club.Id) timeline.Add(club);
        }
        return timeline;
    }
}
