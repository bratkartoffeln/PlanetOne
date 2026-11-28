using System;
using System.IO;
using PlanetOne.Core;
using PlanetOne.Platform;

internal static class SaveSlotTests
{
    public static void Register(Action<string,Action> run)
    {
        run("M9 separate save slots retain state and backup without touching other games",Independent);
        run("M9 corrupt slots stay listed and require explicit backup recovery",Recovery);
        run("M9 legacy saves remain read-only and slot identifiers reject path traversal",Legacy);
        run("M9 full slots and backup-only slots never cause an implicit overwrite",Full);
    }
    private static string Root(){return Path.Combine(Environment.CurrentDirectory,"TestResults","SaveSlots",Guid.NewGuid().ToString("N"));}
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void Independent()
    {
        var slots=new SaveSlots(Root());var a=GameSimulation.CreateDefault(7);var b=GameSimulation.CreateReferenceScenario(9);
        string original=SaveCodec.ToJson(a.State),other=SaveCodec.ToJson(b.State);
        slots.Save("slot-1",a.State);slots.Save("slot-2",b.State);a.Step(20);
        bool rejected=false;try{slots.Save("slot-1",a.State);}catch(IOException){rejected=true;}
        Check(rejected && SaveCodec.ToJson(slots.Load("slot-1"))==original,"Unconfirmed overwrite changed a save");
        slots.Save("slot-1",a.State,true);
        Check(SaveCodec.ToJson(slots.Load("slot-1",true))==original && SaveCodec.ToJson(slots.Load("slot-2"))==other,"Slot or backup isolation failed");
        var info=slots.Inspect("slot-1");Check(info.Valid && info.BackupValid && info.Seed==7 && info.Tick==a.State.Tick,"Metadata does not describe saved state");
    }
    private static void Recovery()
    {
        var slots=new SaveSlots(Root());var a=GameSimulation.CreateDefault();string original=SaveCodec.ToJson(a.State);
        slots.Save("slot-1",a.State);a.Step();slots.Save("slot-1",a.State,true);File.WriteAllText(slots.PathFor("slot-1"),"{broken");
        var info=slots.List()[0];Check(info.Exists && !info.Valid && info.BackupValid && info.Error!=null,"Corrupt entry vanished or appeared valid");
        bool rejected=false;try{slots.Load("slot-1");}catch(InvalidDataException){rejected=true;}
        Check(rejected && SaveCodec.ToJson(slots.Load("slot-1",true))==original,"Load silently fell back or lost backup");
        slots.Save("slot-1",a.State,true);Check(SaveCodec.ToJson(slots.Load("slot-1",true))==original,"Recovery destroyed good backup");
    }
    private static void Legacy()
    {
        var slots=new SaveSlots(Root());var a=GameSimulation.CreateDefault();string path=slots.PathFor("prototype-save");SnapshotStore.Save(path,a.State);string bytes=File.ReadAllText(path);
        Check(slots.List().Count==9 && slots.Inspect("prototype-save").ReadOnly,"Legacy entry unavailable");
        bool rejected=false;try{slots.Save("prototype-save",a.State,true);}catch(InvalidOperationException){rejected=true;}
        Check(rejected && File.ReadAllText(path)==bytes,"Legacy file overwritten");
        foreach(string id in new[]{"../outside","slot-0","slot-9","slot-01","slot-1/../../outside",null})
        {rejected=false;try{slots.PathFor(id);}catch(ArgumentException){rejected=true;}Check(rejected,"Unsafe slot accepted");}
    }
    private static void Full()
    {
        var slots=new SaveSlots(Root());var a=GameSimulation.CreateDefault();
        for(int i=1;i<=SaveSlots.Count;i++){Check(slots.FirstEmpty()==SaveSlots.Id(i),"Wrong empty slot");slots.Save(SaveSlots.Id(i),a.State);}
        Check(slots.FirstEmpty()==null,"Full catalog offers occupied slot");
        File.Move(slots.PathFor("slot-4"),slots.PathFor("slot-4")+".bak");
        Check(slots.FirstEmpty()==null && slots.Inspect("slot-4").BackupValid,"Backup-only slot treated as empty");
    }
}
