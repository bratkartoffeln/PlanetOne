using PlanetOne.Core;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeController
    {
        public void StartAiScenario()
        {
            if(!simulation.State.AiScenario&&!simulation.State.InvasionTestFixture&&!simulation.State.SpaceTestFixture&&!simulation.State.MilitaryTestFixture)priorSession=simulation;
            simulation=GameSimulation.CreateAiScenario();foodRates.Clear();controlGroups.Clear();shipGroups.Clear();
            showingSpacePanel=false;showingMilitary=false;BuildSpace();SetView(0);Notify(simulation.State.LastMessage);
        }

        private void DrawAiSession()
        {
            if(!simulation.State.AiScenario)return;
            GUILayout.Label("M6 · COMPUTERGEGNER",headingStyle);
            GUILayout.Label("Die Freien Stationen bauen selbstständig auf. Szenariovorgabe: Krieg. Beide Seiten starten mit denselben Vorräten und ohne Flotte. Gegnerwissen bleibt verborgen.",smallStyle);
            if(priorSession!=null&&GUILayout.Button("Zur vorherigen Kolonie zurück",buttonStyle,GUILayout.Height(36)))
            {simulation=priorSession;priorSession=null;foodRates.Clear();BuildSpace();SetView(0);}
        }
    }
}
