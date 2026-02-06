using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Yugo.Core.Entities;
using Yugo.Core.Game;
using Yugo.Core.Rules;
using Yugo.Core.Serialization;
using Xunit;

namespace Yugo.Core.Tests;

public class LevelBasicsTests
{
    [Fact]
    public void Grid_IsInside_MatchesBounds()
    {
        var grid = new Grid(4, 3);

        Assert.True(grid.IsInside(new Point(0, 0)));
        Assert.True(grid.IsInside(new Point(3, 2)));
        Assert.False(grid.IsInside(new Point(-1, 0)));
        Assert.False(grid.IsInside(new Point(0, -1)));
        Assert.False(grid.IsInside(new Point(4, 0)));
        Assert.False(grid.IsInside(new Point(0, 3)));
    }

    [Fact]
    public void Entity_Translate_UpdatesGridCells()
    {
        var level = CreateLevel(6, 4);
        var entity = new TestEntity(level, [new Point(1, 1), new Point(2, 1)]);

        Assert.Same(entity, level.Grid[1, 1]);
        Assert.Same(entity, level.Grid[2, 1]);

        entity.TranslatePublic(new Point(1, 1));

        Assert.Null(level.Grid[1, 1]);
        Assert.Null(level.Grid[2, 1]);
        Assert.Same(entity, level.Grid[2, 2]);
        Assert.Same(entity, level.Grid[3, 2]);
    }

    [Fact]
    public void TryPush_BlockedByWall_DoesNotMove()
    {
        var level = CreateLevel(6, 1);
        var movable = new Movable(level, [new Point(1, 0)], 1);
        var wall = new Wall(level, [new Point(2, 0)]);

        level.AddEntity(movable);
        level.AddEntity(wall);

        level.TryPush(movable, Direction.Right);

        Assert.Same(movable, level.Grid[1, 0]);
        Assert.Same(wall, level.Grid[2, 0]);
    }

    [Fact]
    public void MovableMergeRule_MergesAdjacentClusters()
    {
        var level = CreateLevel(4, 2);
        var a = new Movable(level, [new Point(0, 0)], 1);
        var b = new Movable(level, [new Point(1, 0)], 1);
        level.AddEntity(a);
        level.AddEntity(b);

        var rule = new MovableMergeRule();
        ((IMergeRule)rule).Apply(level);

        Assert.Single(level.Entities.OfType<Movable>());
        var root = level.Entities.OfType<Movable>().Single();
        Assert.Equal(2, root.OccupiedCells.Count);
        Assert.Same(root, level.Grid[0, 0]);
        Assert.Same(root, level.Grid[1, 0]);
    }

    [Fact]
    public void MovableWinRule_DetectsDuplicateClusters()
    {
        var level = CreateLevel(4, 2);
        level.AddEntity(new Movable(level, [new Point(0, 0)], 1));
        level.AddEntity(new Movable(level, [new Point(2, 0)], 1));

        var rule = new MovableWinRule();
        Assert.Equal(WinRuleResult.NotSatisfied, rule.IsSatisfied(level));

        var level2 = CreateLevel(4, 2);
        level2.AddEntity(new Movable(level2, [new Point(0, 0)], 1));
        level2.AddEntity(new Movable(level2, [new Point(2, 0)], 2));
        Assert.Equal(WinRuleResult.Satisfied, rule.IsSatisfied(level2));
    }

    [Fact]
    public void LevelXml_LoadsEntitiesAndRules()
    {
        var xml = new XElement(
            "level",
            new XAttribute("width", "5"),
            new XAttribute("height", "4"),
            new XAttribute("gravity", "Down"),
            new XElement(
                "rules",
                new XElement("merge", new XAttribute("type", "movable")),
                new XElement("win", new XAttribute("type", "movable"))
            ),
            new XElement(
                "entities",
                new XElement("wall", new XAttribute("cells", "0,0;1,0")),
                new XElement("movable", new XAttribute("cluster", "1"), new XAttribute("cells", "2,1"))
            )
        );

        var level = LevelXml.Parse(xml, null);

        Assert.Equal(5, level.Grid.Width);
        Assert.Equal(4, level.Grid.Height);
        Assert.Equal(Direction.Down, level.Gravity);
        Assert.Equal(2, level.Entities.Count);
        Assert.Same(level.Entities[0], level.Grid[0, 0]);
        Assert.Same(level.Entities[0], level.Grid[1, 0]);
    }

    [Fact]
    public void Move_Left_DoesNotOverwriteWalls_WhenPushBlocked()
    {
        var xml = new XElement(
            "level",
            new XAttribute("width", "12"),
            new XAttribute("height", "8"),
            new XAttribute("gravity", "Down"),
            new XElement(
                "rules",
                new XElement("merge", new XAttribute("type", "movable")),
                new XElement("win", new XAttribute("type", "movable"))
            ),
            new XElement(
                "entities",
                new XElement("wall", new XAttribute("cells", "0,1;0,2;0,3;0,4;0,5;0,6")),
                new XElement("movable", new XAttribute("cluster", "1"), new XAttribute("cells", "2,2;3,2"))
            )
        );

        var level = LevelXml.Parse(xml, null);
        var entity = level.Grid[2, 2];
        Assert.NotNull(entity);
        level.Move(entity!, Direction.Left);

        Assert.IsType<Wall>(level.Grid[0, 2]);
        Assert.DoesNotContain(new Point(0, 2), level.Entities.OfType<Movable>().Single().OccupiedCells);
    }

    private static Level CreateLevel(int width, int height)
    {
        return new Level(width, height, Array.Empty<IMergeRule>(), Array.Empty<IWinRule>());
    }

    private sealed class TestEntity : Entity
    {
        public TestEntity(Level level, IEnumerable<Point> cells)
            : base(level, cells) { }

        public void TranslatePublic(Point offset)
        {
            Translate(offset);
        }

        public override void OnClick() { }

        public override void OnMove(Direction dir) { }

        public override IEnumerable<Point>? TryPush(Direction dir)
        {
            return OccupiedCells.Select(p => p + dir.ToPoint());
        }

        public override void OnPush(Direction dir) { }

        public override bool NeedsUpdate() => false;

        public override void Update() { }
    }
}
