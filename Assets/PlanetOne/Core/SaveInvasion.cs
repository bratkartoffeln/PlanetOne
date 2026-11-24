using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public static partial class SaveCodec
    {
        private static void MigrateInvasion(GameState state)
        {
            state.Beachheads=new List<BeachheadState>();
            foreach(var s in state.Ships) { s.Health=s.Destroyed?0:Definitions.SpaceWeapons[(int)s.Kind].Health; s.Troops=new List<UnitState>(); s.ProductionKind=-1; }
            state.Version=5;
        }
        private static void ValidateInvasion(GameState state, HashSet<int> factions, HashSet<long> entities)
        {
            Require(state.Beachheads!=null,"Landeköpfe fehlen.");
            foreach(var p in state.Planets) foreach(var b in p.Buildings)
            {
                Require((b.OwnerId==0 || factions.Contains(b.OwnerId)) && (b.CapturingFaction==0 || factions.Contains(b.CapturingFaction)) && Finite(b.CaptureProgress) && b.CaptureProgress>=0 && b.CaptureProgress<=Definitions.CaptureSeconds,"Ungültige Besetzung.");
                Require(b.RestoreWorkerId>=0 && Finite(b.RestoreProgress) && b.RestoreProgress>=0 && b.RestoreProgress<Definitions.BeachheadSeconds && (b.RestoreWorkerId==0?b.RestoreProgress==0:b.Destroyed && b.Kind==BuildingKind.Headquarters && p.Units.Exists(u=>u.Id==b.RestoreWorkerId && u.Kind==UnitKind.Engineer)),"Ungültige Verwaltungsreparatur.");
            }
            foreach(var s in state.Ships)
            {
                var d=Definitions.SpaceWeapons[(int)s.Kind];
                Require(Finite(s.Health) && s.Health>=0 && s.Health<=d.Health && s.Destroyed==(s.Health==0) && s.Ammunition>=0 && s.Ammunition<=d.Ammo && Finite(s.Cooldown) && s.Cooldown>=0 && Finite(s.ActionProgress) && s.ActionProgress>=0,"Ungültiger Schiffskampfzustand.");
                Require(Enum.IsDefined(typeof(TransportPhase),s.TransportPhase) && s.Troops!=null && s.Troops.Count<=Definitions.TroopCapacity && (s.Kind==ShipKind.LandingTransport || s.Troops.Count==0 && s.TransportPhase==TransportPhase.Space),"Ungültiger Truppentransport.");
                if(s.TransportPhase!=TransportPhase.Space)
                { var p=state.Planets.Find(v=>v.Id==s.TargetPlanetId); Require(p!=null && s.LandingX>=0 && s.LandingY>=0 && s.LandingX+1<p.Width && s.LandingY+1<p.Height && s.ActionProgress<=Definitions.TransitionSeconds+0.001,"Ungültige Landung."); }
                Require(s.ProductionKind==-1 || s.Kind==ShipKind.OrbitalYard && s.ProductionKind>=(int)ShipKind.Scout && s.ProductionKind<Definitions.Ships.Length,"Ungültiger Werftauftrag.");
                Require(Finite(s.ProductionProgress) && s.ProductionProgress>=0 && (s.ProductionKind>=0?s.ProductionProgress<Definitions.Ships[s.ProductionKind].Seconds && (s.ProductionProgress==0 || s.ProductionPaid):s.ProductionProgress==0 && !s.ProductionPaid),"Ungültiger Werftfortschritt.");
                foreach(var u in s.Troops) Require(u.OwnerId==s.OwnerId && (!s.Destroyed || u.Destroyed),"Ungültiger Truppeneigentümer oder Transportverlust.");
                // Reuse all existing troop invariants and the global identity set for actual embarked objects.
                var hold=new PlanetState {Width=18,Height=14,Units=s.Troops};
                ValidateMilitary(state,hold,factions,entities);
            }
            foreach(var h in state.Beachheads)
            {
                var p=state.Planets.Find(v=>v.Id==h.PlanetId);
                Require(h.Id>0 && entities.Add(h.Id) && factions.Contains(h.OwnerId) && p!=null && h.X>=0 && h.Y>=0 && h.X<p.Width && h.Y<p.Height && h.Food>=0 && h.Parts>=0 && Finite(h.Progress) && h.Progress>=0 && h.Progress<=Definitions.BeachheadSeconds && Finite(h.Health) && h.Health>=0 && h.Health<=100 && h.Destroyed==(h.Health==0),"Ungültiger Landekopf.");
            }
        }
    }
}
