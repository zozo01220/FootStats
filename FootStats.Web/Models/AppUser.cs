namespace FootStats.Web.Models;

public enum UserRole
{
    Admin = 0,
    User = 1,
    SuperAdmin = 2,
    Consultant = 3
}

public class AppUser
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;

    /// <summary>Adresse email de contact, requise pour envoyer une invitation.</summary>
    public string? Email { get; set; }

    /// <summary>Date/heure (UTC) de la toute première connexion réussie de ce compte ; jamais réécrite ensuite.</summary>
    public DateTime? HasLoggedInAt { get; set; }

    /// <summary>Dernier battement de session (mis à jour par le polling de <c>MainLayout</c>, ~20s) : sert à
    /// déterminer si le compte est actuellement en ligne et à afficher sa dernière activité.</summary>
    public DateTime? LastSeenAt { get; set; }

    /// <summary>Incrémenté pour forcer la fin de toutes les sessions actives de ce compte (bouton "Déconnecter"),
    /// sans le désactiver. Comparé à la revendication "sv" du cookie à chaque poll de session.</summary>
    public int SessionVersion { get; set; }

    /// <summary>Lien de parenté affiché sur la carte (ex: "Papa", "Tata", "Ami"). Libre, non contraint en base.</summary>
    public string? Relationship { get; set; }

    /// <summary>Admin qui a créé ce compte famille (Role=User) ; permet de le rattacher même s'il n'a encore aucun enfant lié.</summary>
    public int? CreatedByAdminId { get; set; }
    public AppUser? CreatedByAdmin { get; set; }

    public List<Player> Players { get; set; } = [];

    /// <summary>Clubs marqués "préféré" manuellement par cet admin, même sans joueur qui y est réellement inscrit.</summary>
    public List<Club> FavoriteClubs { get; set; } = [];
}
