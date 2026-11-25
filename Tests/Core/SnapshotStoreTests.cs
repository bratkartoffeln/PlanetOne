using System;
using System.IO;
using PlanetOne.Core;
using PlanetOne.Platform;

internal static class SnapshotStoreTests
{
    public static void Register(Action<string,Action> run)
    {
        run("M8 disk snapshots retain the exact previous valid state as an explicit backup",Backup);
        run("M8 corrupt primary recovery preserves a good backup when saving again",CorruptPrimary);
        run("M8 invalid state and locked destination leave primary and backup untouched",WriteFailure);
        run("M8 bounded file loading rejects corrupt unsupported and oversized snapshots",InvalidFiles);
    }
    private static string PathForTest()
    {return Path.Combine(Environment.CurrentDirectory,"TestResults","SnapshotStore",Guid.NewGuid().ToString("N"),"save.json");}
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void Backup()
    {
        var s=GameSimulation.CreateDefault(2);string path=PathForTest(),first=SaveCodec.ToJson(s.State);SnapshotStore.Save(path,s.State);
        Check(SaveCodec.ToJson(SnapshotStore.Load(path))==first && !File.Exists(path+".bak"),"First save changed state");
        s.Step(20);string second=SaveCodec.ToJson(s.State);SnapshotStore.Save(path,s.State);
        Check(SaveCodec.ToJson(SnapshotStore.Load(path))==second && SaveCodec.ToJson(SnapshotStore.Load(path+".bak"))==first,"Previous valid snapshot not retained");
        Check(Directory.GetFiles(Path.GetDirectoryName(path),"*.tmp").Length==0,"Temporary save left behind");
    }
    private static void CorruptPrimary()
    {
        var s=GameSimulation.CreateDefault();string path=PathForTest(),first=SaveCodec.ToJson(s.State);SnapshotStore.Save(path,s.State);s.Step();SnapshotStore.Save(path,s.State);
        File.WriteAllText(path,"{broken");bool rejected=false;try{SnapshotStore.Load(path);}catch(InvalidDataException){rejected=true;}Check(rejected,"Bad primary silently recovered");
        Check(SaveCodec.ToJson(SnapshotStore.Load(path+".bak"))==first,"Explicit backup cannot load");
        s.Step();SnapshotStore.Save(path,s.State);
        Check(SaveCodec.ToJson(SnapshotStore.Load(path))==SaveCodec.ToJson(s.State) && SaveCodec.ToJson(SnapshotStore.Load(path+".bak"))==first,"Saving damaged the good backup");
    }
    private static void WriteFailure()
    {
        var s=GameSimulation.CreateDefault();string path=PathForTest();SnapshotStore.Save(path,s.State);s.Step();SnapshotStore.Save(path,s.State);
        string primary=File.ReadAllText(path),backup=File.ReadAllText(path+".bak");int version=s.State.Version;s.State.Version=999;
        bool rejected=false;try{SnapshotStore.Save(path,s.State);}catch(InvalidDataException){rejected=true;}Check(rejected,"Invalid state was written");s.State.Version=version;s.Step();
        using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None))
        {rejected=false;try{SnapshotStore.Save(path,s.State);}catch(IOException){rejected=true;}Check(rejected,"Locked destination ignored");}
        Check(File.ReadAllText(path)==primary && File.ReadAllText(path+".bak")==backup,"Failed save overwrote a valid snapshot");
        Check(Directory.GetFiles(Path.GetDirectoryName(path),"*.tmp").Length==0,"Failed save leaked a temporary file");
    }
    private static void InvalidFiles()
    {
        string path=PathForTest();Directory.CreateDirectory(Path.GetDirectoryName(path));
        foreach(string data in new[]{"", "{}", "{\"Version\":999}","{not-json"})
        {File.WriteAllText(path,data);bool rejected=false;try{SnapshotStore.Load(path);}catch(InvalidDataException){rejected=true;}Check(rejected,"Invalid disk snapshot accepted");}
        using(var stream=new FileStream(path,FileMode.Create,FileAccess.Write))stream.SetLength(SnapshotStore.MaximumFileBytes+1);
        bool oversized=false;try{SnapshotStore.Load(path);}catch(InvalidDataException){oversized=true;}Check(oversized,"Oversized file read without a bound");
    }
}
