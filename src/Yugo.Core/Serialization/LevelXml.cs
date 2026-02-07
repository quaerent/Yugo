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

    public static void Save(Level level, string path)
    {
        var document = new XDocument(ToXElement(level));
        document.Save(path);
    }

    public static XElement ToXElement(Level level)
    {
        SnapshotFactory.EnsureRegistered(typeof(LevelXml).Assembly);
        SnapshotFactory.EnsureRegistered(typeof(Level).Assembly);

        var root = new XElement(
            "level",
            new XAttribute("width", level.Grid.Width),
            new XAttribute("height", level.Grid.Height),
            new XAttribute("gravity", level.Gravity.ToString())
        );

        var mergeElement = new XElement("merge");
        foreach (var rule in level.MergeRules)
        {
            var snapshot = SnapshotFactory.Snapshot(rule);
            mergeElement.Add(SnapshotToElement(snapshot));
        }
        root.Add(mergeElement);

        var winElement = new XElement("win");
        foreach (var rule in level.WinRules)
        {
            var snapshot = SnapshotFactory.Snapshot(rule);
            winElement.Add(SnapshotToElement(snapshot));
        }
        root.Add(winElement);

        var entitiesElement = new XElement("entities");
        foreach (var entity in level.Entities)
        {
            var snapshot = SnapshotFactory.Snapshot(entity);
            entitiesElement.Add(SnapshotToElement(snapshot));
        }
        root.Add(entitiesElement);

        return root;
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
        ParseRules(root.Element("merge"), root.Element("win"), mergeRules, winRules);

        var levelFactory = factory ?? DefaultFactory;
        var level = levelFactory(width, height, mergeRules, winRules);
        level.Gravity = gravity;

        var entitiesElement =
            root.Element("entities")
            ?? throw new InvalidOperationException("Level XML must contain <entities>.");

        SnapshotFactory.EnsureRegistered(typeof(LevelXml).Assembly);
        foreach (var element in entitiesElement.Elements())
        {
            var snapshot = ParseEntitySnapshot(element);
            var created = SnapshotFactory.Create<ISnapshotSerializable>(snapshot, level);
            if (created is not Entity entity)
                throw new InvalidOperationException($"Type '{snapshot.TypeId}' is not an Entity.");
            level.AddEntity(entity);
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
        XElement? mergeElement,
        XElement? winElement,
        List<IMergeRule> mergeRules,
        List<IWinRule> winRules
    )
    {
        SnapshotFactory.EnsureRegistered(typeof(LevelXml).Assembly);

        if (mergeElement != null)
        {
            foreach (var element in mergeElement.Elements())
            {
                var created = SnapshotFactory.Create<ISnapshotSerializable>(
                    ParseRuleSnapshot(element)
                );
                if (created is not IMergeRule mergeRule)
                    throw new InvalidOperationException(
                        $"Type '{element.Name.LocalName}' is not a merge rule."
                    );
                mergeRules.Add(mergeRule);
            }
        }

        if (winElement != null)
        {
            foreach (var element in winElement.Elements())
            {
                var created = SnapshotFactory.Create<ISnapshotSerializable>(
                    ParseRuleSnapshot(element)
                );
                if (created is not IWinRule winRule)
                    throw new InvalidOperationException(
                        $"Type '{element.Name.LocalName}' is not a win rule."
                    );
                winRules.Add(winRule);
            }
        }
    }

    private static Snapshot ParseRuleSnapshot(XElement element)
    {
        var data = new Dictionary<string, string>();
        foreach (var attr in element.Attributes())
        {
            data[attr.Name.LocalName] = attr.Value;
        }

        return new Snapshot(element.Name.LocalName.ToLowerInvariant(), data);
    }

    private static Snapshot ParseEntitySnapshot(XElement element)
    {
        var typeId = element.Name.LocalName.ToLowerInvariant();
        var data = new Dictionary<string, string>();

        var cellsAttribute = element.Attribute("cells")?.Value;
        if (!string.IsNullOrWhiteSpace(cellsAttribute))
        {
            data["cells"] = cellsAttribute;
        }
        else
        {
            var cells = element.Elements("cell").Select(ParseCellElement).ToList();
            if (cells.Count == 0)
                throw new InvalidOperationException("Entity must define at least one cell.");
            data["cells"] = SnapshotHelpers.SerializeCells(cells);
        }

        if (typeId == "movable")
        {
            var clusterId = ReadRequiredInt(element, "cluster", "clusterId");
            data["clusterId"] = clusterId.ToString();
        }

        var deadRaw = element.Attribute("dead")?.Value;
        data["dead"] = string.IsNullOrWhiteSpace(deadRaw) ? "0" : deadRaw;

        return new Snapshot(typeId, data);
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

    private static XElement SnapshotToElement(Snapshot snapshot)
    {
        var element = new XElement(snapshot.TypeId);
        foreach (var (key, value) in snapshot.Data)
        {
            element.SetAttributeValue(key, value);
        }
        return element;
    }
}
