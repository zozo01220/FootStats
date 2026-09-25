namespace FootStats.Web.Data;

/// <summary>Fait circuler les mises à jour d'un match en direct entre circuits Blazor Server indépendants
/// (fiche match de l'admin, page famille sans connexion) : chaque circuit vit dans son propre scope DI, donc rien
/// ne les relie sans un service singleton partagé (voir CLAUDE.md, "Render modes : le piège principal").
/// Notifie par tournoi (pas par match) : un ajout ou une suppression de match doit aussi rafraîchir les circuits
/// qui suivent ce tournoi, alors même qu'ils n'ont encore aucune trace de ce <c>MatchRecord.Id</c> en mémoire.</summary>
public class LiveMatchNotifier
{
    public event Action<int>? TournamentChanged;

    public void NotifyTournamentChanged(int tournamentId) => TournamentChanged?.Invoke(tournamentId);
}
