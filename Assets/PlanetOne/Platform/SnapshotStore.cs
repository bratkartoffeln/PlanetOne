using System;
using System.IO;
using System.Text;
using PlanetOne.Core;

namespace PlanetOne.Platform
{
    /// <summary>Local filesystem adapter. A failed write never overwrites the usable snapshot.</summary>
    public static class SnapshotStore
    {
        public const long MaximumFileBytes=16*1024*1024;
        public static GameState Load(string path)
        {
            using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))
            {
                if(stream.Length==0 || stream.Length>MaximumFileBytes)throw new InvalidDataException("Spielstand ist leer oder überschreitet die zulässige Dateigröße.");
                using(var reader=new StreamReader(stream,new UTF8Encoding(false,true),true))
                {
                    try{return SaveCodec.FromJson(reader.ReadToEnd());}
                    catch(DecoderFallbackException error){throw new InvalidDataException("Spielstand enthält ungültige Textdaten.",error);}
                }
            }
        }
        public static void Save(string path,GameState state)
        {
            string json=SaveCodec.ToJson(state);SaveCodec.FromJson(json);
            path=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                byte[] bytes=new UTF8Encoding(false,true).GetBytes(json);
                using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
                {stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                if(!File.Exists(path)){File.Move(temporary,path);return;}
                bool previousValid=true;
                try{Load(path);}catch(InvalidDataException){previousValid=false;}
                // Keep an existing good backup if the primary file was damaged externally.
                // Unsupported atomic replacement fails safely; never fall back to overwriting in place.
                File.Replace(temporary,path,previousValid?path+".bak":null);
            }
            finally
            {
                if(File.Exists(temporary))
                    try{File.Delete(temporary);}catch(IOException){}catch(UnauthorizedAccessException){}
            }
        }
    }
}
