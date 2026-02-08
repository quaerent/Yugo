using System.Text.Json;
using System.Text.Json.Serialization;

namespace Yugo.Game;

public record LevelIdentity(
    string LocalPath,
    string? Title = null, // Transient for cloud levels
    int? CloudId = null,
    int? AuthorId = null
)
{
    [JsonIgnore]
    public bool IsCloud => CloudId.HasValue;
}

public static class PersistentSettings
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Yugo"
    );

    private static readonly string RecentLevelsFile = Path.Combine(AppDataPath, "recent_v4.json");

    static PersistentSettings()
    {
        if (!Directory.Exists(AppDataPath))
            Directory.CreateDirectory(AppDataPath);
    }

    public static List<LevelIdentity> LoadRecentLevels()
    {
        try
        {
            if (!File.Exists(RecentLevelsFile))
                return new List<LevelIdentity>();
            var json = File.ReadAllText(RecentLevelsFile);
            return JsonSerializer.Deserialize<List<LevelIdentity>>(json)
                ?? new List<LevelIdentity>();
        }
        catch
        {
            return new List<LevelIdentity>();
        }
    }

    public static void SaveRecentLevels(List<LevelIdentity> levels)
    {
        try
        {
            // When saving, we can clear Title for cloud levels to ensure they aren't stored
            var sanitized = levels.Select(l => l.IsCloud ? l with { Title = null } : l).ToList();
            var json = JsonSerializer.Serialize(sanitized);
            File.WriteAllText(RecentLevelsFile, json);
        }
        catch { }
    }
}
