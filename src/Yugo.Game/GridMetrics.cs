using Microsoft.Xna.Framework;

namespace Yugo.Game;

public readonly record struct GridMetrics(
    Point Origin,
    int HalfUnitPixels,
    int GridWidth,
    int GridHeight
);
