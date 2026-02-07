using Microsoft.Xna.Framework;
using Yugo.Core.Game;
using Yugo.Core.Serialization;

namespace Yugo.Core.Entities;

[TypeId("piston", "Piston")]
public class Piston : ClusterEntity
{
    public enum Orientation
    {
        Horizontal,
        Vertical,
    }

    public Orientation Axis { get; set; }

    public Piston(Level level)
        : base(level) { }

    public Piston(Level level, IEnumerable<Point> occupiedCells, Orientation axis, int clusterId)
        : base(level, occupiedCells, clusterId)
    {
        Axis = axis;
    }

    public override void OnClick() { }

    public override void OnMove(Direction dir) { }

    public override IEnumerable<Point>? TryPush(Direction dir)
    {
        var offset = dir.ToPoint();
        var newCells = new List<Point>();

        // Check if pushing along the axis
        bool isAlongAxis =
            (Axis == Orientation.Horizontal && (dir == Direction.Left || dir == Direction.Right))
            || (Axis == Orientation.Vertical && (dir == Direction.Up || dir == Direction.Down));

        if (isAlongAxis)
        {
            // Determine the coordinate of the boundary we are moving away from
            int sourceBoundaryCoord = dir switch
            {
                Direction.Down => 0,
                Direction.Up => Level.Grid.Height - 1,
                Direction.Right => 0,
                Direction.Left => Level.Grid.Width - 1,
                _ => -1,
            };

            // Calculate shifted cells and track growth points
            var growthPoints = new List<Point>();
            foreach (var p in OccupiedCells)
            {
                // Shift
                var moved = p + offset;
                if (Level.Grid.IsInside(moved))
                {
                    newCells.Add(moved);
                }

                // Check if this point was at the source boundary
                bool wasAtBoundary =
                    (Axis == Orientation.Vertical && p.Y == sourceBoundaryCoord)
                    || (Axis == Orientation.Horizontal && p.X == sourceBoundaryCoord);

                if (wasAtBoundary)
                {
                    growthPoints.Add(p); // These positions now "grow" new cells
                }
            }

            // Add growth points back to maintain connection to the boundary
            foreach (var gp in growthPoints)
            {
                if (!newCells.Contains(gp))
                {
                    newCells.Add(gp);
                }
            }
        }
        else
        {
            // Perpendicular push: blocked
            return null;
        }

        return newCells;
    }

    public override void OnPush(Direction dir) { }

    public override bool NeedsUpdate() => false;

    public override void Update() { }

    public override void Serialize(Dictionary<string, string> data)
    {
        base.Serialize(data);
        data["axis"] = Axis.ToString();
    }

    public override void Deserialize(Dictionary<string, string> data)
    {
        base.Deserialize(data);
        if (
            data.TryGetValue("axis", out var axisRaw)
            && Enum.TryParse<Orientation>(axisRaw, out var axis)
        )
        {
            Axis = axis;
        }
    }
}
