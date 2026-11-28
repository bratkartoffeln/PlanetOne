using PlanetOne.Core;

// Explicit terrain fixtures replace obsolete road-removal assumptions.
internal static class LogisticsBarrier
{
    internal static int[] Column(PlanetState p,int x)
    {var before=(int[])p.Terrain.Clone();for(int y=0;y<p.Height;y++)p.Terrain[y*p.Width+x]=1;return before;}
    internal static int[] Ring(PlanetState p,int x,int y)
    {var before=(int[])p.Terrain.Clone();for(int d=0;d<6;d++){var n=HexGrid.Neighbor(x,y,d);if(n.X>=0&&n.Y>=0&&n.X<p.Width&&n.Y<p.Height)p.Terrain[n.Y*p.Width+n.X]=1;}return before;}
    internal static void Restore(PlanetState p,int[] before){System.Array.Copy(before,p.Terrain,before.Length);}
}
