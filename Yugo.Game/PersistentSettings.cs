using System.Text.Json;

namespace Yugo.Game;

public record LevelIdentity(
    string Title,
    string LocalPath,
    int? CloudId = null,
    int? AuthorId = null
)
{
    public bool IsCloud => CloudId.HasValue;
}

public static class PersistentSettings
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Yugo"
    );

    private static readonly string RecentLevelsFile = Path.Combine(AppDataPath, "recent_v3.json");

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
            var json = JsonSerializer.Serialize(levels);
            File.WriteAllText(RecentLevelsFile, json);
        }
        catch { }
    }
}
