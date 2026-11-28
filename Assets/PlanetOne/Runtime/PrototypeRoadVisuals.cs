using UnityEngine;
using PlanetOne.Core;
namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        private int roadVisualHash;
        private void RefreshRoadVisuals(PlanetState planet)
        {
            int hash=17;unchecked{foreach(bool road in planet.Roads)hash=hash*31+(road?1:0);foreach(var building in planet.Buildings)hash=hash*31+building.X*31+building.Y;}
            if(hash==roadVisualHash)return;roadVisualHash=hash;
            for(int i=0;i<roadViews.Length;i++){if(roadViews[i]!=null)Destroy(roadViews[i].gameObject);roadViews[i]=null;}
        }
    }
}
