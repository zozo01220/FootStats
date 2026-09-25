namespace FootStats.Web.Models;

/// <summary>Présence du joueur à un événement (occurrence d'entraînement, tournoi ou plateau).</summary>
public enum AttendanceStatus
{
    Present,
    Excused,
    Absent
}

/// <summary>Pointage de présence pour une séance d'entraînement précise (un <see cref="Training"/> + une date
/// d'occurrence). Une occurrence sans enregistrement n'entre pas dans le calcul du taux de présence : seules
/// les séances explicitement pointées comptent. La présence à un <see cref="Tournament"/> se pointe directement
/// sur l'entité (un seul événement, pas de récurrence), voir <see cref="Tournament.AttendanceStatus"/>.</summary>
public class TrainingAttendance
{
    public int Id { get; set; }

    public int TrainingId { get; set; }
    public Training? Training { get; set; }

    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; }
}
