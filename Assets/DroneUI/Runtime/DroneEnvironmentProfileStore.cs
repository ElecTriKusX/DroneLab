using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace DroneLab.UI
{
    /// <summary>User overrides and deletion markers keep bundled profiles editable without altering Resources.</summary>
    public sealed class DroneEnvironmentProfileStore
    {
        public readonly string Folder;
        private string Markers => Path.Combine(Folder, ".removed.json");
        public DroneEnvironmentProfileStore(string folder) { Folder = folder; }
        public static bool ValidId(string id) => Guid.TryParseExact(id, "N", out _) ||
            (id != null && id.StartsWith("builtin-environment_", StringComparison.Ordinal) && id.Substring(8).All(c => c <= 127 && (char.IsLetterOrDigit(c) || c == '_')));
        public string PathFor(string id) { if (!ValidId(id)) throw new ArgumentException("Неверный идентификатор профиля."); return Path.Combine(Folder,id+".json"); }
        public HashSet<string> Removed() => File.Exists(Markers) ? new HashSet<string>(JsonConvert.DeserializeObject<string[]>(File.ReadAllText(Markers)) ?? Array.Empty<string>()) : new HashSet<string>();
        private void WriteMarkers(HashSet<string> ids) { Directory.CreateDirectory(Folder); Atomic(Markers, JsonConvert.SerializeObject(ids.OrderBy(x => x))); }
        public void Save(DroneEnvironmentDocument document)
        {
            Directory.CreateDirectory(Folder); Atomic(PathFor(document.id), JsonConvert.SerializeObject(document,Formatting.Indented));
            var removed = Removed(); if (removed.Remove(document.id)) WriteMarkers(removed);
        }
        public void Delete(DroneEnvironmentDocument document)
        {
            var path = PathFor(document.id);
            if (document.id.StartsWith("builtin-",StringComparison.Ordinal)) { var removed = Removed(); removed.Add(document.id); WriteMarkers(removed); }
            File.Delete(path);
        }
        public IEnumerable<string> Files() { Directory.CreateDirectory(Folder); return Directory.GetFiles(Folder,"*.json").Where(p => Path.GetFileName(p) != ".removed.json").OrderBy(x=>x); }
        public static void Atomic(string path,string contents)
        {
            string temp = path + ".tmp"; File.WriteAllText(temp,contents);
            try { if (File.Exists(path)) File.Replace(temp,path,null); else File.Move(temp,path); }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }
}
