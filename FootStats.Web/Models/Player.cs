namespace FootStats.Web.Models;

public class Player
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateOnly BirthDate { get; set; }
    public required string Position { get; set; }
    public int? PreferredNumber { get; set; }
    public string? PhotoPath { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsCaptain { get; set; }

    /// <summary>Admin (parent) propriétaire de la fiche de ce joueur ; null pour les joueurs créés avant l'introduction des familles.</summary>
    public int? OwnerAdminId { get; set; }
    public AppUser? OwnerAdmin { get; set; }

    public List<PlayerClub> ClubHistory { get; set; } = [];
    public List<Season> Seasons { get; set; } = [];
    public List<AppUser> Users { get; set; } = [];
}
