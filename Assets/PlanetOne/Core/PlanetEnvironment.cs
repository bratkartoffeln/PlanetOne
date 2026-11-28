using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public enum PlanetKind { Rocky, Desert, Temperate, Ocean, Ice, GasGiant }

    /// <summary>World composition is data; neither camera nor texture chooses the rules.</summary>
    public static class PlanetEnvironment
    {
        // The prototype star has solar luminosity. Orbit lengths remain scaled (10 units/AU).
        public const double UnitsPerAu = 10;
        public static bool Airborne(PlanetState p) { return p.Kind == PlanetKind.GasGiant; }
        public static string Name(PlanetKind kind)
        {
            switch(kind) {
                case PlanetKind.Desert: return "Wüstenwelt";
                case PlanetKind.Temperate: return "Bewachsene Welt";
                case PlanetKind.Ocean: return "Ozeanwelt";
                case PlanetKind.Ice: return "Eiswelt";
                case PlanetKind.GasGiant: return "Gasriese";
                default: return "Felswelt";
            }
        }
        public static PlanetKind InitialKind(int id, int parent)
        {
            if(parent >= 0) return PlanetKind.Rocky;
            switch(id) { case 1:return PlanetKind.Temperate; case 2:return PlanetKind.Ice;
                case 4: case 5:return PlanetKind.Desert; case 6:return PlanetKind.Ocean;
                case 7:return PlanetKind.GasGiant; default:return PlanetKind.Rocky; }
        }
        public static double RadiusEarths(PlanetKind kind)
        {
            switch(kind) { case PlanetKind.Rocky:return .28; case PlanetKind.Desert:return .82;
                case PlanetKind.Temperate:return 1; case PlanetKind.Ocean:return 1.25;
                case PlanetKind.Ice:return .9; default:return 9.5; }
        }
        public static double Albedo(PlanetKind kind)
        {return kind==PlanetKind.Ice?.6:kind==PlanetKind.Ocean?.23:kind==PlanetKind.Rocky?.12:kind==PlanetKind.GasGiant?.5:.3;}
        public static double GreenhouseKelvin(PlanetKind kind)
        {return kind==PlanetKind.Temperate?33:kind==PlanetKind.Ocean?45:kind==PlanetKind.Desert?15:kind==PlanetKind.Ice?12:0;}
        public static double StellarAu(GameState state,PlanetState p)
        {return (p.ParentId<0?p:state.Planets.Find(w=>w.Id==p.ParentId)).OrbitRadius/UnitsPerAu;}
        public static double EquilibriumKelvin(GameState state,PlanetState p)
        {return 278.3*Math.Pow(1-Albedo(p.Kind),.25)/Math.Sqrt(StellarAu(state,p));}
        public static double RotationSeconds(PlanetKind kind)
        {return kind==PlanetKind.GasGiant?240:kind==PlanetKind.Rocky?1200:kind==PlanetKind.Desert?900:600;}
        public static bool AllowsBuilding(PlanetKind world,BuildingKind kind)
        {
            if(kind==BuildingKind.GasExtractor)return world==PlanetKind.GasGiant;
            return world!=PlanetKind.GasGiant || (kind!=BuildingKind.Mine && kind!=BuildingKind.CrystalMine && kind!=BuildingKind.Wall && kind!=BuildingKind.Gate);
        }
        // Recoverable gas in the colony's operating band, measured in freight units.
        public static int InitialHydrogen(int seed,PlanetState planet)
        {return Airborne(planet)?1600+2*(int)(unchecked((uint)(seed+planet.Id*191))%401):0;}
        public static bool BlocksGround(int terrain) { return terrain==1 || terrain==4; }

        // One hex per segment preserves the existing capacity, speed, return trip and recovery accounting.
        // Drones use air corridors above the platforms, so intermediate buildings do not obstruct freight.
        public static List<GridCell> AirPath(int width,int height,GridCell start,GridCell end)
        {
            var path=new List<GridCell>{start};var cell=start;
            while(cell.X!=end.X || cell.Y!=end.Y) {
                int remaining=HexGrid.Steps(cell.X,cell.Y,end.X,end.Y);bool found=false;
                for(int d=0;d<6;d++) {
                    var next=HexGrid.Neighbor(cell.X,cell.Y,d);
                    if(next.X<0 || next.Y<0 || next.X>=width || next.Y>=height)continue;
                    if(HexGrid.Steps(next.X,next.Y,end.X,end.Y)>=remaining)continue;
                    cell=next;path.Add(cell);found=true;break;
                }
                if(!found)return null;
            }
            return path;
        }
        public static bool OceanLand(double x,double y)
        {
            var point=HexGrid.ToWorld(x,y);return OceanHeight(point.X,point.Y)>.12;
        }
        public static double OceanHeight(double worldX,double worldZ)
        {
            double x=worldX-.25,y=worldZ/HexGrid.RowHeight;
            double a=(x-8)/8.5,b=(y-6)/7,c=(x-23)/3.7,d=(y-16)/3.1;
            return Math.Max(-1.6,Math.Min(2.4,1.7*Math.Max(1+.10*Math.Sin(x*.7+y*.4)-a*a-b*b,1-c*c-d*d)));
        }
    }
}
