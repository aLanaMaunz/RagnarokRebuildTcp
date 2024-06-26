﻿using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RoRebuildServer.Data;

namespace RoRebuildServer.Simulation.Pathfinding;

public static class DistanceCache
{
    private static Direction[] directions = null!;
    private static float[] angles = null!;
    private static float[] distances = null!;
    private static int[] intDistances = null!;
    private static int[] distanceToSquareRange = null!;

    private static int max;
    private static int width;
    private static int height;
    private static int maxRangeConversion;

    private const int center = ServerConfig.MaxViewDistance;

    public static void Init()
    {
        max = ServerConfig.MaxViewDistance;
        width = max * 2 + 1;
        height = max * 2 + 1;
        maxRangeConversion = max * 2; //probably too much but oh well

        angles = new float[width * height];
        distances = new float[width * height];
        intDistances = new int[width * height];
        directions = new Direction[width * height];
        distanceToSquareRange = new int[maxRangeConversion];

        var centerPos = new Position(center, center);

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var pos = new Position(x, y);

                distances[x + y * width] = CalcDistance(x, y, center, center);
                intDistances[x + y * width] = (int)Math.Round(distances[x + y * width]);

                var angle = centerPos.Angle(pos);
                var facing = Directions.GetFacingForAngle(angle);

                angles[x + y * width] = angle;
                directions[x + y * width] = facing;
            }
        }

        for (var i = 0; i < maxRangeConversion; i++)
        {
            distanceToSquareRange[i] = (int)(i * MathF.Sqrt(2) / 2f);
        }

    }

    public static int FitSquareRangeInCircle(int range)
    {
        if (range < maxRangeConversion)
            return distanceToSquareRange[range];
        return (int)(range * MathF.Sqrt(2));
    }

    public static float Angle(Position p1, Position p2)
    {
        var offset = p1 - p2;
        if (offset.SquareDistance(Position.Zero) > max)
            return p1.Angle(p2);

        return angles[(offset.X + center) + (offset.Y + center) * width];
    }

    public static Direction Direction(Position p1, Position p2)
    {
        var offset = p2 - p1;
        if (offset.SquareDistance(Position.Zero) > max)
        {
            var angle = p1.Angle(p2);
            return Directions.GetFacingForAngle(angle);
        }

        return directions[(offset.X + center) + (offset.Y + center) * width];
    }

    public static int IntDistance(Position p1, Position p2)
    {
        var offset = p1 - p2;
        if (offset.SquareDistance(Position.Zero) > max)
            return (int)Math.Round(CalcDistance(offset.X, offset.Y, 0, 0));

        return intDistances[(offset.X + center) + (offset.Y + center) * width];
    }

    public static float Distance(Position p1, Position p2)
    {
        var offset = p1 - p2;
        if (offset.SquareDistance(Position.Zero) > max)
            return CalcDistance(offset.X, offset.Y, 0, 0);

        return distances[(offset.X + center) + (offset.Y + center) * width];
    }

    private static float CalcDistance(int x1, int y1, int x2, int y2)
    {
        var p1 = Math.Pow((x2 - x1), 2);
        var p2 = Math.Pow((y2 - y1), 2);
        return (float)Math.Sqrt(p1 + p2);
    }

    public static int DistanceTo(this Position p1, Position p2)
    {
        return IntDistance(p1, p2);
    }

    public static bool InRange(this Position p1, Position p2, int distance)
    {
        return IntDistance(p1, p2) <= distance;
    }
}