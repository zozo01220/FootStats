using FootStats.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FootStats.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<PlayerClub> PlayerClubs => Set<PlayerClub>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<MatchRecord> Matches => Set<MatchRecord>();
    public DbSet<Training> Trainings => Set<Training>();
    public DbSet<TrainingOccurrenceOverride> TrainingOccurrenceOverrides => Set<TrainingOccurrenceOverride>();
    public DbSet<TrainingAttendance> TrainingAttendances => Set<TrainingAttendance>();
    public DbSet<EquestrianCompetition> EquestrianCompetitions => Set<EquestrianCompetition>();
    public DbSet<EquestrianEntry> EquestrianEntries => Set<EquestrianEntry>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<SmtpSettings> SmtpSettings => Set<SmtpSettings>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<ShareInvite> ShareInvites => Set<ShareInvite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>()
            .HasOne(p => p.OwnerAdmin)
            .WithMany()
            .HasForeignKey(p => p.OwnerAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Season>()
            .HasOne(s => s.Club)
            .WithMany(c => c.Seasons)
            .HasForeignKey(s => s.ClubId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Season>()
            .HasOne(s => s.Player)
            .WithMany(p => p.Seasons)
            .HasForeignKey(s => s.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PlayerClub>()
            .HasOne(pc => pc.Player)
            .WithMany(p => p.ClubHistory)
            .HasForeignKey(pc => pc.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PlayerClub>()
            .HasOne(pc => pc.Club)
            .WithMany(c => c.Memberships)
            .HasForeignKey(pc => pc.ClubId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Tournament>()
            .HasOne(t => t.Season)
            .WithMany(s => s.Tournaments)
            .HasForeignKey(t => t.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MatchRecord>()
            .HasOne(m => m.Tournament)
            .WithMany(t => t.Matches)
            .HasForeignKey(m => m.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Training>()
            .HasOne(t => t.Season)
            .WithMany()
            .HasForeignKey(t => t.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TrainingOccurrenceOverride>()
            .HasOne(o => o.Training)
            .WithMany()
            .HasForeignKey(o => o.TrainingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TrainingOccurrenceOverride>()
            .HasIndex(o => new { o.TrainingId, o.Date })
            .IsUnique();

        modelBuilder.Entity<TrainingAttendance>()
            .HasOne(a => a.Training)
            .WithMany()
            .HasForeignKey(a => a.TrainingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TrainingAttendance>()
            .HasIndex(a => new { a.TrainingId, a.Date })
            .IsUnique();

        modelBuilder.Entity<EquestrianCompetition>()
            .HasOne(c => c.Season)
            .WithMany(s => s.EquestrianCompetitions)
            .HasForeignKey(c => c.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EquestrianEntry>()
            .HasOne(e => e.Competition)
            .WithMany(c => c.Entries)
            .HasForeignKey(e => e.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<AppUser>()
            .HasOne(u => u.CreatedByAdmin)
            .WithMany()
            .HasForeignKey(u => u.CreatedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppUser>()
            .HasMany(u => u.Players)
            .WithMany(p => p.Users)
            .UsingEntity(j => j.ToTable("UserPlayers"));

        modelBuilder.Entity<AppUser>()
            .HasMany(u => u.FavoriteClubs)
            .WithMany(c => c.FavoritedByAdmins)
            .UsingEntity(j => j.ToTable("AdminFavoriteClubs"));

        modelBuilder.Entity<Invitation>()
            .HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Invitation>()
            .HasOne(i => i.CreatedByAdmin)
            .WithMany()
            .HasForeignKey(i => i.CreatedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ShareInvite>()
            .HasMany(s => s.Players)
            .WithMany()
            .UsingEntity(j => j.ToTable("ShareInvitePlayers"));

        modelBuilder.Entity<ShareInvite>()
            .HasOne(s => s.CreatedByAdmin)
            .WithMany()
            .HasForeignKey(s => s.CreatedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ShareInvite>()
            .HasOne(s => s.ClaimedByUser)
            .WithMany()
            .HasForeignKey(s => s.ClaimedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
