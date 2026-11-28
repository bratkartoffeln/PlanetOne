using System;
using System.Collections;
using System.IO;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeSmokeRunner
    {
        private IEnumerator RunInteractionChecks()
        {
            var network=RunSupplyNetworkChecks();while(network.MoveNext())yield return network.Current;
            var walls=RunWallChecks();while(walls.MoveNext())yield return walls.Current;
            var radar=RunSelectionRadarChecks();while(radar.MoveNext())yield return radar.Current;
            var orders=RunDirectOrderChecks();while(orders.MoveNext())yield return orders.Current;
            var arrows=RunOrderArrowChecks();while(arrows.MoveNext())yield return arrows.Current;
            var focus=RunPlanetFocusChecks();while(focus.MoveNext())yield return focus.Current;
            var traffic=RunSpaceTrafficVisualChecks();while(traffic.MoveNext())yield return traffic.Current;
            var groundModels=RunGroundModelRadiusChecks();while(groundModels.MoveNext())yield return groundModels.Current;
        }
        private IEnumerator RunDirectOrderChecks()
        {
            controller.ConfigurePresentationTestPath(Path.Combine(outputDirectory,"direct-order-settings.json"));
            controller.SetMenuVisible(false);controller.SetSettingsVisible(false);controller.SetInterfaceLayout(InterfaceLayout.OrbitalBridge);
            controller.StartMilitaryFixture();controller.SetSimulationPaused(true);controller.CloseContextPanel();
            var ground=controller.Simulation;var planet=ground.FindPlanet(1);
            var tank=planet.Units.Find(u=>u.OwnerId==1&&u.Kind==UnitKind.Tank);
            var enemy=planet.Units.Find(u=>u.OwnerId==2&&u.Kind==UnitKind.Tank);
            // Explicit military fixture: relocate existing bodies into an unobstructed test area.
            // No production, stock, crew or replacement units are introduced.
            PlaceDirectOrderUnit(tank,12,15);PlaceDirectOrderUnit(enemy,15,15);
            Require(ground.CanTraverse(planet,1,tank.Kind,12,15)&&ground.CanTraverse(planet,2,enemy.Kind,15,15),"Direct ground-order fixture uses legal separated terrain");
            yield return null;yield return null;
            Require(controller.SelectUnit(tank.Id),"Existing own tank is selected for direct orders");
            controller.CloseContextPanel();controller.SetSurfaceFocus(controller.UnitView(tank.Id).position);yield return null;
            CheckDirectSelectionBounds(tank.Id,false,controller.UnitView(tank.Id));
            Vector2 movePoint=controller.SurfaceScreenPoint(12,17);int pending=ground.State.PendingCommands.Count;
            Require(controller.PointerActionAtScreen(movePoint)==WorldPointerAction.Move,"Free ground has the direct movement cursor");
            controller.PreviewWorldPointer(movePoint);yield return Capture("direct-ground-selection-move.png");Require(captureFailure==null,captureFailure??"Actual ground selection frame and movement pointer captured");
            Require(controller.BeginSurfacePan(movePoint)&&controller.EndSpacePan(movePoint)&&!controller.IsCommandMenuOpen,"Plain ground right-click submits movement without opening a menu");
            var move=LastDirectOrder(ground,pending,CommandKind.MoveUnit,tank.Id);
            Require(move.X==12&&move.Y==17,"Ground move retains the clicked hex coordinates");
            AdvanceSeconds(Definitions.TickSeconds);CheckDirectOrderAccepted(ground,move.Id);
            yield return null;
            Require(ground.CanSeeEntity(planet,1,enemy.Id)&&ground.AtWar(1,enemy.OwnerId),"Attack fixture contains an actually visible declared enemy");
            Vector2 attackPoint=controller.UnitSelectionScreenRect(enemy.Id,false).center;
            Require(controller.PointerActionAtScreen(attackPoint)==WorldPointerAction.Attack,"Visible ground enemy selects the attack cursor");
            controller.PreviewWorldPointer(attackPoint);yield return Capture("direct-ground-attack-cursor.png");Require(captureFailure==null,captureFailure??"Ground attack crosshair captured");
            CheckAttackPointerPixels("direct-ground-attack-cursor.png",attackPoint);
            pending=ground.State.PendingCommands.Count;
            Require(controller.BeginSurfacePan(attackPoint)&&controller.EndSpacePan(attackPoint)&&!controller.IsCommandMenuOpen,"Plain enemy right-click attacks without a context menu");
            var attack=LastDirectOrder(ground,pending,CommandKind.AttackTarget,tank.Id);Require(attack.TargetId==enemy.Id,"Ground attack pins the clicked enemy");
            AdvanceSeconds(Definitions.TickSeconds);CheckDirectOrderAccepted(ground,attack.Id);yield return null;
            attackPoint=controller.UnitSelectionScreenRect(enemy.Id,false).center;pending=ground.State.PendingCommands.Count;
            Require(controller.PointerActionAtScreen(attackPoint,true)==WorldPointerAction.Context,"Control modifier explicitly selects context interaction");
            controller.PreviewWorldPointer(attackPoint,true);
            Require(controller.BeginSurfacePan(attackPoint,true)&&controller.EndSpacePan(attackPoint)&&controller.IsCommandMenuOpen&&ground.State.PendingCommands.Count==pending,"Control-right-click opens ground actions without issuing an order");
            yield return Capture("direct-ground-context.png");Require(captureFailure==null,captureFailure??"Explicit ground context menu captured");
            Require(controller.InvokeContextCommand("diplomacy")&&ground.State.PendingCommands.Count==pending,"Foreign-unit context opens diplomacy without an implicit war or attack");
            yield return Capture("direct-ground-diplomacy.png");Require(captureFailure==null,captureFailure??"Diplomacy reached through unit context captured");
            controller.CloseContextPanel();controller.PreviewWorldPointer(null);yield return null;
            Vector2 dragStart=controller.WorldViewport.center,dragEnd=dragStart+new Vector2(74,27);pending=ground.State.PendingCommands.Count;
            Vector3 beforePan=controller.SurfaceFocus;
            Require(controller.BeginSurfacePan(dragStart),"Ground camera drag starts in the world");controller.ContinueSpacePan(dragEnd);
            Require(!controller.EndSpacePan(dragEnd)&&!controller.IsCommandMenuOpen&&ground.State.PendingCommands.Count==pending&&Vector3.Distance(beforePan,controller.SurfaceFocus)>.01f,"Ground dragging pans without an accidental move or attack");
            controller.SetSurfaceFocus(controller.UnitView(enemy.Id).position);yield return null;
            var relation=ground.State.Relations.Find(r=>(r.FirstId==1&&r.SecondId==2)||(r.FirstId==2&&r.SecondId==1));relation.Relation=RelationKind.Peace;
            attackPoint=controller.UnitSelectionScreenRect(enemy.Id,false).center;pending=ground.State.PendingCommands.Count;
            Require(controller.PointerActionAtScreen(attackPoint)==WorldPointerAction.Blocked&&!controller.IssueDefaultOrderAtScreen(attackPoint)&&ground.State.PendingCommands.Count==pending&&!ground.AtWar(1,2),"Neutral ground unit cannot be attacked or trigger an automatic war declaration");
            relation.Relation=RelationKind.War;PlaceDirectOrderUnit(enemy,planet.Width-2,planet.Height-2);yield return null;
            Require(!ground.CanSeeEntity(planet,1,enemy.Id),"Hidden-enemy ground case is outside actual friendly sight");
            controller.SetSurfaceFocus(new Vector3((float)enemy.X,0,(float)(enemy.Y*HexGrid.RowHeight)));yield return null;
            Vector2 hidden=controller.SurfaceScreenPoint(enemy.X,enemy.Y);pending=ground.State.PendingCommands.Count;
            Require(controller.PointerActionAtScreen(hidden)!=WorldPointerAction.Attack,"Hidden ground enemy does not reveal an attack cursor");controller.IssueDefaultOrderAtScreen(hidden);
            CheckNoHiddenAttack(ground,pending,"Hidden ground position");

            controller.StartInvasionFixture();controller.SetSimulationPaused(true);controller.CloseContextPanel();
            var space=controller.Simulation;var frigate=space.State.Ships.Find(s=>s.OwnerId==1&&s.Kind==ShipKind.EscortFrigate);
            var opponent=space.State.Ships.Find(s=>s.OwnerId==2&&s.Kind==ShipKind.Corvette);
            frigate.X=30;frigate.Y=30;frigate.Order=ShipOrder.Hold;opponent.X=33;opponent.Y=30;opponent.Order=ShipOrder.Hold;
            yield return null;yield return null;
            Require(controller.SelectShip(frigate.Id),"Existing armed escort frigate is selected");controller.CloseContextPanel();
            controller.SetSpaceFocus(PrototypeController.SpacePosition(31,30,2.3f));yield return null;
            // Isolated camera framing makes the actual geometry/cursor legible at both test resolutions.
            Camera.main.orthographicSize=10;yield return null;
            CheckDirectSelectionBounds(frigate.Id,true,controller.ShipView(frigate.Id));
            movePoint=Camera.main.WorldToScreenPoint(PrototypeController.SpacePosition(29,33,2.3f));pending=space.State.PendingCommands.Count;
            Require(controller.PointerActionAtScreen(movePoint)==WorldPointerAction.Move,"Empty space has the direct flight cursor");
            controller.PreviewWorldPointer(movePoint);yield return Capture("direct-space-selection-move.png");Require(captureFailure==null,captureFailure??"Actual ship frame and flight pointer captured");
            Require(controller.BeginSpacePan(movePoint)&&controller.EndSpacePan(movePoint)&&!controller.IsCommandMenuOpen,"Plain space right-click submits flight without opening a menu");
            move=LastDirectOrder(space,pending,CommandKind.MoveShipPosition,frigate.Id);Require(move.X==29&&move.Y==33,"Direct free-flight order retains the chosen coordinates");
            AdvanceSeconds(Definitions.TickSeconds);CheckDirectOrderAccepted(space,move.Id);yield return null;
            Require(space.IsShipVisible(1,opponent)&&space.AtWar(1,opponent.OwnerId),"Space attack target is a visible declared enemy");
            attackPoint=controller.UnitSelectionScreenRect(opponent.Id,true).center;
            Require(controller.PointerActionAtScreen(attackPoint)==WorldPointerAction.Attack,"Armed ship over an enemy receives the attack cursor");
            controller.PreviewWorldPointer(attackPoint);yield return Capture("direct-space-attack-cursor.png");Require(captureFailure==null,captureFailure??"Space attack crosshair captured");
            CheckAttackPointerPixels("direct-space-attack-cursor.png",attackPoint);pending=space.State.PendingCommands.Count;
            Require(controller.BeginSpacePan(attackPoint)&&controller.EndSpacePan(attackPoint)&&!controller.IsCommandMenuOpen,"Plain enemy-ship right-click submits the direct attack");
            attack=LastDirectOrder(space,pending,CommandKind.AttackShip,frigate.Id);Require(attack.TargetId==opponent.Id,"Space attack pins the actual clicked corvette");
            AdvanceSeconds(Definitions.TickSeconds);CheckDirectOrderAccepted(space,attack.Id);yield return null;
            attackPoint=controller.UnitSelectionScreenRect(opponent.Id,true).center;pending=space.State.PendingCommands.Count;
            controller.PreviewWorldPointer(attackPoint,true);
            Require(controller.BeginSpacePan(attackPoint,true)&&controller.EndSpacePan(attackPoint)&&controller.IsCommandMenuOpen&&space.State.PendingCommands.Count==pending,"Control-right-click opens foreign-ship actions without attacking");
            yield return Capture("direct-space-context.png");Require(captureFailure==null,captureFailure??"Foreign ship context captured");
            Require(controller.InvokeContextCommand("diplomacy")&&space.State.PendingCommands.Count==pending,"Foreign-ship diplomacy action is available without creating orders");
            controller.CloseContextPanel();controller.PreviewWorldPointer(null);yield return null;
            dragStart=controller.WorldViewport.center;dragEnd=dragStart+new Vector2(68,-23);pending=space.State.PendingCommands.Count;beforePan=controller.SpaceFocus;
            Require(controller.BeginSpacePan(dragStart),"System camera drag starts");controller.ContinueSpacePan(dragEnd);
            Require(!controller.EndSpacePan(dragEnd)&&space.State.PendingCommands.Count==pending&&!controller.IsCommandMenuOpen&&Vector3.Distance(beforePan,controller.SpaceFocus)>.01f,"System dragging never releases an accidental ship order");
            controller.SetSpaceFocus(controller.ShipView(opponent.Id).position);yield return null;Camera.main.orthographicSize=10;
            relation=space.State.Relations.Find(r=>(r.FirstId==1&&r.SecondId==2)||(r.FirstId==2&&r.SecondId==1));relation.Relation=RelationKind.Peace;
            attackPoint=controller.UnitSelectionScreenRect(opponent.Id,true).center;pending=space.State.PendingCommands.Count;
            Require(controller.PointerActionAtScreen(attackPoint)==WorldPointerAction.Blocked&&!controller.IssueDefaultOrderAtScreen(attackPoint)&&space.State.PendingCommands.Count==pending&&!space.AtWar(1,2),"Neutral ship blocks direct attack without declaring war");
            relation.Relation=RelationKind.War;opponent.X=90;opponent.Y=90;opponent.Order=ShipOrder.Hold;yield return null;
            Require(!space.IsShipVisible(1,opponent),"Hidden corvette is outside all friendly sensors");
            controller.SetSpaceFocus(PrototypeController.SpacePosition(opponent.X,opponent.Y,2.3f));yield return null;Camera.main.orthographicSize=10;
            hidden=Camera.main.WorldToScreenPoint(PrototypeController.SpacePosition(opponent.X,opponent.Y,2.3f));pending=space.State.PendingCommands.Count;
            Require(controller.PointerActionAtScreen(hidden)!=WorldPointerAction.Attack,"Hidden ship produces no attack cursor");controller.IssueDefaultOrderAtScreen(hidden);
            CheckNoHiddenAttack(space,pending,"Hidden space position");controller.PreviewWorldPointer(null);
        }

        private static void PlaceDirectOrderUnit(UnitState unit,int x,int y)
        {unit.X=unit.GoalX=x;unit.Y=unit.GoalY=y;unit.Order=UnitOrder.Hold;unit.Path.Clear();unit.PathDistance=0;unit.TargetId=0;unit.Traffic=null;}
        private SimCommand LastDirectOrder(GameSimulation sim,int previous,CommandKind kind,long actor)
        {
            Require(sim.State.PendingCommands.Count==previous+1,"Exactly one direct command is queued: "+kind);
            var command=sim.State.PendingCommands[previous];Require(command.Kind==kind&&command.ActorId==actor&&command.IssuerId==sim.State.PlayerFactionId,"Direct command uses the selected owned actor and shared adapter: "+kind);return command;
        }
        private void CheckDirectOrderAccepted(GameSimulation sim,long id)
        {var result=sim.State.CommandResults.Find(r=>r.CommandId==id);Require(result!=null&&result.Success,"Direct order passes authoritative simulation validation: "+id);}
        private void CheckNoHiddenAttack(GameSimulation sim,int previous,string label)
        {
            for(int i=previous;i<sim.State.PendingCommands.Count;i++)
            {var kind=sim.State.PendingCommands[i].Kind;Require(kind!=CommandKind.AttackTarget&&kind!=CommandKind.AttackShip&&kind!=CommandKind.DeclareWar,label+" does not reveal or attack an unseen unit");}
        }
        private void CheckDirectSelectionBounds(long id,bool space,Transform model)
        {
            Rect frame=controller.UnitSelectionScreenRect(id,space);Require(frame.width>=28&&frame.height>=28,"Selection frame has a usable minimum size: "+id);
            int count=0;bool encloses=true;
            foreach(var part in model.GetComponentsInChildren<Renderer>())
            {
                if(part is LineRenderer||!part.enabled||!part.gameObject.activeInHierarchy)continue;count++;
                Bounds bounds=part.bounds;
                for(int corner=0;corner<8;corner++)
                {
                    var world=bounds.center+Vector3.Scale(bounds.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                    encloses&=frame.Contains(Camera.main.WorldToScreenPoint(world));
                }
            }
            Require(count>1&&encloses,"Selection frame encloses every visible part of the actual multipart model");
        }
        private void CheckAttackPointerPixels(string file,Vector2 center)
        {
            var image=new Texture2D(2,2,TextureFormat.RGB24,false);
            try
            {
                image.LoadImage(File.ReadAllBytes(Path.Combine(outputDirectory,file)));int red=0;
                for(int y=Mathf.RoundToInt(center.y)-16;y<=Mathf.RoundToInt(center.y)+16;y++)for(int x=Mathf.RoundToInt(center.x)-16;x<=Mathf.RoundToInt(center.x)+16;x++)
                {
                    float radius=Vector2.Distance(new Vector2(x,y),center);if(radius<8||radius>15||x<0||y<0||x>=image.width||y>=image.height)continue;
                    Color color=image.GetPixel(x,y);if(color.r>.4f&&color.r>color.g*1.5f&&color.r>color.b*1.4f)red++;
                }
                Require(red>=12,"Actual screenshot contains the red attack reticle: "+file);
            }
            finally{Destroy(image);}
        }
    }
}
