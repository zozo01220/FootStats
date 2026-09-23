namespace FootStats.Web.Models;

/// <summary>Classe le lien de parenté libre saisi sur un compte pour choisir silhouette et couleur d'affichage.</summary>
public enum RelationshipCategory
{
    Homme,
    Femme,
    Neutre
}

public static class RelationshipDisplay
{
    /// <summary>Suggestions proposées dans le formulaire ; le champ reste libre en base (peut contenir "Ami", "Cousin"...).</summary>
    public static readonly string[] Presets =
    [
        "Papa", "Maman", "Frère", "Sœur", "Grand-père", "Grand-mère", "Oncle", "Tante", "Ami(e)"
    ];

    private static readonly HashSet<string> Masculins = new(StringComparer.OrdinalIgnoreCase)
    {
        "Papa", "Père", "Frère", "Grand-père", "Oncle", "Beau-père", "Parrain", "Cousin"
    };

    private static readonly HashSet<string> Femininins = new(StringComparer.OrdinalIgnoreCase)
    {
        "Maman", "Mère", "Sœur", "Soeur", "Grand-mère", "Tante", "Belle-mère", "Marraine", "Cousine"
    };

    public static RelationshipCategory Category(string? relationship)
    {
        if (string.IsNullOrWhiteSpace(relationship)) return RelationshipCategory.Neutre;
        if (Masculins.Contains(relationship.Trim())) return RelationshipCategory.Homme;
        if (Femininins.Contains(relationship.Trim())) return RelationshipCategory.Femme;
        return RelationshipCategory.Neutre;
    }
}
