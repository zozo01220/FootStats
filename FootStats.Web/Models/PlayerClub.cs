namespace FootStats.Web.Models;

/// <summary>Période d'appartenance d'un joueur à un club (un enfant peut changer de club).</summary>
public class PlayerClub
{
    public int Id { get; set; }

    public int PlayerId { get; set; }
    public Player? Player { get; set; }

    public int ClubId { get; set; }
    public Club? Club { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
