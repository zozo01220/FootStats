using FootStats.Web.Models;

namespace FootStats.Web.Data;

/// <summary>Développe une règle d'entraînement récurrente (<see cref="Training"/>) en dates concrètes
/// sur une plage donnée, pour l'affichage dans le calendrier.</summary>
public static class TrainingOccurrenceHelper
{
    public static IEnumerable<DateOnly> Occurrences(Training training, DateOnly rangeStart, DateOnly rangeEnd)
    {
        var from = training.StartDate > rangeStart ? training.StartDate : rangeStart;
        var to = training.EndDate is { } end && end < rangeEnd ? end : rangeEnd;

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            if (Matches(training.Days, day.DayOfWeek)) yield return day;
        }
    }

    public static bool Matches(DaysOfWeekMask mask, DayOfWeek day) => mask.HasFlag(ToMask(day));

    public static DaysOfWeekMask ToMask(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => DaysOfWeekMask.Monday,
        DayOfWeek.Tuesday => DaysOfWeekMask.Tuesday,
        DayOfWeek.Wednesday => DaysOfWeekMask.Wednesday,
        DayOfWeek.Thursday => DaysOfWeekMask.Thursday,
        DayOfWeek.Friday => DaysOfWeekMask.Friday,
        DayOfWeek.Saturday => DaysOfWeekMask.Saturday,
        DayOfWeek.Sunday => DaysOfWeekMask.Sunday,
        _ => DaysOfWeekMask.None
    };

    public static readonly (DayOfWeek Day, DaysOfWeekMask Mask, string Short)[] WeekOrder =
    [
        (DayOfWeek.Monday, DaysOfWeekMask.Monday, "Lun"),
        (DayOfWeek.Tuesday, DaysOfWeekMask.Tuesday, "Mar"),
        (DayOfWeek.Wednesday, DaysOfWeekMask.Wednesday, "Mer"),
        (DayOfWeek.Thursday, DaysOfWeekMask.Thursday, "Jeu"),
        (DayOfWeek.Friday, DaysOfWeekMask.Friday, "Ven"),
        (DayOfWeek.Saturday, DaysOfWeekMask.Saturday, "Sam"),
        (DayOfWeek.Sunday, DaysOfWeekMask.Sunday, "Dim")
    ];

    public static string DaysLabel(DaysOfWeekMask mask)
    {
        var names = WeekOrder.Where(w => mask.HasFlag(w.Mask)).Select(w => w.Short).ToList();
        return names.Count == 0 ? "Aucun jour" : string.Join(" & ", names);
    }

    /// <summary>Motifs d'annulation proposés dans le sélecteur de la modale d'entraînement, avec le
    /// libellé, la description et la classe CSS (couleur) associés à chacun.</summary>
    public static readonly (TrainingCancellationReason Value, string Label, string Description, string CssClass)[] CancellationReasons =
    [
        (TrainingCancellationReason.Annule, "Annulé", "Séance supprimée exceptionnellement", "reason-annule"),
        (TrainingCancellationReason.TreveVacances, "Trêve / vacances", "Période sans entraînement", "reason-treve"),
        (TrainingCancellationReason.JoueurMalade, "Joueur malade", "Absence pour raison de santé", "reason-malade")
    ];

    public static string CancellationReasonLabel(TrainingCancellationReason reason) =>
        CancellationReasons.First(r => r.Value == reason).Label;

    public static string CancellationReasonCssClass(TrainingCancellationReason reason) =>
        CancellationReasons.First(r => r.Value == reason).CssClass;
}
