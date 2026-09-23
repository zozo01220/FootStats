namespace FootStats.Web.Data;

public class MatchTrendPoint
{
    public string Opponent { get; set; } = "?";
    public int Goals { get; set; }
    public DateOnly Date { get; set; }
}

public class PositionShare
{
    public required string Position { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class SeasonGoalsPerMatch
{
    public required string Label { get; set; }
    public double GoalsPerMatch { get; set; }
}

public class BestEventRecord
{
    public int TournamentId { get; set; }
    public string? Name { get; set; }
    public required string City { get; set; }
    public DateOnly Date { get; set; }
    public int Goals { get; set; }
}

public class BestMatchRecord
{
    public string Opponent { get; set; } = "?";
    public DateOnly Date { get; set; }
    public int Goals { get; set; }
}

public class PlayerDashboardStats
{
    public int MatchCount { get; set; }
    public int Goals { get; set; }
    public double GoalsPerMatch { get; set; }
    public double StarterRate { get; set; }
    public int? MainTeamNumber { get; set; }
    public double MainTeamPercentage { get; set; }
    public List<MatchTrendPoint> GoalsTrend { get; set; } = [];
    public List<PositionShare> PositionShares { get; set; } = [];
    public List<SeasonGoalsPerMatch> SeasonComparison { get; set; } = [];
    public BestEventRecord? BestEvent { get; set; }
    public int BestScoringStreak { get; set; }
    public BestMatchRecord? BestMatch { get; set; }
}
