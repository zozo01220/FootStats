namespace FootStats.Web.Models;

public enum Sport
{
    Football,
    Basketball,
    Equitation
}

public static class SportDisplay
{
    public static string Label(Sport sport) => sport switch
    {
        Sport.Football => "Football",
        Sport.Basketball => "Basketball",
        Sport.Equitation => "Équitation",
        _ => sport.ToString()
    };

    /// <summary>Mot utilisé pour désigner ce que marque le joueur dans un match (buts en foot, points en basket).</summary>
    public static string ScoreWord(Sport sport) => sport == Sport.Basketball ? "Points" : "Buts";
}
