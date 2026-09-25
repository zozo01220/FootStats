using System.Security.Cryptography;
using System.Web;
using FootStats.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FootStats.Web.Data;

public enum MatchShareTokenState
{
    Valid,
    NotFound,
    Expired
}

public class MatchShareValidationResult
{
    public required MatchShareTokenState State { get; init; }
    public Tournament? Tournament { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>Génère et valide les liens de suivi en direct envoyés par WhatsApp : même mécanique de jeton que
/// <see cref="ShareInviteService"/> (empreinte SHA-256, jeton brut jamais stocké), mais sans création de compte —
/// juste une consultation en lecture seule, valable 48h, ouverte à quiconque possède le lien. Un lien couvre
/// l'évènement (tournoi/plateau) entier : tous ses matchs restent consultables, pas seulement celui en direct.</summary>
public class MatchShareLinkService(AppDbContext db, IConfiguration config, IHttpContextAccessor httpContextAccessor)
{
    private const int ExpiryHours = 48;

    /// <summary>Dérivée de la requête en cours quand elle existe (dev comme prod, quel que soit le domaine réel
    /// vu par nginx), sinon repliée sur App:BaseUrl (utile pour les services d'arrière-plan sans requête HTTP).</summary>
    private string BaseUrl
    {
        get
        {
            var request = httpContextAccessor.HttpContext?.Request;
            if (request is not null) return $"{request.Scheme}://{request.Host}";
            return (config["App:BaseUrl"] ?? "https://footstats.lok-izy.fr").TrimEnd('/');
        }
    }

    public async Task<(MatchShareLink Link, string RawToken)> CreateAsync(int tournamentId, int userId)
    {
        var (rawToken, tokenHash) = GenerateToken();
        var link = new MatchShareLink
        {
            TokenHash = tokenHash,
            TournamentId = tournamentId,
            CreatedByUserId = userId,
            ExpiresAt = DateTime.UtcNow.AddHours(ExpiryHours)
        };
        db.MatchShareLinks.Add(link);
        await db.SaveChangesAsync();
        return (link, rawToken);
    }

    public string BuildLink(string rawToken) => $"{BaseUrl}/suivi/{rawToken}";

    public string BuildWhatsAppShareUrl(string rawToken, string message)
    {
        var text = $"{message} {BuildLink(rawToken)}";
        return $"https://wa.me/?text={HttpUtility.UrlEncode(text)}";
    }

    /// <summary>Consultation en lecture seule, rappelée en boucle (via <see cref="LiveMatchNotifier"/>) tant que la
    /// page invité reste ouverte sur le même circuit Blazor. <c>AsNoTracking</c> est indispensable ici : sans lui,
    /// EF Core renvoie les instances déjà suivies par ce même <see cref="AppDbContext"/> (durée de vie = le circuit
    /// entier) sans rafraîchir leurs propriétés — le score affiché resterait figé sur sa première valeur lue.</summary>
    public async Task<MatchShareValidationResult> ValidateTokenAsync(string rawToken)
    {
        var tokenHash = HashToken(rawToken);
        var link = await db.MatchShareLinks
            .AsNoTracking()
            .Include(l => l.Tournament!).ThenInclude(t => t.Season!).ThenInclude(s => s.Player)
            .Include(l => l.Tournament!).ThenInclude(t => t.Matches).ThenInclude(m => m.Events)
            .FirstOrDefaultAsync(l => l.TokenHash == tokenHash);

        if (link is null) return new MatchShareValidationResult { State = MatchShareTokenState.NotFound };
        if (link.ExpiresAt <= DateTime.UtcNow)
        {
            return new MatchShareValidationResult { State = MatchShareTokenState.Expired, Tournament = link.Tournament, ExpiresAt = link.ExpiresAt };
        }
        return new MatchShareValidationResult { State = MatchShareTokenState.Valid, Tournament = link.Tournament, ExpiresAt = link.ExpiresAt };
    }

    private static (string rawToken, string tokenHash) GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Base64UrlEncode(bytes);
        return (rawToken, HashToken(rawToken));
    }

    private static string HashToken(string rawToken)
    {
        var hashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
