namespace FootStats.Web.Models;

[Flags]
public enum DaysOfWeekMask
{
    None = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64
}

/// <summary>Règle d'entraînement récurrente pour une saison (ex: "mardi et vendredi, 18h-19h30").
/// Les séances individuelles ne sont pas stockées : elles sont générées à la volée pour le calendrier
/// à partir de cette règle (voir <see cref="TrainingOccurrenceHelper"/>).</summary>
public class Training
{
    public int Id { get; set; }

    public int SeasonId { get; set; }
    public Season? Season { get; set; }

    public DaysOfWeekMask Days { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public required string Location { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
