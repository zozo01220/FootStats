namespace FootStats.Web.Models;

public class Club
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? LogoPath { get; set; }

    /// <summary>Couleur principale du maillot (hex), utilisée pour le maillot avec numéro dans la carte joueur.</summary>
    public string JerseyColor { get; set; } = "#1D4ED8";

    /// <summary>Acronyme du club (ex: "USD" pour "US Divonnaise"), utilisé dans les pastilles quand pas de logo. Calculé automatiquement si non renseigné.</summary>
    public string? Acronym { get; set; }

    public List<Season> Seasons { get; set; } = [];
    public List<PlayerClub> Memberships { get; set; } = [];
    public List<AppUser> FavoritedByAdmins { get; set; } = [];
}
