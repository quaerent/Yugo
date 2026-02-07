using System.Text.Json;

namespace Yugo.Game;

public static class PersistentSettings
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Yugo"
    );

    private static readonly string RecentLevelsFile = Path.Combine(AppDataPath, "recent.json");

    static PersistentSettings()
    {
        if (!Directory.Exists(AppDataPath))
        {
            Directory.CreateDirectory(AppDataPath);
        }
    }

    public static List<string> LoadRecentLevels()
    {
        try
        {
            if (!File.Exists(RecentLevelsFile))
                return new List<string>();

            var json = File.ReadAllText(RecentLevelsFile);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    public static void SaveRecentLevels(List<string> levels)
    {
        try
        {
            var json = JsonSerializer.Serialize(levels);
            File.WriteAllText(RecentLevelsFile, json);
        }
        catch
        {
            // Silently fail for now
        }
    }
}
