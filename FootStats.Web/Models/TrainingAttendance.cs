namespace FootStats.Web.Models;

/// <summary>Présence du joueur à une occurrence précise d'un <see cref="Training"/> récurrent.</summary>
public enum TrainingAttendanceStatus
{
    Present,
    Excused,
    Absent
}

/// <summary>Pointage de présence pour une séance d'entraînement précise (un <see cref="Training"/> + une date
/// d'occurrence). Une occurrence sans enregistrement n'entre pas dans le calcul du taux de présence : seules
/// les séances explicitement pointées comptent.</summary>
public class TrainingAttendance
{
    public int Id { get; set; }

    public int TrainingId { get; set; }
    public Training? Training { get; set; }

    public DateOnly Date { get; set; }
    public TrainingAttendanceStatus Status { get; set; }
}
