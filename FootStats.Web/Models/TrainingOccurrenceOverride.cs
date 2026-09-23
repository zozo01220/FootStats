namespace FootStats.Web.Models;

/// <summary>Motif d'annulation d'une séance d'entraînement ponctuelle.</summary>
public enum TrainingCancellationReason
{
    Annule,
    TreveVacances,
    JoueurMalade
}

/// <summary>Exception ponctuelle à une règle <see cref="Training"/> récurrente : annule ou modifie
/// (heure, lieu) une seule occurrence (une date précise) sans toucher aux autres séances de la récurrence.</summary>
public class TrainingOccurrenceOverride
{
    public int Id { get; set; }

    public int TrainingId { get; set; }
    public Training? Training { get; set; }

    public DateOnly Date { get; set; }
    public bool IsCancelled { get; set; }
    public TrainingCancellationReason? CancellationReason { get; set; }

    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? Location { get; set; }
}
