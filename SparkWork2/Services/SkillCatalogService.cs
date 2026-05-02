namespace SparkWork2.Services;

public class SkillCatalogService
{
    private readonly List<string> _horecaSkills = new()
    {
        "Accueil client",
        "Service en salle",
        "Prise de commande",
        "Encaissement",
        "Gestion des réservations",
        "Mise en place",
        "Nettoyage de salle",
        "Service au bar",
        "Préparation de boissons",
        "Connaissance des vins",
        "Cuisine chaude",
        "Cuisine froide",
        "Préparation des desserts",
        "Dressage des assiettes",
        "Plonge",
        "Respect des normes HACCP",
        "Gestion du stress",
        "Travail en équipe",
        "Ponctualité",
        "Flexibilité horaire",
        "Service rapide",
        "Relation client",
        "Gestion des stocks",
        "Réception de marchandises",
        "Organisation d'événements"
    };

    public IReadOnlyList<string> HorecaSkills => _horecaSkills;

    public List<string> ParseSkills(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new List<string>();

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string FormatSkills(IEnumerable<string> skills)
    {
        return string.Join(", ", skills
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    public bool IsKnownHorecaSkill(string skill)
    {
        return _horecaSkills.Any(x =>
            string.Equals(x, skill, StringComparison.OrdinalIgnoreCase));
    }
}
