using System.Collections.Generic;
using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private readonly Dictionary<long,int> wallShapes=new Dictionary<long,int>();
        private static bool IsFortification(BuildingKind kind){return kind==BuildingKind.Wall||kind==BuildingKind.Gate;}
        private static bool HasWallLinks(BuildingKind kind){return IsFortification(kind) || Definitions.IsTower(kind);}
        private int WallShape(PlanetState planet,int x,int y,int owner,bool includeTowers=true)
        {
            return GameSimulation.FortificationMask(planet,x,y,owner,includeTowers);
        }
        private void BuildConnectedWall(Transform root,BuildingKind kind,int x,int y,int owner)
        {
            var planet=simulation.FindPlanet(openPlanetId);int shape=WallShape(planet,x,y,owner,!Definitions.IsTower(kind));
            var center=SurfacePosition(x,0,y);
            var ends=new List<Vector3>();
            for(int i=0;i<6;i++)if((shape&(1<<i))!=0)
            {
                var n=HexGrid.Neighbor(x,y,i);var other=SurfacePosition(n.X,0,n.Y);
                ends.Add((other-center)*.5f);
            }
            if(Definitions.IsTower(kind))
            {
                foreach(var end in ends)WallArm(root,center,Vector3.ProjectOnPlane(end,Vector3.up).normalized*.28f,end);
                return;
            }
            if(ends.Count==0 && kind==BuildingKind.Wall){ends.Add(Vector3.right*.42f);ends.Add(Vector3.left*.42f);}
            if(kind==BuildingKind.Wall)
            {
                Part(root,"Mauerpfeiler",new Vector3(0,.25f,0),new Vector3(.15f,.5f,.15f),Steel);
                Part(root,"Betriebsanzeige",new Vector3(0,.505f,0),new Vector3(.05f,.015f,.05f),Amber);
                foreach(var end in ends)WallArm(root,center,Vector3.zero,end);
            }
            else
            {
                var passage=HexGrid.Neighbor(x,y,GameSimulation.GatePassage(shape));
                var axis=Vector3.Cross(Vector3.up,Vector3.ProjectOnPlane(SurfacePosition(passage.X,0,passage.Y)-center,Vector3.up)).normalized;
                if(shape==0){ends.Clear();ends.Add(axis*.42f);ends.Add(-axis*.42f);}
                var gate=ColonyGroup(root,"Ausgerichtetes Tor",Vector3.zero);
                var right=center+axis*.32f;var left=center-axis*.32f;
                right.y=ReliefHeight(right.x,right.z);left.y=ReliefHeight(left.x,left.z);
                gate.localPosition=Vector3.up*((right.y+left.y)*.5f-center.y);
                gate.localRotation=Quaternion.FromToRotation(Vector3.right,(right-left).normalized);ColonyGate(gate);
                foreach(var end in ends)
                {
                    var jamb=axis*(Vector3.Dot(end,axis)>=0?.32f:-.32f);
                    WallArm(root,center,jamb,end);
                }
            }
        }
        private void WallArm(Transform root,Vector3 center,Vector3 start,Vector3 end)
        {
            if((end-start).sqrMagnitude<.00001f)return;
            // Sample both ends of each short section on the same relief as the wheels.
            var arm=ColonyGroup(root,"Verbundene Mauer",Vector3.zero);
            int sections=Mathf.Max(1,Mathf.CeilToInt((end-start).magnitude/.15f));
            for(int i=0;i<sections;i++)
            {
                var a=Vector3.Lerp(start,end,i/(float)sections);var b=Vector3.Lerp(start,end,(i+1f)/sections);
                a.y=ReliefHeight(center.x+a.x,center.z+a.z)-center.y;
                b.y=ReliefHeight(center.x+b.x,center.z+b.z)-center.y;
                var delta=b-a;var mid=(a+b)*.5f;
                var panel=Part(arm,"Mauerfeld",mid+Vector3.up*.18f,new Vector3(.11f,.52f,delta.magnitude+.018f),Hull);
                var cap=Part(arm,"Mauerabschluss",mid+Vector3.up*.43f,new Vector3(.14f,.04f,delta.magnitude+.018f),Trim);
                panel.localRotation=cap.localRotation=Quaternion.LookRotation(delta);
            }
        }
    }
}
