namespace FootStats.Web.Models;

/// <summary>Réglages d'envoi d'emails via l'API Brevo (transactionnel). Table à une seule ligne.</summary>
public class SmtpSettings
{
    public int Id { get; set; }

    public string? SenderEmail { get; set; }
    public string? SenderDisplayName { get; set; }

    /// <summary>Clé API Brevo, chiffrée via IDataProtector. Jamais stockée en clair.</summary>
    public string? ApiKeyEncrypted { get; set; }

    public DateTime? LastTestedAt { get; set; }
    public string? LastTestResult { get; set; }
}
