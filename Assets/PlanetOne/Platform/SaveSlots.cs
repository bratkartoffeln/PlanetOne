using System;
using System.Collections.Generic;
using System.IO;
using PlanetOne.Core;

namespace PlanetOne.Platform
{
    public sealed class SaveSlotInfo
    {
        public string Id, Label, Error, BackupError;
        public bool ReadOnly, Exists, Valid, BackupExists, BackupValid;
        public long Tick;
        public int Seed;
        public DateTime SavedUtc;
    }

    /// <summary>Fixed local slots; metadata is derived from validated snapshots, never a second manifest.</summary>
    public sealed class SaveSlots
    {
        public const int Count = 8;
        private readonly string root;
        private static readonly string[] LegacyNames = { "prototype-save", "diplomacy-save", "ai-scenario-save", "space-fixture-save", "military-fixture-save", "invasion-fixture-save", "diplomacy-fixture-save" };
        private static readonly string[] LegacyLabels = { "Aufbaupartie", "Diplomatie / Drei Kolonien", "Computergegner", "Prüfkarte Raumfahrt", "Prüfkarte Bodengefecht", "Prüfkarte Invasion", "Prüfkarte Verträge" };
        public SaveSlots(string directory) { root = Path.GetFullPath(directory); }
        public static string Id(int index)
        { if(index<1 || index>Count)throw new ArgumentOutOfRangeException("index"); return "slot-"+index; }
        public string PathFor(string id)
        {
            for(int i=1;i<=Count;i++)if(id==Id(i))return Path.Combine(root,"Saves",id+".json");
            foreach(string name in LegacyNames)if(id==name)return Path.Combine(root,name+".json");
            throw new ArgumentException("Unbekannter Speicherplatz.","id");
        }
        public bool IsReadOnly(string id) { PathFor(id); return Array.IndexOf(LegacyNames,id)>=0; }
        public string FirstEmpty()
        {
            for(int i=1;i<=Count;i++)
            {string path=PathFor(Id(i));if(!File.Exists(path) && !File.Exists(path+".bak"))return Id(i);}
            return null;
        }
        public List<SaveSlotInfo> List()
        {
            var result=new List<SaveSlotInfo>();
            for(int i=1;i<=Count;i++)result.Add(Inspect(Id(i)));
            foreach(string name in LegacyNames)
            {var info=Inspect(name);if(info.Exists || info.BackupExists)result.Add(info);}
            return result;
        }
        public SaveSlotInfo Inspect(string id)
        {
            string path=PathFor(id);bool legacy=IsReadOnly(id);
            var info=new SaveSlotInfo {Id=id,ReadOnly=legacy,Label=legacy?"Bisheriger Stand · "+LegacyLabels[Array.IndexOf(LegacyNames,id)]:"Speicherplatz "+id.Substring(5),Exists=File.Exists(path),BackupExists=File.Exists(path+".bak")};
            try
            {
                if(info.Exists){var state=SnapshotStore.Load(path);info.Tick=state.Tick;info.Seed=state.Seed;info.SavedUtc=File.GetLastWriteTimeUtc(path);info.Valid=true;}
            }
            catch(Exception error) when(error is IOException || error is UnauthorizedAccessException || error is InvalidDataException)
            {info.Error=error.Message;}
            try {if(info.BackupExists){SnapshotStore.Load(path+".bak");info.BackupValid=true;}}
            catch(Exception error) when(error is IOException || error is UnauthorizedAccessException || error is InvalidDataException)
            {info.BackupError=error.Message;}
            return info;
        }
        public GameState Load(string id,bool backup=false) { return SnapshotStore.Load(PathFor(id)+(backup?".bak":"")); }
        public void Save(string id,GameState state,bool overwrite=false)
        {
            string path=PathFor(id);
            if(IsReadOnly(id))throw new InvalidOperationException("Bisherige Spielstände bleiben unverändert. Einen neuen Speicherplatz wählen.");
            if(!overwrite && (File.Exists(path) || File.Exists(path+".bak")))throw new IOException("Speicherplatz ist belegt. Überschreiben zuerst bestätigen.");
            SnapshotStore.Save(path,state);
        }
    }
}
