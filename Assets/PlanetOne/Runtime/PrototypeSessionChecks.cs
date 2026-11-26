using System;
using System.Collections;
using System.IO;
using PlanetOne.Core;
using PlanetOne.Platform;
using UnityEngine;

namespace PlanetOne.Runtime
{
    public sealed partial class PrototypeSmokeRunner
    {
        private IEnumerator RunSessionChecks()
        {
            string directory=Path.Combine(outputDirectory,"sessions");var slots=new SaveSlots(directory);
            controller.ConfigurePresentationTestPath(Path.Combine(outputDirectory,"sessions-presentation.json"));
            controller.ConfigureSessionTestPath(directory,true);
            Require(!controller.HasActiveSession,"Title screen does not start a playable fixture");
            long tick=controller.Simulation.State.Tick;yield return null;yield return null;
            Require(controller.Simulation.State.Tick==tick && !controller.IsWorldPoint(controller.WorldViewport.center),"Title holds simulation time and blocks world input");
            controller.HandleShortcut(KeyCode.Escape);
            Require(!controller.HasActiveSession && controller.CurrentSessionPage==SessionPage.Home && !controller.HandleShortcut(KeyCode.Space),"Escape cannot enter the preview simulation");
            yield return Capture("sessions-title.png");Require(captureFailure==null,captureFailure??"Title screen captured");
            var before=controller.Simulation;
            Require(!controller.RequestNewSession(0,"not a seed") && ReferenceEquals(before,controller.Simulation),"Invalid seed does not replace the running state");
            Require(controller.RequestNewSession(0,"8026") && controller.HasActiveSession && !controller.HasSessionConfirmation,"First normal game starts without a discard prompt");
            string reference=SaveCodec.ToJson(GameSimulation.CreateReferenceScenario(8026).State);
            Require(SaveCodec.ToJson(controller.Simulation.State)==reference,"Menu starts the actual deterministic F05 economy without bonus grants");
            yield return null;yield return null;
            Require(SaveCodec.ToJson(controller.Simulation.State)==reference && !controller.Simulation.State.Paused,"Guide freezes time without rewriting the simulation pause flag");
            controller.RequestNewSession(2,"2026");
            Require(controller.HasSessionConfirmation && SaveCodec.ToJson(controller.Simulation.State)==reference,"Replacing a live game waits for explicit confirmation");
            controller.ResolveSessionConfirmation(false);
            Require(SaveCodec.ToJson(controller.Simulation.State)==reference,"Cancelled new game keeps the complete old state");
            controller.RequestNewSession(2,"2026");controller.ResolveSessionConfirmation(true);
            Require(SaveCodec.ToJson(controller.Simulation.State)==SaveCodec.ToJson(GameSimulation.CreateAiScenario(2026).State),"War scenario uses its ordinary economy and validated scenario factory");
            controller.RequestNewSession(1,"2026");controller.ResolveSessionConfirmation(true);
            Require(SaveCodec.ToJson(controller.Simulation.State)==SaveCodec.ToJson(GameSimulation.CreateDiplomacyScenario(2026).State),"Diplomatic scenario uses its actual deterministic start");
            controller.RequestNewSession(0,"8026");controller.ResolveSessionConfirmation(true);
            controller.SetSimulationPaused(true);reference=SaveCodec.ToJson(controller.Simulation.State);
            for(int page=0;page<4;page++)
            {controller.OpenGuideDestination(page);Require(SaveCodec.ToJson(controller.Simulation.State)==reference,"Guide navigation issues no simulation commands: "+page);controller.ShowSessionPage(SessionPage.Guide);}
            Require(controller.SaveGame() && controller.ActiveSlotId=="slot-1","First F5 saves to an empty slot");
            Require(SaveCodec.ToJson(slots.Load("slot-1"))==reference,"Quick save stores the complete current state");
            controller.SetSimulationPaused(false);controller.Simulation.Step(20);controller.SetSimulationPaused(true);string second=SaveCodec.ToJson(controller.Simulation.State);
            Require(second!=reference,"Overwrite fixture contains actual simulation progress");
            controller.RequestSaveSessionSlot("slot-1");
            Require(controller.HasSessionConfirmation && SaveCodec.ToJson(slots.Load("slot-1"))==reference,"Occupied slot remains untouched before confirmation");
            controller.ResolveSessionConfirmation(false);
            Require(SaveCodec.ToJson(slots.Load("slot-1"))==reference,"Cancelled overwrite preserves the saved game");
            controller.RequestSaveSessionSlot("slot-1");controller.ResolveSessionConfirmation(true);
            Require(SaveCodec.ToJson(slots.Load("slot-1"))==second && SaveCodec.ToJson(slots.Load("slot-1",true))==reference,"Confirmed overwrite creates the correct independent backup");
            controller.ShowSessionPage(SessionPage.Load);var running=controller.Simulation;
            controller.RequestLoadSessionSlot("slot-1",true);controller.ResolveSessionConfirmation(false);
            Require(ReferenceEquals(running,controller.Simulation) && SaveCodec.ToJson(running.State)==second,"Cancelled backup load keeps the running game");
            File.WriteAllText(slots.PathFor("slot-1"),"{damaged");
            Require(!controller.LoadSessionSlot("slot-1") && ReferenceEquals(running,controller.Simulation) && controller.ActiveSlotId=="slot-1","Damaged load keeps state and active slot binding");
            controller.ShowSessionPage(SessionPage.Load);yield return Capture("sessions-damaged.png");Require(captureFailure==null,captureFailure??"Corrupt slot and explicit backup captured");
            Require(controller.LoadSessionSlot("slot-1",true) && SaveCodec.ToJson(controller.Simulation.State)==reference,"Explicit backup restores exactly the previous state");
            Require(controller.SaveGame() && SaveCodec.ToJson(slots.Load("slot-1",true))==reference,"Saving recovery retains the good backup");
            Require(controller.SaveSessionSlot("slot-2") && controller.ActiveSlotId=="slot-2","Saving a second slot changes only that session's destination");
            string legacy=slots.PathFor("prototype-save");SnapshotStore.Save(legacy,GameSimulation.CreateDefault(19).State);string legacyBytes=File.ReadAllText(legacy);
            Require(controller.LoadSessionSlot("prototype-save") && controller.ActiveSlotId==null,"Old saves load without becoming an overwrite destination");
            Require(controller.SaveGame() && controller.ActiveSlotId=="slot-3" && File.ReadAllText(legacy)==legacyBytes,"F5 after legacy load chooses a free slot and preserves the old file");
            controller.StartSpaceFixture();controller.SetSimulationPaused(true);
            Require(controller.ActiveSlotId==null,"Test fixture does not inherit the previous game's save destination");
            Require(controller.RestorePreviousSession() && controller.ActiveSlotId=="slot-3","Returning from a fixture restores the previous save destination");
            controller.Simulation.State.Paused=true;controller.SetMenuVisible(false);
            Require(controller.HandleShortcut(KeyCode.F9) && controller.HasSessionConfirmation,"F9 requests confirmation before discarding live progress");
            controller.HandleShortcut(KeyCode.Escape);
            Require(!controller.HasSessionConfirmation && controller.CurrentSessionPage==SessionPage.Load,"Escape cancels the load confirmation");
            for(int i=4;i<=SaveSlots.Count;i++)slots.Save(SaveSlots.Id(i),controller.Simulation.State);
            controller.RequestNewSession(0,"8027");controller.ResolveSessionConfirmation(true);
            Require(!controller.SaveGame() && controller.CurrentSessionPage==SessionPage.Save && controller.ActiveSlotId==null,"Full slots open a choice instead of overwriting another game");
            Require(File.ReadAllText(legacy)==legacyBytes && SaveCodec.ToJson(slots.Load("slot-2"))==reference,"Full catalog and new session preserve prior games");
            foreach(InterfaceLayout layout in Enum.GetValues(typeof(InterfaceLayout)))
            {
                controller.SetInterfaceLayout(layout);
                foreach(SessionPage page in new[]{SessionPage.Home,SessionPage.NewGame,SessionPage.Save,SessionPage.Guide})
                {
                    controller.ShowSessionPage(page);tick=controller.Simulation.State.Tick;yield return null;
                    Require(!controller.IsWorldPoint(controller.WorldViewport.center) && controller.Simulation.State.Tick==tick,"Session page blocks input and freezes time: "+layout+"/"+page);
                    yield return Capture("sessions-"+layout+"-"+page+".png");Require(captureFailure==null,captureFailure??("Session page captured: "+layout+"/"+page));
                }
            }
            controller.ShowSessionPage(SessionPage.Home);tick=controller.Simulation.State.Tick;
            controller.HandleShortcut(KeyCode.Escape);yield return new WaitForSecondsRealtime(.15f);
            Require(controller.Simulation.State.Tick>tick,"Closing the menu resumes an unpaused game");
            controller.SetSimulationPaused(true);tick=controller.Simulation.State.Tick;controller.ShowSessionPage(SessionPage.Home);controller.HandleShortcut(KeyCode.Escape);yield return new WaitForSecondsRealtime(.15f);
            Require(controller.Simulation.State.Tick==tick && controller.Simulation.State.Paused,"Closing the menu preserves a player-paused game");
            foreach(int previousWorld in new[]{6,7})
            {
                controller.StartPlanetEnvironmentFixture();controller.SetView(previousWorld);
                Require(controller.OpenPlanetId==previousWorld,"Replacement fixture opens its ocean or gas world: "+previousWorld);
                int seed=9180+previousWorld;
                Require(controller.RequestNewSession(0,seed.ToString()) && controller.HasSessionConfirmation,"Replacing a fixture still asks before discarding it");
                controller.ResolveSessionConfirmation(true);yield return null;
                Require(controller.OpenPlanetId==0 && !controller.IsViewTransitioning && controller.Simulation.FindPlanet(previousWorld)==null,
                    "Session replacement clears a previous world that does not exist in the new scenario: "+previousWorld);
                Require(SaveCodec.ToJson(controller.Simulation.State)==SaveCodec.ToJson(GameSimulation.CreateReferenceScenario(seed).State),
                    "Ocean/gas fixture replacement preserves the exact new reference economy: "+previousWorld);
            }
            controller.SetMenuVisible(false);controller.SetView(0);controller.SetSpaceFocus(Vector3.zero);controller.CloseContextPanel();yield return null;
            Require(controller.SelectStarAtScreen(Camera.main.WorldToScreenPoint(Vector3.zero)) && controller.StarSelected,"Replacement test selects the actual central star");
            Require(controller.RequestNewSession(0,"9188") && controller.HasSessionConfirmation,"Star selection does not bypass session confirmation");
            controller.ResolveSessionConfirmation(true);yield return null;
            Require(controller.OpenPlanetId==0 && !controller.StarSelected && controller.Simulation.FindPlanet(controller.SelectedPlanetId)!=null &&
                SaveCodec.ToJson(controller.Simulation.State)==SaveCodec.ToJson(GameSimulation.CreateReferenceScenario(9188).State),
                "Starting a new reference game after star selection selects a valid world without dereferencing the star sentinel");
            controller.ConfigureSmokeSavePath(Path.Combine(outputDirectory,"smoke-save.json"));
        }
    }
}
