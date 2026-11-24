using System;
using System.Collections.Generic;

namespace PlanetOne.Core
{
    public static partial class SaveCodec
    {
        private static void ValidateAi(GameState state)
        {
            Require(state.ComputerFactions!=null&&state.ComputerFactions.Count<=64,"KI-Zustand fehlt oder zu groß.");
            var owners=new HashSet<int>();var assigned=new HashSet<long>();
            foreach(var ai in state.ComputerFactions)
            {
                Require(ai!=null&&state.Factions!=null&&state.Factions.Exists(f=>f!=null&&f.Id==ai.FactionId&&!f.IsPlayer)&&owners.Add(ai.FactionId),"Ungültige KI-Fraktion.");
                Require(Enum.IsDefined(typeof(AiProfile),ai.Profile)&&ai.NextPlanTick>=0&&ai.NextPlanTick<=state.Tick+Definitions.AiPlanTicks&&ai.ScoutWaypoint>=0,"Ungültiger KI-Takt oder Profil.");
                Require(ai.Contacts!=null&&ai.Contacts.Count<=10000&&ai.Decisions!=null&&ai.Decisions.Count<=Definitions.AiDecisionLimit,"Ungültiges KI-Wissen oder Protokoll.");
                var contacts=new HashSet<long>();
                foreach(var c in ai.Contacts)
                {
                    Require(c!=null&&c.Id>0&&c.Id<state.NextEntityId&&contacts.Add(c.Id)&&c.Tick>=0&&c.Tick<=state.Tick&&Enum.IsDefined(typeof(BuildingKind),c.Kind),"Ungültige KI-Sichtung.");
                    var p=state.Planets==null?null:state.Planets.Find(w=>w.Id==c.PlanetId);
                    Require(p!=null&&c.X>=0&&c.Y>=0&&c.X<p.Width&&c.Y<p.Height&&state.Factions.Exists(f=>f.Id==c.OwnerId),"Ungültiger KI-Sichtungsort.");
                }
                foreach(var d in ai.Decisions)Require(d!=null&&d.Tick>=0&&d.Tick<=state.Tick&&d.CommandId>=0&&d.CommandId<state.NextCommand&&d.Goal!=null&&d.Reason!=null&&d.Alternative!=null&&d.Action!=null,"Ungültiger KI-Entscheidungsnachweis.");
                var op=ai.Operation;
                Require(op!=null&&Enum.IsDefined(typeof(OperationPhase),op.Phase)&&op.ChangedTick>=0&&op.ChangedTick<=state.Tick&&op.RetryAfter>=0&&op.Reason!=null&&op.Ships!=null&&op.Units!=null,"Ungültige KI-Operation.");
                Require(op.FoodReserved>=0&&op.PartsReserved>=0&&op.FoodReserved+op.PartsReserved<=24,"Ungültiges Operationsbudget.");
                Require(op.TargetPlanetId==0||state.Planets.Exists(p=>p.Id==op.TargetPlanetId),"Unbekanntes KI-Ziel.");
                Require(op.RejectedLandings!=null&&op.RejectedLandings.Count<=4096,"Ungültige gesperrte KI-Landeflächen.");
                var rejectedCells=new HashSet<string>();
                foreach(var c in op.RejectedLandings)Require(c.X>=0&&c.Y>=0&&c.X<128&&c.Y<128&&rejectedCells.Add(c.X+":"+c.Y),"Ungültige oder doppelte gesperrte Landefläche.");
                Require(op.HomePlanetId==0||state.Planets.Exists(p=>p.Id==op.HomePlanetId),"Unbekannte KI-Rückzugsbasis.");
                if(op.LandingChosen){var p=state.Planets.Find(w=>w.Id==op.TargetPlanetId);Require(p!=null&&op.LandingX>=0&&op.LandingY>=0&&op.LandingX<p.Width&&op.LandingY<p.Height,"Ungültiger KI-Landeort.");}
                foreach(long id in op.Ships)
                    Require(assigned.Add(id)&&state.Ships.Exists(s=>s.Id==id&&s.OwnerId==ai.FactionId),"Schiff doppelt oder fremd reserviert.");
                foreach(long id in op.Units)
                {
                    bool own=false;
                    foreach(var p in state.Planets)if(p.Units.Exists(u=>u.Id==id&&u.OwnerId==ai.FactionId))own=true;
                    foreach(var s in state.Ships)if(s.Troops!=null&&s.Troops.Exists(u=>u.Id==id&&u.OwnerId==ai.FactionId))own=true;
                    Require(assigned.Add(id)&&own,"Truppe doppelt oder fremd reserviert.");
                }
                if(op.Phase==OperationPhase.Completed||op.Phase==OperationPhase.Cancelled)
                    Require(op.Units.Count==0&&op.Ships.Count==0&&op.FoodReserved==0&&op.PartsReserved==0,"Abgeschlossene Operation hält Reservierungen.");
            }
        }
    }
}
