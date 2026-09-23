namespace FootStats.Web.Data;

/// <summary>Couleur de la pastille d'icône et du bouton de validation. Seul <see cref="Danger"/> donne un
/// bouton rouge : il est réservé aux actions destructives ou irréversibles.</summary>
public enum ConfirmTone
{
    Neutral,
    Warning,
    Danger
}

/// <summary>Pictogramme affiché dans la pastille. Choisi selon l'action, pas selon le ton.</summary>
public enum ConfirmIcon
{
    Question,
    Trash,
    Archive,
    Star,
    Refresh,
    Power,
    Link,
    UserOff,
    Calendar,
    Transfer
}

/// <summary>Contenu d'une boîte de confirmation FootStats (remplace le <c>confirm()</c> natif du navigateur).</summary>
public class ConfirmRequest
{
    public required string Title { get; init; }
    public required string Message { get; init; }

    public string ConfirmLabel { get; init; } = "Confirmer";
    public string CancelLabel { get; init; } = "Annuler";
    public ConfirmTone Tone { get; init; } = ConfirmTone.Neutral;
    public ConfirmIcon Icon { get; init; } = ConfirmIcon.Question;

    /// <summary>Carte rappelant l'objet concerné (joueur, club, séance…). Ignorée si <c>SubjectName</c> est vide.</summary>
    public string? SubjectName { get; init; }
    public string? SubjectMeta { get; init; }
    public string? SubjectInitials { get; init; }
    public string? SubjectImagePath { get; init; }
}

/// <summary>Pilote la boîte de confirmation unique montée dans le layout : <see cref="AskAsync"/> l'ouvre et
/// attend la réponse de l'utilisateur, comme le faisait <c>confirm()</c>.</summary>
public class ConfirmService
{
    private TaskCompletionSource<bool>? completion;

    public ConfirmRequest? Current { get; private set; }

    public event Action? OnChanged;

    public Task<bool> AskAsync(ConfirmRequest request)
    {
        // Une boîte déjà ouverte est abandonnée (réponse négative) plutôt que laissée en attente pour toujours.
        completion?.TrySetResult(false);

        Current = request;
        // Pas de RunContinuationsAsynchronously : la suite du code appelant doit reprendre en ligne, sur le
        // thread du dispatcher Blazor qui traite le clic, comme le faisait la reprise après JS.InvokeAsync.
        completion = new TaskCompletionSource<bool>();
        OnChanged?.Invoke();
        return completion.Task;
    }

    public void Resolve(bool confirmed)
    {
        var pending = completion;
        completion = null;
        Current = null;
        OnChanged?.Invoke();
        pending?.TrySetResult(confirmed);
    }
}
