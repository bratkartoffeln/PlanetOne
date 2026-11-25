using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    /// <summary>Pointy-top odd-row hexes. Adjacent centers are one world unit apart.
    /// Stored coordinates remain column/row; fractional rows interpolate their offsets,
    /// so existing paths and movement progress retain their meaning after migration.</summary>
    public static class HexGrid
    {
        public static readonly double RowHeight = Math.Sqrt(3) / 2;
        public static readonly double Radius = 1 / Math.Sqrt(3);

        public static SimPoint ToWorld(double x, double y)
        {
            int row = (int)Math.Floor(y); double t = y - row;
            double shift = ((row & 1) * (1 - t) + ((row + 1) & 1) * t) * 0.5;
            return new SimPoint(x + shift, y * RowHeight);
        }

        public static GridCell FromWorld(double x, double y)
        {
            // Nearest center is the exact Voronoi hex, including its sloping edges.
            int row = (int)Math.Round(y / RowHeight);
            double best = double.MaxValue; var result = new GridCell();
            for (int r = row - 1; r <= row + 1; r++)
            {
                int col = (int)Math.Round(x - (r & 1) * 0.5);
                var center = ToWorld(col, r);
                double d = (x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y);
                if (d < best) { best = d; result = new GridCell(col, r); }
            }
            return result;
        }

        public static GridCell CellAt(double x, double y)
        { var world = ToWorld(x, y); return FromWorld(world.X, world.Y); }

        public static GridCell Neighbor(int x, int y, int direction)
        {
            int shift = y & 1;
            switch (direction)
            {
                case 0: return new GridCell(x + 1, y);
                case 1: return new GridCell(x + shift, y + 1);
                case 2: return new GridCell(x + shift - 1, y + 1);
                case 3: return new GridCell(x - 1, y);
                case 4: return new GridCell(x + shift - 1, y - 1);
                case 5: return new GridCell(x + shift, y - 1);
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        public static int Steps(int ax, int ay, int bx, int by)
        {
            int aq = ax - (ay - (ay & 1)) / 2, bq = bx - (by - (by & 1)) / 2;
            int q = aq - bq, r = ay - by;
            return (Math.Abs(q) + Math.Abs(r) + Math.Abs(q + r)) / 2;
        }

        public static bool Adjacent(GridCell a, GridCell b)
        { return Steps(a.X, a.Y, b.X, b.Y) == 1; }

        public static double Distance(double ax, double ay, double bx, double by)
        {
            var a = ToWorld(ax, ay); var b = ToWorld(bx, by);
            double x = a.X - b.X, y = a.Y - b.Y;
            return Math.Sqrt(x * x + y * y);
        }

        public static IEnumerable<GridCell> LandingCells(int x, int y)
        {
            yield return new GridCell(x, y);
            for (int i = 0; i < 6; i++) yield return Neighbor(x, y, i);
        }
    }
}
