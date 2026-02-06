using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Yugo.Core.Entities;
using Yugo.Core.Game;
using Yugo.Core.Rules;

namespace Yugo.Core.Serialization;

public static class LevelXml
{
    public static Level Load(string path)
    {
        using var stream = File.OpenRead(path);
        return Load(stream, null);
    }

    public static Level Load(
        string path,
        Func<int, int, IReadOnlyList<IMergeRule>, IReadOnlyList<IWinRule>, Level> factory
    )
    {
        using var stream = File.OpenRead(path);
        return Load(stream, factory);
    }

    public static Level Load(
        Stream stream,
        Func<int, int, IReadOnlyList<IMergeRule>, IReadOnlyList<IWinRule>, Level>? factory
    )
    {
        var document = XDocument.Load(stream);
        if (document.Root == null)
            throw new InvalidOperationException("Level XML must have a root element.");
        return Parse(document.Root, factory);
    }

    public static Level Parse(
        XElement root,
        Func<int, int, IReadOnlyList<IMergeRule>, IReadOnlyList<IWinRule>, Level>? factory
    )
    {
        if (!root.Name.LocalName.Equals("level", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Root element must be <level>.");

        var width = ReadRequiredInt(root, "width");
        var height = ReadRequiredInt(root, "height");
        var gravity = ParseDirection(root.Attribute("gravity")?.Value ?? "Down");

        var mergeRules = new List<IMergeRule>();
        var winRules = new List<IWinRule>();
        ParseRules(root.Element("rules"), mergeRules, winRules);

        var levelFactory = factory ?? DefaultFactory;
        var level = levelFactory(width, height, mergeRules, winRules);
        level.Gravity = gravity;

        var entitiesElement =
            root.Element("entities")
            ?? throw new InvalidOperationException("Level XML must contain <entities>.");

        foreach (var element in entitiesElement.Elements())
        {
            var name = element.Name.LocalName.ToLowerInvariant();
            switch (name)
            {
                case "wall":
                    {
                        var cells = ParseCells(element);
                        var wall = new Wall(level, cells);
                        level.AddEntity(wall);
                        break;
                    }
                case "movable":
                    {
                        var clusterId = ReadRequiredInt(element, "cluster", "clusterId");
                        var cells = ParseCells(element);
                        var movable = new Movable(level, cells, clusterId);
                        level.AddEntity(movable);
                        break;
                    }
                default:
                    throw new InvalidOperationException(
                        $"Unknown entity type '{element.Name.LocalName}'."
                    );
            }
        }

        return level;
    }

    private static Level DefaultFactory(
        int width,
        int height,
        IReadOnlyList<IMergeRule> mergeRules,
        IReadOnlyList<IWinRule> winRules
    )
    {
        return new Level(width, height, mergeRules, winRules);
    }

    private static void ParseRules(
        XElement? rulesElement,
        List<IMergeRule> mergeRules,
        List<IWinRule> winRules
    )
    {
        if (rulesElement == null)
            return;

        foreach (var element in rulesElement.Elements())
        {
            var name = element.Name.LocalName.ToLowerInvariant();
            switch (name)
            {
                case "merge":
                    mergeRules.Add(CreateMergeRule(ReadRequiredString(element, "type")));
                    break;
                case "win":
                    winRules.Add(CreateWinRule(ReadRequiredString(element, "type")));
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unknown rule element '{element.Name.LocalName}'."
                    );
            }
        }
    }

    private static IMergeRule CreateMergeRule(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "movable" => new MovableMergeRule(),
            _ => throw new InvalidOperationException($"Unknown merge rule type '{type}'."),
        };
    }

    private static IWinRule CreateWinRule(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "movable" => new MovableWinRule(),
            _ => throw new InvalidOperationException($"Unknown win rule type '{type}'."),
        };
    }

    private static List<Point> ParseCells(XElement element)
    {
        var cellsAttribute = element.Attribute("cells")?.Value;
        if (!string.IsNullOrWhiteSpace(cellsAttribute))
            return ParseCellsAttribute(cellsAttribute);

        var cells = element.Elements("cell").Select(ParseCellElement).ToList();
        if (cells.Count == 0)
            throw new InvalidOperationException("Entity must define at least one cell.");

        return cells;
    }

    private static List<Point> ParseCellsAttribute(string value)
    {
        var cells = new List<Point>();
        var tokens = value.Split(
            [';', '|'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        foreach (var token in tokens)
        {
            var parts = token.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );
            if (parts.Length != 2)
                throw new InvalidOperationException($"Invalid cell format '{token}'.");
            if (!int.TryParse(parts[0], out var x) || !int.TryParse(parts[1], out var y))
                throw new InvalidOperationException($"Invalid cell coordinates '{token}'.");
            cells.Add(new Point(x, y));
        }
        if (cells.Count == 0)
            throw new InvalidOperationException("Entity must define at least one cell.");
        return cells;
    }

    private static Point ParseCellElement(XElement element)
    {
        var x = ReadRequiredInt(element, "x");
        var y = ReadRequiredInt(element, "y");
        return new Point(x, y);
    }

    private static int ReadRequiredInt(XElement element, params string[] names)
    {
        foreach (var name in names)
        {
            var value = element.Attribute(name)?.Value;
            if (string.IsNullOrWhiteSpace(value))
                continue;
            if (!int.TryParse(value, out var result))
                throw new InvalidOperationException($"Invalid integer for attribute '{name}'.");
            return result;
        }

        throw new InvalidOperationException(
            $"Missing required attribute '{string.Join(" or ", names)}'."
        );
    }

    private static string ReadRequiredString(XElement element, string name)
    {
        var value = element.Attribute(name)?.Value;
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Missing required attribute '{name}'.");
        return value;
    }

    private static Direction ParseDirection(string value)
    {
        if (Enum.TryParse<Direction>(value, true, out var dir))
            return dir;
        throw new InvalidOperationException($"Unknown gravity direction '{value}'.");
    }
}
