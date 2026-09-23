namespace FootStats.Web.Data;

/// <summary>Génère un nom d'utilisateur unique (slug prénom.nom) à partir des existants, utilisé aussi bien pour
/// la création manuelle d'un compte (Users.razor) que pour l'auto-inscription publique (Register.razor).</summary>
public static class UsernameGenerator
{
    public static string Generate(string firstName, string lastName, List<string> existing)
    {
        string Slug(string value)
        {
            var normalized = value.Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
            var chars = normalized.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark);
            var result = new string(chars.ToArray()).Normalize(System.Text.NormalizationForm.FormC);
            return new string(result.Where(char.IsLetterOrDigit).ToArray());
        }

        var baseUsername = $"{Slug(firstName)}.{Slug(lastName)}";
        var username = baseUsername;
        var i = 1;
        while (existing.Contains(username, StringComparer.OrdinalIgnoreCase))
        {
            i++;
            username = $"{baseUsername}{i}";
        }
        return username;
    }
}
